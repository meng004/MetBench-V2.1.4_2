using System.Collections.ObjectModel;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pipeline;

/// <summary>
/// P1 — <see cref="SystemMtExecutionRecorder"/>：把一次 <c>PipelineOutcome</c> 投影为
/// <c>Execution</c> + <c>Result</c>，作为 v2 结果 schema 的统一写入口。
/// 计划见 docs/superpowers/plans/2026-05-22-systemmt-engine-unification-plan.md。
/// </summary>
public sealed class SystemMtExecutionRecorderTests
{
    [Fact]
    public void Record_ok_outcome_inserts_execution_and_result()
    {
        var (recorder, execs, results) = NewRecorder();

        var recorded = recorder.Record(
            MakeContext(), MakeOutcome(PipelineStatus.Ok, sourceV: 1.0, followupV: 1.5), mrInstanceId: 7);

        var exec = Assert.Single(execs.Data);
        var result = Assert.Single(results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(result.AssertionPassed);
        Assert.Equal(exec.IdExecution, result.ExecutionId);
        Assert.Equal(exec.IdExecution, recorded.ExecutionId);
        Assert.Equal(result.IdResult, recorded.ResultId);
        Assert.Equal(7, exec.MRInstanceId);
    }

    [Fact]
    public void Record_anomaly_outcome_inserts_failed_result()
    {
        var (recorder, execs, results) = NewRecorder();

        recorder.Record(
            MakeContext(), MakeOutcome(PipelineStatus.Anomaly, sourceV: 1.0, followupV: 0.4), mrInstanceId: 1);

        Assert.Equal("anomaly", Assert.Single(execs.Data).Status);
        var result = Assert.Single(results.Data);
        Assert.False(result.AssertionPassed);
        Assert.Equal("assertion-violated", result.FailureReason);
    }

    [Fact]
    public void Record_error_outcome_inserts_execution_without_result()
    {
        var (recorder, execs, results) = NewRecorder();

        var recorded = recorder.Record(MakeContext(), MakeErrorOutcome("SUT crashed"), mrInstanceId: 1);

        var exec = Assert.Single(execs.Data);
        Assert.Equal("error", exec.Status);
        Assert.Equal("SUT crashed", exec.ErrorMessage);
        Assert.Empty(results.Data);
        Assert.Null(recorded.ResultId);
    }

    [Fact]
    public void Record_copies_version_snapshot_and_triggeredby_from_context()
    {
        var (recorder, execs, _) = NewRecorder();
        var ctx = MakeContext(
            catalogSha: "abc123", sutVersion: "openmoc@2026-05",
            metbenchVersion: "v2.2", triggeredBy: "limeng");

        recorder.Record(ctx, MakeOutcome(PipelineStatus.Ok, 1.0, 1.0), mrInstanceId: 1);

        var exec = Assert.Single(execs.Data);
        Assert.Equal("abc123", exec.CatalogVersionSha);
        Assert.Equal("openmoc@2026-05", exec.SutVersionSnapshot);
        Assert.Equal("v2.2", exec.MetbenchVersion);
        Assert.Equal("limeng", exec.TriggeredBy);
    }

    [Fact]
    public void Record_maps_result_value_timing_and_assertion_fields()
    {
        var (recorder, _, results) = NewRecorder();

        recorder.Record(MakeContext(), MakeOutcome(PipelineStatus.Ok, sourceV: 2.0, followupV: 5.0), mrInstanceId: 1);

        var r = Assert.Single(results.Data);
        Assert.Equal(2.0, r.SourceValue);
        Assert.Equal(5.0, r.FollowupValue);
        Assert.Equal(3.0, r.ObservedDelta);
        Assert.Equal(TimeSpan.FromSeconds(1), r.SourceElapsed);
        Assert.Equal(0, r.SourceExitCode);
        Assert.Contains("k_eff", r.AssertionExpression);
    }

    [Fact]
    public void Record_null_arguments_throw()
    {
        var (recorder, _, _) = NewRecorder();

        Assert.Throws<ArgumentNullException>(() =>
            recorder.Record(null!, MakeOutcome(PipelineStatus.Ok, 1.0, 1.0), mrInstanceId: 1));
        Assert.Throws<ArgumentNullException>(() =>
            recorder.Record(MakeContext(), null!, mrInstanceId: 1));
    }

    // ===== fixtures =====

    private static (SystemMtExecutionRecorder Recorder, FakeExecRepo Execs, FakeResultRepo Results) NewRecorder()
    {
        var execs = new FakeExecRepo();
        var results = new FakeResultRepo();
        return (new SystemMtExecutionRecorder(execs, results), execs, results);
    }

    private static PipelineContext MakeContext(
        string catalogSha = "", string sutVersion = "", string metbenchVersion = "",
        string triggeredBy = "test")
        => new(
            MrCode: "MR-test", TransformationName: "ScaleField", AssertionTypeCode: "greater",
            ValueName: "k_eff", TargetFieldPath: "fuel.nu_sigma_f", PathSyntax: "json-pointer",
            Parameters: new Dictionary<string, string>(), Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: "openmoc", SourceCasePath: "/tmp/src.json", WorkingDirectory: "/tmp",
            InputParserCommand: "x", OutputParserCommand: "x", RunnerCommand: "x",
            TimeoutSeconds: 60,
            CatalogVersionSha: catalogSha, SutVersionSnapshot: sutVersion,
            MetbenchVersion: metbenchVersion, TriggeredBy: triggeredBy);

    private static PipelineOutcome MakeOutcome(string finalStatus, double sourceV, double followupV)
    {
        var passed = finalStatus == PipelineStatus.Ok;
        var input = new AssertionInput(sourceV, 0.0, followupV, 0.0, "k_eff", null);
        var assertion = passed
            ? SystemMtAssertionResultV2.PassedResult(input, "greater")
            : SystemMtAssertionResultV2.FailedResult(input, "greater", "assertion-violated");
        return new PipelineOutcome(
            FinalStatus: finalStatus, ErrorMessage: null,
            StartedAt: DateTime.UtcNow.AddSeconds(-5), FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: "/tmp/art",
            SourceInputPath: "/tmp/s.in", FollowupInputPath: "/tmp/f.in",
            SourceOutputPath: "/tmp/s.out", FollowupOutputPath: "/tmp/f.out",
            SourceMetrics: new Dictionary<string, double> { ["k_eff"] = sourceV },
            FollowupMetrics: new Dictionary<string, double> { ["k_eff"] = followupV },
            AssertionResult: assertion,
            SourceElapsed: TimeSpan.FromSeconds(1), FollowupElapsed: TimeSpan.FromSeconds(1),
            SourceExitCode: 0, FollowupExitCode: 0);
    }

    private static PipelineOutcome MakeErrorOutcome(string error)
        => new(
            FinalStatus: PipelineStatus.Error, ErrorMessage: error,
            StartedAt: DateTime.UtcNow.AddSeconds(-5), FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: "/tmp/art",
            SourceInputPath: "", FollowupInputPath: "", SourceOutputPath: "", FollowupOutputPath: "",
            SourceMetrics: null, FollowupMetrics: null, AssertionResult: null,
            SourceElapsed: TimeSpan.Zero, FollowupElapsed: TimeSpan.Zero,
            SourceExitCode: 0, FollowupExitCode: 0);
}

internal sealed class FakeExecRepo : IExecutionRepository
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

internal sealed class FakeResultRepo : IResultRepository
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
