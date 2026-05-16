using System.Collections.ObjectModel;
using MetBench_BLL.RCaseRepro;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2RCaseRepro;

/// <summary>
/// F9 测试：RCaseReproductionService —— 自动复现 R-Case + 链接到 KnownBug。
/// </summary>
public sealed class RCaseReproductionServiceTests
{
    [Fact]
    public async Task ReproduceAsync_unknown_bug_code_throws()
    {
        var fakes = MakeFakes();
        var svc = NewService(fakes);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ReproduceAsync(MakeSpecAndPrime(fakes, "R-Bogus", PipelineStatus.Anomaly, 1.13, 0.51)));
    }

    [Fact]
    public async Task ReproduceAsync_anomaly_with_large_gap_marks_reproduced()
    {
        // R-Case-4: ModSigmaA(1.5) → OpenMOC 0.4764 vs OpenMC 0.9683 (51% gap)
        var fakes = MakeFakes(rCase4: true);
        var svc = NewService(fakes);

        var report = await svc.ReproduceAsync(
            MakeSpecAndPrime(fakes, "R-Case-4", PipelineStatus.Anomaly, sourceK: 0.9683, followupK: 0.4764, threshold: 0.05));

        Assert.True(report.Reproduced);
        Assert.Equal("R-Case-4", report.KnownBugCode);
        Assert.NotNull(report.AnomalyId);
        Assert.NotNull(report.ExecutionId);
        Assert.NotNull(report.ResultId);
        Assert.NotNull(report.ObservedGap);
        Assert.True(report.ObservedGap > 0.05, $"expected gap > 5%, got {report.ObservedGap:P2}");

        // 落库
        Assert.Single(fakes.Executions.Data);
        Assert.Single(fakes.Results.Data);
        Assert.Single(fakes.Anomalies.Data);

        // Anomaly 链到 KnownBug
        var anomaly = fakes.Anomalies.Data[0];
        Assert.Equal(fakes.RCase4!.IdBug, anomaly.LinkedKnownBugId);
        Assert.Equal("major", anomaly.Severity);
        Assert.Equal("confirmed-bug", anomaly.Status);

        // AuditLog
        Assert.Contains(fakes.Audit.Logs, l => l.Action == "r-case.reproduced");
    }

    [Fact]
    public async Task ReproduceAsync_passing_pipeline_marks_not_reproduced_no_anomaly()
    {
        var fakes = MakeFakes(rCase4: true);
        var svc = NewService(fakes);

        // pipeline 通过 + 两个 k 都接近 → gap < threshold
        var report = await svc.ReproduceAsync(
            MakeSpecAndPrime(fakes, "R-Case-4", PipelineStatus.Ok, sourceK: 1.00, followupK: 1.005, threshold: 0.05));

        Assert.False(report.Reproduced);
        Assert.Null(report.AnomalyId);
        Assert.Null(report.ExecutionId);
        Assert.Empty(fakes.Executions.Data);
        Assert.Empty(fakes.Anomalies.Data);

        Assert.Contains(fakes.Audit.Logs, l => l.Action == "r-case.not-reproduced");
    }

    [Fact]
    public async Task ReproduceAsync_pipeline_exception_returns_error_report()
    {
        var fakes = MakeFakes(rCase4: true);
        fakes.Pipeline.NextThrow = new InvalidOperationException("simulated SUT failure");
        var svc = NewService(fakes);

        var report = await svc.ReproduceAsync(
            MakeSpecAndPrime(fakes, "R-Case-4", PipelineStatus.Ok, 1.0, 1.0));

        Assert.False(report.Reproduced);
        Assert.Equal("error", report.FinalStatus);
        Assert.Contains("simulated SUT failure", report.FailureReason);
        Assert.Contains(fakes.Audit.Logs, l => l.Action == "r-case.error");
    }

    [Fact]
    public async Task ReproduceAsync_anomaly_status_alone_triggers_reproduction_even_when_gap_small()
    {
        // Pipeline status=anomaly 但 gap 小 → 仍标 reproduced（assertion 已判 fail）
        var fakes = MakeFakes(rCase4: true);
        var svc = NewService(fakes);

        var report = await svc.ReproduceAsync(
            MakeSpecAndPrime(fakes, "R-Case-4", PipelineStatus.Anomaly, 1.0, 1.001, threshold: 0.05));

        Assert.True(report.Reproduced);
        Assert.Single(fakes.Anomalies.Data);
    }

    [Fact]
    public async Task ReproduceAsync_link_anomaly_false_skips_anomaly_row()
    {
        var fakes = MakeFakes(rCase4: true);
        var svc = NewService(fakes);

        var report = await svc.ReproduceAsync(new RCaseReproductionSpec(
            KnownBugCode: "R-Case-4",
            Context: MakeContext(),
            DetectionThreshold: 0.05,
            LinkAnomalyOnReproduction: false));
        fakes.Pipeline.NextOutcome = MakeOutcome(PipelineStatus.Anomaly, 0.9683, 0.4764);

        // 重跑 with no-link
        report = await svc.ReproduceAsync(new RCaseReproductionSpec(
            KnownBugCode: "R-Case-4",
            Context: MakeContext(),
            DetectionThreshold: 0.05,
            LinkAnomalyOnReproduction: false));

        Assert.True(report.Reproduced);
        Assert.Null(report.AnomalyId);          // 不链
        Assert.Empty(fakes.Anomalies.Data);     // anomaly 表空
        Assert.NotEmpty(fakes.Executions.Data); // execution + result 仍落
    }

    [Fact]
    public async Task ReproduceAsync_cancellation_propagates()
    {
        var fakes = MakeFakes(rCase4: true);
        fakes.Pipeline.RespectCancellation = true;
        var svc = NewService(fakes);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            svc.ReproduceAsync(MakeSpecAndPrime(fakes, "R-Case-4", PipelineStatus.Ok, 1.0, 1.0), cts.Token));
    }

    [Fact]
    public async Task ReproduceAsync_persists_execution_version_snapshot_from_context()
    {
        var fakes = MakeFakes(rCase4: true);
        var svc = NewService(fakes);

        await svc.ReproduceAsync(MakeSpecAndPrime(fakes, "R-Case-4", PipelineStatus.Anomaly, 0.9683, 0.4764,
            catalogSha: "abc123", sutVersion: "openmoc@2026-05", metbenchVersion: "v2.1"));

        var exec = fakes.Executions.Data[0];
        Assert.Equal("abc123", exec.CatalogVersionSha);
        Assert.Equal("openmoc@2026-05", exec.SutVersionSnapshot);
        Assert.Equal("v2.1", exec.MetbenchVersion);
    }

    [Fact]
    public async Task Audit_details_include_gap_threshold_and_ids()
    {
        var fakes = MakeFakes(rCase4: true);
        var svc = NewService(fakes);

        await svc.ReproduceAsync(MakeSpecAndPrime(fakes, "R-Case-4", PipelineStatus.Anomaly, 0.9683, 0.4764, threshold: 0.05));

        var log = fakes.Audit.Logs.Single(l => l.Action == "r-case.reproduced");
        Assert.Contains("gap", log.DetailsJson);
        Assert.Contains("threshold", log.DetailsJson);
        Assert.Contains("executionId", log.DetailsJson);
    }

    // ===== Fixtures =====

    private static RCaseReproductionService NewService(Fakes f) =>
        new(f.Pipeline, f.Bugs, f.Executions, f.Results, f.Anomalies, f.Audit);

    private static Fakes MakeFakes(bool rCase4 = false)
    {
        var fakes = new Fakes();
        if (rCase4)
        {
            fakes.RCase4 = new KnownBug
            {
                IdBug = 4, Code = "R-Case-4",
                Title = "OpenMOC CPUSolver power-iter basin @ moderator-σ_a factor=1.5",
                Status = "open",
                Description = "MetBench self-discovery: OpenMOC k=0.4764 vs OpenMC k=0.9683 — 51% gap.",
            };
            fakes.Bugs.Add(fakes.RCase4);
        }
        return fakes;
    }

    /// <summary>
    /// 同时构造 spec 并把对应 outcome 注入 fakes.Pipeline.NextOutcome。
    /// </summary>
    private static RCaseReproductionSpec MakeSpecAndPrime(
        Fakes fakes,
        string code,
        string finalStatus,
        double sourceK,
        double followupK,
        double threshold = 0.05,
        string catalogSha = "",
        string sutVersion = "",
        string metbenchVersion = "")
    {
        fakes.Pipeline.NextOutcome = MakeOutcome(finalStatus, sourceK, followupK);
        return new RCaseReproductionSpec(
            KnownBugCode: code,
            Context: MakeContext(catalogSha, sutVersion, metbenchVersion),
            DetectionThreshold: threshold,
            LinkAnomalyOnReproduction: true,
            Actor: "test");
    }

    private static PipelineContext MakeContext(
        string catalogSha = "", string sutVersion = "", string metbenchVersion = "")
        => new(
            MrCode: "MR14-cmp-openmoc-vs-openmc",
            TransformationName: "m_cmp",
            AssertionTypeCode: "bound",
            ValueName: "k_eff",
            TargetFieldPath: "scenario.case_path",
            PathSyntax: "json-pointer",
            Parameters: new Dictionary<string, string>(),
            Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: "openmoc",
            SourceCasePath: "/tmp/case.json",
            WorkingDirectory: "/tmp",
            InputParserCommand: "x", OutputParserCommand: "x", RunnerCommand: "x",
            TimeoutSeconds: 60,
            CatalogVersionSha: catalogSha,
            SutVersionSnapshot: sutVersion,
            MetbenchVersion: metbenchVersion,
            TriggeredBy: "test");

    private static PipelineOutcome MakeOutcome(string finalStatus, double sourceK, double followupK)
        => new(
            FinalStatus: finalStatus,
            ErrorMessage: null,
            StartedAt: DateTime.UtcNow.AddSeconds(-10),
            FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: "/tmp/art",
            SourceInputPath: "/tmp/src.in", FollowupInputPath: "/tmp/flw.in",
            SourceOutputPath: "/tmp/src.out", FollowupOutputPath: "/tmp/flw.out",
            SourceMetrics: new Dictionary<string, double> { ["k_eff"] = sourceK },
            FollowupMetrics: new Dictionary<string, double> { ["k_eff"] = followupK },
            AssertionResult: new SystemMtAssertionResultV2(
                AssertionTypeCode: "bound",
                Passed: finalStatus == PipelineStatus.Ok,
                SourceValue: sourceK,
                FollowupValue: followupK,
                ObservedDelta: followupK - sourceK,
                ExpectedThreshold: 0.05,
                Expression: $"|{followupK}-{sourceK}|",
                FailureReason: finalStatus == PipelineStatus.Ok ? null : "gap-exceeded"),
            SourceElapsed: TimeSpan.FromSeconds(1),
            FollowupElapsed: TimeSpan.FromSeconds(1),
            SourceExitCode: 0,
            FollowupExitCode: 0);

    internal sealed class Fakes
    {
        public KnownBug? RCase4 { get; set; }
        public FakeRCaseBugRepo Bugs { get; } = new();
        public FakeRCaseExecRepo Executions { get; } = new();
        public FakeRCaseResultRepo Results { get; } = new();
        public FakeRCaseAnomalyRepo Anomalies { get; } = new();
        public FakeRCaseAuditRepo Audit { get; } = new();
        public ProgrammablePipeline Pipeline { get; } = new();
    }
}

/// <summary>测试用 pipeline — 把 spec 的 (status, sourceK, followupK) 反推到 outcome。</summary>
internal sealed class ProgrammablePipeline : ISystemMtPipeline
{
    public PipelineOutcome? NextOutcome { get; set; }
    public Exception? NextThrow { get; set; }
    public bool RespectCancellation { get; set; }

    public Task<PipelineOutcome> ExecuteAsync(
        PipelineContext context, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (RespectCancellation && cancellationToken.IsCancellationRequested)
            cancellationToken.ThrowIfCancellationRequested();
        if (NextThrow is { } ex) throw ex;
        return Task.FromResult(NextOutcome ?? BuildFromTransformName(context));
    }

    /// <summary>
    /// 默认行为：用 context.TransformationName 当 hint 反推一个 outcome。
    /// 测试覆盖到的具体值由 MakeSpec → ProgrammablePipeline.NextOutcome 显式设。
    /// 兜底 outcome 是 Ok + k=1.0。
    /// </summary>
    private static PipelineOutcome BuildFromTransformName(PipelineContext _)
        => new(
            FinalStatus: PipelineStatus.Ok, ErrorMessage: null,
            StartedAt: DateTime.UtcNow, FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: "/tmp", SourceInputPath: "", FollowupInputPath: "",
            SourceOutputPath: "", FollowupOutputPath: "",
            SourceMetrics: new Dictionary<string, double> { ["k_eff"] = 1.0 },
            FollowupMetrics: new Dictionary<string, double> { ["k_eff"] = 1.0 },
            AssertionResult: null,
            SourceElapsed: TimeSpan.Zero, FollowupElapsed: TimeSpan.Zero,
            SourceExitCode: 0, FollowupExitCode: 0);
}

internal sealed class FakeRCaseBugRepo : IKnownBugRepository
{
    public List<KnownBug> Data { get; } = new();
    public ObservableCollection<KnownBug> GetAll() => new(Data);
    public KnownBug Get(int id) => Data.First(b => b.IdBug == id);
    public ObservableCollection<KnownBug> Get(KnownBug e) => new(Data);
    public bool Add(KnownBug e) { Data.Add(e); return true; }
    public bool Modify(KnownBug e) => true;
    public bool Remove(KnownBug e) => Data.Remove(e);
    public KnownBug? GetByCode(string code) => Data.FirstOrDefault(b => b.Code == code);
    public ObservableCollection<KnownBug> GetByStatus(string status) => new(Data.Where(b => b.Status == status).ToList());
}

internal sealed class FakeRCaseExecRepo : IExecutionRepository
{
    public List<Execution> Data { get; } = new();
    public ObservableCollection<Execution> GetAll() => new(Data);
    public Execution? Get(Guid id) => Data.FirstOrDefault(e => e.IdExecution == id);
    public ObservableCollection<Execution> Get(Execution e) => new(Data);
    public bool Add(Execution e) { Data.Add(e); return true; }
    public bool Modify(Execution e) => true;
    public bool Remove(Execution e) => Data.Remove(e);
    public ObservableCollection<Execution> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<Execution> GetByMRInstance(int id) => new();
    public ObservableCollection<Execution> GetByBatch(Guid id) => new();
    public ObservableCollection<Execution> GetByStatus(string s, int p, int sz) => new();
    public ObservableCollection<Execution> GetByDateRange(DateTime f, DateTime t) => new();
}

internal sealed class FakeRCaseResultRepo : IResultRepository
{
    public List<Result> Data { get; } = new();
    public ObservableCollection<Result> GetAll() => new(Data);
    public Result? Get(Guid id) => Data.FirstOrDefault(r => r.IdResult == id);
    public ObservableCollection<Result> Get(Result e) => new(Data);
    public bool Add(Result e) { Data.Add(e); return true; }
    public bool Modify(Result e) => true;
    public bool Remove(Result e) => Data.Remove(e);
    public ObservableCollection<Result> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public Result? GetByExecution(Guid id) => Data.FirstOrDefault(r => r.ExecutionId == id);
}

internal sealed class FakeRCaseAnomalyRepo : IAnomalyRepository
{
    public List<MetBench_Domain.Anomaly> Data { get; } = new();
    public ObservableCollection<MetBench_Domain.Anomaly> GetAll() => new(Data);
    public MetBench_Domain.Anomaly? Get(Guid id) => Data.FirstOrDefault(a => a.IdAnomaly == id);
    public ObservableCollection<MetBench_Domain.Anomaly> Get(MetBench_Domain.Anomaly e) => new(Data);
    public bool Add(MetBench_Domain.Anomaly e) { Data.Add(e); return true; }
    public bool Modify(MetBench_Domain.Anomaly e) => true;
    public bool Remove(MetBench_Domain.Anomaly e) => Data.Remove(e);
    public ObservableCollection<MetBench_Domain.Anomaly> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public MetBench_Domain.Anomaly? GetByResult(Guid id) => Data.FirstOrDefault(a => a.ResultId == id);
    public ObservableCollection<MetBench_Domain.Anomaly> GetByStatus(string s) => new();
    public ObservableCollection<MetBench_Domain.Anomaly> GetByLinkedBug(int id) => new();
}

internal sealed class FakeRCaseAuditRepo : IAuditLogRepository
{
    public List<AuditLog> Logs { get; } = new();
    public ObservableCollection<AuditLog> GetAll() => new(Logs);
    public AuditLog? Get(Guid id) => Logs.FirstOrDefault(l => l.IdLog == id);
    public ObservableCollection<AuditLog> Get(AuditLog e) => new(Logs);
    public bool Add(AuditLog e) { Logs.Add(e); return true; }
    public bool Modify(AuditLog e) => true;
    public bool Remove(AuditLog e) => Logs.Remove(e);
    public ObservableCollection<AuditLog> GetPage(int p, int s) => new(Logs.Skip(p * s).Take(s).ToList());
    public int Count() => Logs.Count;
    public ObservableCollection<AuditLog> GetByDateRange(DateTime f, DateTime t) => new();
    public ObservableCollection<AuditLog> GetByAction(string a) => new(Logs.Where(l => l.Action == a).ToList());
    public ObservableCollection<AuditLog> GetByTargetEntity(string et, string eid) => new();
}
