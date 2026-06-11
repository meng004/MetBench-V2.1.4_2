using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class SystemMtAsyncPipelineTests
{
    /// <summary>Stub launcher: one known MR-on-SUT; RunAsync returns a configurable result.</summary>
    private sealed class StubLauncher : ISystemMtLauncher
    {
        private readonly MrSummary _summary;
        private readonly Func<string, MrRunResult> _run;
        public string? LastMrId;

        public StubLauncher(MrSummary summary, Func<string, MrRunResult> run)
        {
            _summary = summary;
            _run = run;
        }

        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrSummary>>(new[] { _summary });

        public Task<MrRunResult> RunAsync(string mrId, IReadOnlyDictionary<string, string>? ov = null, CancellationToken ct = default)
        {
            LastMrId = mrId;
            return Task.FromResult(_run(mrId));
        }

        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(IReadOnlyList<BatchMrRunRequest> r, IProgress<BatchProgress>? p = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrRunResult>>(Array.Empty<MrRunResult>());
    }

    private static MrSummary Summary(string id, string sut) =>
        new(id, id, sut, "transform", "assert", "k_eff",
            new Dictionary<string, string>(), "desc");

    private static MrRunResult Result(string mrId, bool passed) => new(
        RecordId: Guid.NewGuid().ToString(), MrId: mrId, Passed: passed, FailureReason: passed ? "" : "assertion failed",
        ValueName: "k_eff", SourceValue: 1.0, FollowUpValue: 1.0,
        SourceElapsed: TimeSpan.Zero, FollowUpElapsed: TimeSpan.Zero);

    [Fact]
    public async Task ExecuteJobAsync_delegates_to_launcher_and_resolves_sut_name()
    {
        var launcher = new StubLauncher(Summary("openmc-q-value", "openmc"), id => Result(id, passed: true));
        var pipeline = new SystemMtAsyncPipeline(launcher);

        var outcome = await pipeline.ExecuteJobAsync(
            Guid.NewGuid(), new SystemMtJobRequest("openmc-q-value"), null, default);

        Assert.Equal("openmc-q-value", launcher.LastMrId);
        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);
        Assert.Equal("openmc", outcome.SutName);          // resolved from MrSummary.SutName
        Assert.NotNull(outcome.Result);
    }

    [Fact]
    public async Task ExecuteJobAsync_keeps_failed_assertion_as_succeeded_job()
    {
        // spec §10: MR assertion failure is a result/anomaly path, NOT infra failure.
        var launcher = new StubLauncher(Summary("mr", "openmc"), id => Result(id, passed: false));
        var pipeline = new SystemMtAsyncPipeline(launcher);

        var outcome = await pipeline.ExecuteJobAsync(
            Guid.NewGuid(), new SystemMtJobRequest("mr"), null, default);

        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);   // infra ok
        Assert.NotNull(outcome.Result);
        Assert.False(outcome.Result!.Passed);                           // assertion still failed
    }

    [Fact]
    public async Task ExecuteJobAsync_unknown_mr_leaves_sut_name_empty_but_runs()
    {
        var launcher = new StubLauncher(Summary("other-mr", "openmc"), id => Result(id, passed: true));
        var pipeline = new SystemMtAsyncPipeline(launcher);

        var outcome = await pipeline.ExecuteJobAsync(
            Guid.NewGuid(), new SystemMtJobRequest("mr-not-in-catalog"), null, default);

        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);
        Assert.Equal(string.Empty, outcome.SutName);
    }

    [Fact]
    public async Task ExecuteJobAsync_passes_parameter_overrides_through()
    {
        IReadOnlyDictionary<string, string>? seen = null;
        var launcher = new ParamCapturingLauncher(Summary("mr", "openmc"), ov => seen = ov);
        var pipeline = new SystemMtAsyncPipeline(launcher);
        var overrides = new Dictionary<string, string> { ["factor"] = "4" };

        await pipeline.ExecuteJobAsync(Guid.NewGuid(), new SystemMtJobRequest("mr", overrides), null, default);

        Assert.NotNull(seen);
        Assert.Equal("4", seen!["factor"]);
    }

    [Fact]
    public async Task ExecuteJobAsync_with_backend_key_fails_closed_without_docker_or_ssh_executor()
    {
        var launcher = new StubLauncher(Summary("mr", "mgn"), id => Result(id, passed: true));
        var pipeline = new SystemMtAsyncPipeline(launcher);

        var outcome = await pipeline.ExecuteJobAsync(
            Guid.NewGuid(),
            new SystemMtJobRequest("mr", RuntimeBackendKey: "sciml-mgn-docker"),
            null,
            default);

        Assert.Equal(SystemMtJobState.Failed, outcome.FinalState);
        Assert.Equal("MiddlewareUnavailable", outcome.FailureKind);
        Assert.Contains("backend executor", outcome.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.Null(launcher.LastMrId);
    }

    private sealed class ParamCapturingLauncher : ISystemMtLauncher
    {
        private readonly MrSummary _summary;
        private readonly Action<IReadOnlyDictionary<string, string>?> _capture;
        public ParamCapturingLauncher(MrSummary summary, Action<IReadOnlyDictionary<string, string>?> capture)
        { _summary = summary; _capture = capture; }
        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrSummary>>(new[] { _summary });
        public Task<MrRunResult> RunAsync(string mrId, IReadOnlyDictionary<string, string>? ov = null, CancellationToken ct = default)
        { _capture(ov); return Task.FromResult(Result(mrId, true)); }
        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(IReadOnlyList<BatchMrRunRequest> r, IProgress<BatchProgress>? p = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrRunResult>>(Array.Empty<MrRunResult>());
    }
}
