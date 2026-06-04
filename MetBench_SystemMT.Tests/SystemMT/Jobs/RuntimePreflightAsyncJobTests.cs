using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public sealed class RuntimePreflightAsyncJobTests
{
    [Fact]
    public async Task Async_pipeline_maps_runtime_preflight_failure_to_failed_outcome()
    {
        var executionId = Guid.NewGuid();
        var launcher = StubLauncher.RuntimePreflightFails(
            "heat-equation-amplitude",
            "heat-equation",
            executionId,
            "dependency missing without legacy prefix");
        var evidence = new StubEvidenceRepository(executionId, RuntimeFailureKind.DependencyMissing.ToString());
        var pipeline = new SystemMtAsyncPipeline(launcher, evidence);

        var outcome = await pipeline.ExecuteJobAsync(
            Guid.NewGuid(),
            new SystemMtJobRequest("heat-equation-amplitude"),
            progress: null,
            cancellationToken: default);

        Assert.Equal(SystemMtJobState.Failed, outcome.FinalState);
        Assert.Equal("heat-equation", outcome.SutName);
        Assert.Null(outcome.Result);
        Assert.Equal("dependency missing without legacy prefix", outcome.FailureReason);
        Assert.Equal(RuntimeFailureKind.DependencyMissing.ToString(), outcome.FailureKind);
        Assert.Equal(1, launcher.RunCalls);
    }

    [Fact]
    public async Task Job_worker_persists_runtime_preflight_failure_as_failed_status_without_result()
    {
        var store = new InMemoryJobStore();
        var jobId = Guid.NewGuid();
        await store.CreateAsync(JobsTestData.Record(jobId, "heat-equation-amplitude", "heat-equation"), default);
        var worker = new SystemMtJobWorker(
            store,
            new StaticFailedPipeline(RuntimeFailureKind.DependencyMissing.ToString()));

        await worker.RunJobAsync(jobId, default);

        var record = await store.GetAsync(jobId, default);
        Assert.NotNull(record);
        Assert.Equal(SystemMtJobState.Failed, record!.State);
        Assert.Equal("missing dependency", record.FailureReason);
        Assert.Equal(RuntimeFailureKind.DependencyMissing.ToString(), record.FailureKind);
        Assert.Equal(RuntimeFailureKind.DependencyMissing.ToString(), record.ToStatus().FailureKind);
        Assert.Null(await store.GetResultAsync(jobId, default));
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

        public static StubLauncher RuntimePreflightFails(
            string mrId,
            string sutName,
            Guid executionId,
            string failureReason)
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
                RecordId: executionId.ToString(),
                MrId: mrId,
                Passed: false,
                FailureReason: failureReason,
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

    private sealed class StubEvidenceRepository : IExecutionEvidenceRepository
    {
        private readonly Guid _executionId;
        private readonly string _failureKind;

        public StubEvidenceRepository(Guid executionId, string failureKind)
        {
            _executionId = executionId;
            _failureKind = failureKind;
        }

        public Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
        {
            if (executionId != _executionId)
                return Task.FromResult<ExecutionEvidence?>(null);

            return Task.FromResult<ExecutionEvidence?>(new ExecutionEvidence
            {
                ExecutionId = executionId,
                RuntimeEvidence = new RuntimeEvidence
                {
                    Passed = false,
                    FailureKind = _failureKind,
                    FailureDetail = "missing dependency",
                },
            });
        }

        public Task<bool> DeleteByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class StaticFailedPipeline : ISystemMtAsyncPipeline
    {
        private readonly string _failureKind;

        public StaticFailedPipeline(string failureKind) => _failureKind = failureKind;

        public Task<JobExecutionOutcome> ExecuteJobAsync(
            Guid jobId,
            SystemMtJobRequest request,
            IProgress<SystemMtJobProgress>? progress,
            CancellationToken cancellationToken) =>
            Task.FromResult(new JobExecutionOutcome(
                SystemMtJobState.Failed,
                "heat-equation",
                Result: null,
                FailureReason: "missing dependency",
                FailureKind: _failureKind));
    }
}
