using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public sealed class RuntimePreflightAsyncJobTests
{
    [Fact]
    public async Task Async_pipeline_maps_runtime_preflight_failure_to_failed_outcome()
    {
        var launcher = StubLauncher.RuntimePreflightFails("heat-equation-amplitude", "heat-equation");
        var pipeline = new SystemMtAsyncPipeline(launcher);

        var outcome = await pipeline.ExecuteJobAsync(
            Guid.NewGuid(),
            new SystemMtJobRequest("heat-equation-amplitude"),
            progress: null,
            cancellationToken: default);

        Assert.Equal(SystemMtJobState.Failed, outcome.FinalState);
        Assert.Equal("heat-equation", outcome.SutName);
        Assert.Null(outcome.Result);
        Assert.Contains("Runtime preflight failed", outcome.FailureReason);
        Assert.Equal(1, launcher.RunCalls);
    }

    [Fact]
    public async Task Job_worker_persists_runtime_preflight_failure_as_failed_status_without_result()
    {
        var store = new InMemoryJobStore();
        var jobId = Guid.NewGuid();
        await store.CreateAsync(JobsTestData.Record(jobId, "heat-equation-amplitude", "heat-equation"), default);
        var launcher = StubLauncher.RuntimePreflightFails("heat-equation-amplitude", "heat-equation");
        var worker = new SystemMtJobWorker(store, new SystemMtAsyncPipeline(launcher));

        await worker.RunJobAsync(jobId, default);

        var status = await store.GetAsync(jobId, default);
        Assert.NotNull(status);
        Assert.Equal(SystemMtJobState.Failed, status!.State);
        Assert.Contains("Runtime preflight failed", status.FailureReason);
        Assert.Equal("heat-equation", status.SutName);
        Assert.Null(await store.GetResultAsync(jobId, default));
        Assert.Equal(1, launcher.RunCalls);
    }

    private sealed class StubLauncher : ISystemMtLauncher
    {
        private readonly MrSummary _summary;
        private readonly MrRunResult _result;

        private StubLauncher(MrSummary summary, MrRunResult result)
        {
            _summary = summary;
            _result = result;
        }

        public int RunCalls { get; private set; }

        public static StubLauncher RuntimePreflightFails(string mrId, string sutName)
        {
            var summary = new MrSummary(
                Id: mrId,
                DisplayName: mrId,
                SutName: sutName,
                TransformationName: "ScaleField",
                AssertionName: "GreaterThan",
                ValueName: "max_u",
                DefaultParameters: new Dictionary<string, string>(),
                Description: "test");
            var result = new MrRunResult(
                RecordId: Guid.NewGuid().ToString(),
                MrId: mrId,
                Passed: false,
                FailureReason: "Runtime preflight failed: missing dependency",
                ValueName: "max_u",
                SourceValue: 0,
                FollowUpValue: 0,
                SourceElapsed: TimeSpan.Zero,
                FollowUpElapsed: TimeSpan.Zero);
            return new StubLauncher(summary, result);
        }

        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MrSummary>>(new[] { _summary });

        public Task<MrRunResult> RunAsync(
            string mrId,
            IReadOnlyDictionary<string, string>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            RunCalls++;
            return Task.FromResult(_result);
        }

        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(
            IReadOnlyList<BatchMrRunRequest> requests,
            IProgress<BatchProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MrRunResult>>(Array.Empty<MrRunResult>());
    }
}
