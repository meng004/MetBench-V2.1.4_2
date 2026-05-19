using System.Collections.ObjectModel;
using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Anomaly;

/// <summary>
/// UC-B7 — 失败的 System MT run 必须流入 Anomalies 表。
/// 覆盖：(a) AnomalyService.RecordAnomalyAsync 单元；(b)(c) SystemMtMrLauncher
/// 把 Passed=false / Passed=true 正反两向路由到 anomaly service。
/// </summary>
public sealed class AnomalyCreationOnFailureTests
{
    [Fact]
    public async Task AnomalyService_RecordAnomalyAsync_creates_entity_with_expected_fields()
    {
        var anomalyRepo = new FakeAnomalyRepository(Array.Empty<MetBench_Domain.Anomaly>());
        var auditRepo = new FakeAuditLogRepository();
        var svc = new AnomalyService(anomalyRepo, auditRepo);

        var resultId = Guid.NewGuid();
        var before = DateTime.UtcNow;
        var anomaly = await svc.RecordAnomalyAsync(
            mrName: "OpenMOC pin-cell — ScaleNuSigmaF (k_eff increases)",
            resultId: resultId.ToString(),
            severity: "minor",
            category: "single-point");
        var after = DateTime.UtcNow;

        Assert.NotNull(anomaly);
        Assert.NotEqual(Guid.Empty, anomaly.IdAnomaly);
        Assert.Equal(resultId, anomaly.ResultId);
        Assert.Equal("minor", anomaly.Severity);
        Assert.Equal("single-point", anomaly.Category);
        Assert.Equal("new", anomaly.Status);
        Assert.InRange(anomaly.DiscoveredAt, before.AddSeconds(-1), after.AddSeconds(1));

        var stored = anomalyRepo.GetAll();
        Assert.Single(stored);
        Assert.Equal(anomaly.IdAnomaly, stored[0].IdAnomaly);

        Assert.Single(auditRepo.Logs);
        var log = auditRepo.Logs[0];
        Assert.Equal("system-mt", log.Actor);
        Assert.Equal("anomaly.created", log.Action);
        Assert.Equal("Anomaly", log.TargetEntityType);
        Assert.Equal(anomaly.IdAnomaly.ToString(), log.TargetEntityId);
    }

    [Fact]
    public async Task SystemMtMrLauncher_records_anomaly_when_result_passed_is_false()
    {
        var recording = new RecordingAnomalyService();
        var stubRepo = new StubResultRepository();
        var launcher = new SystemMtMrLauncher(
            new LauncherOptions(SutRoot: "/tmp/dummy", SystemPython: "python3", OpenMocPython: "python3"),
            stubRepo,
            recording);

        var resultId = Guid.NewGuid().ToString();
        var failingResult = MakeResult(passed: false);

        await launcher.RecordAnomalyIfFailedAsync(
            mrName: "OpenMOC pin-cell — ScaleNuSigmaF (k_eff increases)",
            recordId: resultId,
            result: failingResult,
            cancellationToken: default);

        Assert.Single(recording.Recorded);
        var call = recording.Recorded[0];
        Assert.Equal("OpenMOC pin-cell — ScaleNuSigmaF (k_eff increases)", call.MrName);
        Assert.Equal(resultId, call.ResultId);
        Assert.Equal("minor", call.Severity);
        Assert.Equal("single-point", call.Category);
    }

    [Fact]
    public async Task SystemMtMrLauncher_does_not_record_anomaly_when_passed_true()
    {
        var recording = new RecordingAnomalyService();
        var stubRepo = new StubResultRepository();
        var launcher = new SystemMtMrLauncher(
            new LauncherOptions(SutRoot: "/tmp/dummy", SystemPython: "python3", OpenMocPython: "python3"),
            stubRepo,
            recording);

        var passingResult = MakeResult(passed: true);

        await launcher.RecordAnomalyIfFailedAsync(
            mrName: "OpenMOC pin-cell — ScaleNuSigmaF (k_eff increases)",
            recordId: Guid.NewGuid().ToString(),
            result: passingResult,
            cancellationToken: default);

        Assert.Empty(recording.Recorded);
    }

    // =================== fixtures ===================

    private static SystemMtResult MakeResult(bool passed)
    {
        var sourceRun = new CliRunResult(
            "source", 0, "stdout-source", string.Empty,
            TimeSpan.FromSeconds(1.0), "/tmp/source/output.json", true, string.Empty);
        var followUpRun = new CliRunResult(
            "follow-up", 0, "stdout-followup", string.Empty,
            TimeSpan.FromSeconds(1.0), "/tmp/followup/output.json", true, string.Empty);
        var sourceOutput = new ParsedOutput(
            new Dictionary<string, double> { ["k_eff"] = 1.13 },
            new Dictionary<string, string>());
        var followUpOutput = new ParsedOutput(
            new Dictionary<string, double> { ["k_eff"] = passed ? 1.51 : 1.10 },
            new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult(
            "GreaterThan", "k_eff", 1.13, passed ? 1.51 : 1.10, passed,
            passed ? string.Empty : "MR violated");
        return new SystemMtResult(
            sourceRun, followUpRun, sourceOutput, followUpOutput, assertion,
            passed, passed ? string.Empty : "MR violated");
    }
}

// =================== test doubles (project-internal) ===================

/// <summary>Captures every RecordAnomalyAsync call for assertion in tests.</summary>
internal sealed class RecordingAnomalyService : IAnomalyService
{
    public sealed record Call(string MrName, string ResultId, string Severity, string Category);
    public List<Call> Recorded { get; } = new();

    public Task<MetBench_Domain.Anomaly> RecordAnomalyAsync(
        string mrName, string resultId, string severity, string category, CancellationToken cancellationToken = default)
    {
        Recorded.Add(new Call(mrName, resultId, severity, category));
        return Task.FromResult(new MetBench_Domain.Anomaly
        {
            IdAnomaly = Guid.NewGuid(),
            ResultId = Guid.TryParse(resultId, out var g) ? g : Guid.NewGuid(),
            Severity = severity,
            Category = category,
            Status = "new",
        });
    }

    // Unused by these tests; throw to surface accidental invocations.
    public IReadOnlyList<MetBench_Domain.Anomaly> List(AnomalyFilter filter) => throw new NotImplementedException();
    public CommonalityReport AnalyzeCommonalities(IReadOnlyList<MetBench_Domain.Anomaly> anomalies) => throw new NotImplementedException();
    public bool TransitionStatus(Guid anomalyId, string newStatus, string? notes, string actor) => throw new NotImplementedException();
    public bool LinkToKnownBug(Guid anomalyId, int knownBugId, string actor) => throw new NotImplementedException();
    public bool UnlinkKnownBug(Guid anomalyId, string actor) => throw new NotImplementedException();
}

/// <summary>Minimal ISystemMtResultRepository stub — tests don't exercise persistence.</summary>
internal sealed class StubResultRepository : ISystemMtResultRepository
{
    public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default)
        => Task.FromResult(Guid.NewGuid().ToString());

    public Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<IReadOnlyList<SystemMtResultRecord>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<IReadOnlyList<SystemMtResultRecord>> ListByMrNameAsync(string mrName, int limit = 100, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
