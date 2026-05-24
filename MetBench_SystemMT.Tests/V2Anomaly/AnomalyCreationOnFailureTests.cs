using System.Collections.ObjectModel;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT;
using MetBench_SystemMT.Tests.V2Pipeline;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Anomaly;

/// <summary>
/// UC-B7 — 失败的 System MT run 必须流入 Anomalies 表。
/// 覆盖:(a) AnomalyService.RecordAnomalyAsync 单元;(b)(c) SystemMtLauncher
/// 把 PipelineOutcome FinalStatus=anomaly / ok 正反两向路由到 anomaly service。
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
    public async Task SystemMtLauncher_records_anomaly_when_outcome_is_anomaly()
    {
        var recording = new RecordingAnomalyService();
        var launcher = MakeLauncher(recording);

        var resultId = Guid.NewGuid();
        var failingOutcome = MakeOutcome(sourceValue: 1.13, followupValue: 1.10, finalStatus: PipelineStatus.Anomaly);

        await launcher.RecordAnomalyIfFailedAsync(
            mrName: "OpenMOC pin-cell — ScaleNuSigmaF (k_eff increases)",
            resultId: resultId,
            outcome: failingOutcome,
            cancellationToken: default);

        Assert.Single(recording.Recorded);
        var call = recording.Recorded[0];
        Assert.Equal("OpenMOC pin-cell — ScaleNuSigmaF (k_eff increases)", call.MrName);
        Assert.Equal(resultId.ToString(), call.ResultId);
        // (1.13 → 1.10) ≈ 2.65% drop → "minor" bucket
        Assert.Equal("minor", call.Severity);
        Assert.Equal("single-point", call.Category);
    }

    [Fact]
    public async Task SystemMtLauncher_does_not_record_anomaly_when_outcome_is_ok()
    {
        var recording = new RecordingAnomalyService();
        var launcher = MakeLauncher(recording);

        var passingOutcome = MakeOutcome(sourceValue: 1.13, followupValue: 1.51, finalStatus: PipelineStatus.Ok);

        await launcher.RecordAnomalyIfFailedAsync(
            mrName: "OpenMOC pin-cell — ScaleNuSigmaF (k_eff increases)",
            resultId: Guid.NewGuid(),
            outcome: passingOutcome,
            cancellationToken: default);

        Assert.Empty(recording.Recorded);
    }

    [Fact]
    public async Task SystemMtLauncher_skips_anomaly_when_resultId_is_null()
    {
        // error / timeout 状态下 recorder 不写 Result,resultId 为 null,不能链 anomaly。
        var recording = new RecordingAnomalyService();
        var launcher = MakeLauncher(recording);

        var errorOutcome = new PipelineOutcome(
            FinalStatus: PipelineStatus.Error, ErrorMessage: "SUT crashed",
            StartedAt: DateTime.UtcNow, FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: "/tmp",
            SourceInputPath: "", FollowupInputPath: "", SourceOutputPath: "", FollowupOutputPath: "",
            SourceMetrics: null, FollowupMetrics: null, AssertionResult: null,
            SourceElapsed: TimeSpan.Zero, FollowupElapsed: TimeSpan.Zero,
            SourceExitCode: 1, FollowupExitCode: 0);

        await launcher.RecordAnomalyIfFailedAsync(
            mrName: "test", resultId: null, outcome: errorOutcome, cancellationToken: default);

        Assert.Empty(recording.Recorded);
    }

    // =================== fixtures ===================

    private static SystemMtLauncher MakeLauncher(IAnomalyService anomalyService) =>
        new(
            new LauncherOptions(
                SutRoot: TestAssetPaths.AssetRoot(),
                SystemPython: TestAssetPaths.PythonExecutable(),
                OpenMocPython: TestAssetPaths.PythonExecutable()),
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            anomalyService,
            new ManifestMrCatalogProvider(new LauncherOptions(
                SutRoot: TestAssetPaths.AssetRoot(),
                SystemPython: TestAssetPaths.PythonExecutable(),
                OpenMocPython: TestAssetPaths.PythonExecutable())));

    private static PipelineOutcome MakeOutcome(
        double sourceValue, double followupValue, string finalStatus)
    {
        var input = new AssertionInput(sourceValue, 0.0, followupValue, 0.0, "k_eff", null);
        var assertion = finalStatus == PipelineStatus.Ok
            ? SystemMtAssertionResultV2.PassedResult(input, "greater")
            : SystemMtAssertionResultV2.FailedResult(input, "greater", "MR violated");
        return new PipelineOutcome(
            FinalStatus: finalStatus, ErrorMessage: null,
            StartedAt: DateTime.UtcNow.AddSeconds(-1), FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: "/tmp",
            SourceInputPath: "/tmp/s.in", FollowupInputPath: "/tmp/f.in",
            SourceOutputPath: "/tmp/s.out", FollowupOutputPath: "/tmp/f.out",
            SourceMetrics: new Dictionary<string, double> { ["k_eff"] = sourceValue },
            FollowupMetrics: new Dictionary<string, double> { ["k_eff"] = followupValue },
            AssertionResult: assertion,
            SourceElapsed: TimeSpan.FromSeconds(1), FollowupElapsed: TimeSpan.FromSeconds(1),
            SourceExitCode: 0, FollowupExitCode: 0);
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
