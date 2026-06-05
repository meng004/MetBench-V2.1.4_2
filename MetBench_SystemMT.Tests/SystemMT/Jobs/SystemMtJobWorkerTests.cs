using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class SystemMtJobWorkerTests
{
    private static (InMemoryJobStore store, Guid id) Seed(string mrId = "mr", string sut = "openmc")
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        store.CreateAsync(JobsTestData.Record(id, mrId, sut), default).GetAwaiter().GetResult();
        return (store, id);
    }

    [Fact]
    public async Task Success_path_reaches_Succeeded_and_saves_result()
    {
        var (store, id) = Seed("mr-ok");
        var worker = new SystemMtJobWorker(store, FakeAsyncPipeline.Succeeds("mr-ok", "openmc"));

        await worker.RunJobAsync(id, default);

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Succeeded, rec!.State);
        Assert.Equal(100, rec.ProgressPercent);
        Assert.NotNull(rec.FinishedAtUtc);
        Assert.NotNull(await store.GetResultAsync(id, default));
    }

    [Fact]
    public async Task Success_path_backfills_sut_name_from_outcome()
    {
        var (store, id) = Seed("mr-ok", sut: "");   // record starts with empty SUT
        var worker = new SystemMtJobWorker(store, FakeAsyncPipeline.Succeeds("mr-ok", "openmc"));

        await worker.RunJobAsync(id, default);

        var rec = await store.GetAsync(id, default);
        Assert.Equal("openmc", rec!.SutName);
    }

    [Fact]
    public async Task Timeout_path_reaches_TimedOut_with_reason()
    {
        var (store, id) = Seed();
        var worker = new SystemMtJobWorker(store, FakeAsyncPipeline.TimesOut("openmc"));
        await worker.RunJobAsync(id, default);
        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.TimedOut, rec!.State);
        Assert.False(string.IsNullOrWhiteSpace(rec.FailureReason));
        Assert.Null(await store.GetResultAsync(id, default));
    }

    [Fact]
    public async Task ArtifactMissing_path_does_not_report_Succeeded_nor_result()
    {
        var (store, id) = Seed();
        var worker = new SystemMtJobWorker(store, FakeAsyncPipeline.ArtifactMissing("openmc"));
        await worker.RunJobAsync(id, default);
        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.ArtifactMissing, rec!.State);
        Assert.Null(await store.GetResultAsync(id, default));
    }

    [Fact]
    public async Task Pipeline_throwing_infra_error_reaches_Failed()
    {
        var (store, id) = Seed();
        var worker = new SystemMtJobWorker(store, new ThrowingPipeline("boom"));
        await worker.RunJobAsync(id, default);
        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Failed, rec!.State);
        Assert.Contains("boom", rec.FailureReason);
    }

    [Fact]
    public async Task Cancellation_reaches_Cancelled()
    {
        var (store, id) = Seed();
        var gated = FakeAsyncPipeline.Succeeds("mr", "openmc", gated: true);
        var worker = new SystemMtJobWorker(store, gated);
        using var cts = new CancellationTokenSource();

        var run = worker.RunJobAsync(id, cts.Token);
        cts.Cancel();
        gated.Gate!.TrySetResult();
        await run;

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Cancelled, rec!.State);
    }

    [Fact]
    public async Task Internal_OCE_unrelated_to_our_token_is_Failed_not_Cancelled()
    {
        var (store, id) = Seed();
        var worker = new SystemMtJobWorker(store, new InternalOceThrowingPipeline());

        await worker.RunJobAsync(id, default);   // our token is never cancelled

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Failed, rec!.State);
    }

    [Fact]
    public async Task Pipeline_returning_non_terminal_state_is_coerced_to_Failed()
    {
        var (store, id) = Seed();
        var worker = new SystemMtJobWorker(store, new NonTerminalPipeline());

        await worker.RunJobAsync(id, default);

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Failed, rec!.State);
        Assert.Contains("non-terminal", rec.FailureReason);
    }

    [Fact]
    public async Task Failure_preserves_last_reported_progress_percent()
    {
        var (store, id) = Seed();
        var worker = new SystemMtJobWorker(store, new ProgressThenThrowPipeline(40));

        await worker.RunJobAsync(id, default);

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Failed, rec!.State);
        Assert.Equal(40, rec.ProgressPercent);   // not reset to 0
    }

    [Fact]
    public async Task Concurrent_cancel_during_run_does_not_orphan_result_nor_overwrite_terminal()
    {
        // P1-orphan: a cancel lands (store → Cancelled) WHILE the pipeline is running and the
        // launcher completes anyway (cooperative cancel the launcher ignored). The worker must
        // re-read, see the terminal Cancelled state, and skip SaveResult + Finalize.
        var (store, id) = Seed("mr");
        // pipeline marks the store Cancelled mid-run, then returns Succeeded with a result
        var pipeline = new CancelMidRunThenSucceedPipeline(store, id);
        var worker = new SystemMtJobWorker(store, pipeline);

        await worker.RunJobAsync(id, default);

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Cancelled, rec!.State);          // terminal preserved
        Assert.Null(await store.GetResultAsync(id, default));         // no orphaned result
    }

    [Fact]
    public async Task Registry_cancel_propagates_to_pipeline_token_and_reaches_Cancelled()
    {
        // P1-cancel: registry.Cancel(jobId) trips the per-job token the worker links into the
        // pipeline, so a running job is actually interrupted (not just store-marked).
        var (store, id) = Seed("mr");
        var registry = new JobCancellationRegistry();
        var pipeline = new WaitForTokenPipeline();
        var worker = new SystemMtJobWorker(store, pipeline, registry);

        var run = worker.RunJobAsync(id, default);
        await pipeline.Started.Task;     // pipeline is now awaiting the linked token
        registry.Cancel(id);             // user cancel → trips the token
        await run;

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Cancelled, rec!.State);
    }

    [Fact]
    public async Task RunBatch_job_uses_operation_dispatcher_and_persists_per_mr_summary()
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        await store.CreateAsync(JobsTestData.Record(id, "mr-a") with
        {
            Kind = SystemMtJobKind.RunBatch,
            BatchItems = new[]
            {
                new SystemMtBatchJobItem("mr-a", SystemMtBatchItemState.Pending),
                new SystemMtBatchJobItem("mr-b", SystemMtBatchItemState.Pending),
            },
        }, default);
        var handler = new RunBatchJobOperationHandler(new WorkerBatchLauncher());
        var worker = new SystemMtJobWorker(
            store,
            FakeAsyncPipeline.Succeeds("unused", "sut"),
            operationDispatcher: handler);

        await worker.RunJobAsync(id, default);

        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Succeeded, rec!.State);
        Assert.Equal(2, rec.BatchItems.Count);
        Assert.Equal(SystemMtBatchItemState.Succeeded, rec.BatchItems[0].State);
        Assert.Equal(SystemMtBatchItemState.Failed, rec.BatchItems[1].State);
        Assert.Equal("assertion failed", rec.BatchItems[1].FailureReason);
        Assert.Null(await store.GetResultAsync(id, default));
    }

    [Fact]
    public async Task Terminal_queued_operation_job_is_not_dispatched()
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        await store.CreateAsync(JobsTestData.Record(id, "mr-a") with
        {
            Kind = SystemMtJobKind.RunBatch,
            State = SystemMtJobState.Cancelled,
            CurrentPhase = "cancelled",
            FailureReason = "cancellation requested",
            BatchItems = new[]
            {
                new SystemMtBatchJobItem("mr-a", SystemMtBatchItemState.Cancelled, "cancellation requested"),
            },
        }, default);
        var dispatcher = new CountingOperationDispatcher();
        var worker = new SystemMtJobWorker(
            store,
            FakeAsyncPipeline.Succeeds("unused", "sut"),
            operationDispatcher: dispatcher);

        await worker.RunJobAsync(id, default);

        Assert.Equal(0, dispatcher.CallCount);
        var rec = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Cancelled, rec!.State);
        Assert.Equal("cancellation requested", rec.FailureReason);
    }

    [Fact]
    public async Task Worker_releases_registry_entry_after_run()
    {
        var (store, id) = Seed("mr-ok");
        var registry = new JobCancellationRegistry();
        var worker = new SystemMtJobWorker(store, FakeAsyncPipeline.Succeeds("mr-ok", "openmc"), registry);

        await worker.RunJobAsync(id, default);

        // after release, a late cancel must not affect a freshly registered token
        registry.Cancel(id);
        Assert.False(registry.Register(id).IsCancellationRequested);
    }

    private sealed class CancelMidRunThenSucceedPipeline : ISystemMtAsyncPipeline
    {
        private readonly InMemoryJobStore _store;
        private readonly Guid _id;
        public CancelMidRunThenSucceedPipeline(InMemoryJobStore store, Guid id) { _store = store; _id = id; }
        public async Task<JobExecutionOutcome> ExecuteJobAsync(
            Guid jobId, SystemMtJobRequest request,
            IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
        {
            var rec = await _store.GetAsync(_id, default);
            await _store.UpdateStatusAsync(rec! with
            {
                State = SystemMtJobState.Cancelled,
                CurrentPhase = "cancelled",
                FailureReason = "cancellation requested",
            }, default);
            return new JobExecutionOutcome(SystemMtJobState.Succeeded, "openmc",
                JobsTestData.Result("mr", passed: true), null);
        }
    }

    private sealed class WaitForTokenPipeline : ISystemMtAsyncPipeline
    {
        public readonly TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<JobExecutionOutcome> ExecuteJobAsync(
            Guid jobId, SystemMtJobRequest request,
            IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);   // throws OCE when token trips
            return new JobExecutionOutcome(SystemMtJobState.Succeeded, "openmc", null, null);
        }
    }

    private sealed class WorkerBatchLauncher : ISystemMtLauncher
    {
        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MrSummary>>(Array.Empty<MrSummary>());

        public Task<MrRunResult> RunAsync(
            string mrId,
            IReadOnlyDictionary<string, string>? parameterOverrides = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(JobsTestData.Result(mrId, passed: true));

        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(
            IReadOnlyList<BatchMrRunRequest> requests,
            IProgress<BatchProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new[]
            {
                JobsTestData.Result("mr-a", passed: true),
                JobsTestData.Result("mr-b", passed: false, "assertion failed"),
            };
            for (var i = 0; i < results.Length; i++)
            {
                progress?.Report(new BatchProgress(i, results.Length, results[i].MrId, null));
                progress?.Report(new BatchProgress(i + 1, results.Length, results[i].MrId, results[i]));
            }

            return Task.FromResult<IReadOnlyList<MrRunResult>>(results);
        }
    }

    private sealed class CountingOperationDispatcher : ISystemMtJobOperationDispatcher
    {
        public int CallCount { get; private set; }

        public Task<JobExecutionOutcome> ExecuteAsync(
            Guid jobId,
            SystemMtJobRecord record,
            IProgress<SystemMtJobProgress>? progress,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new JobExecutionOutcome(
                SystemMtJobState.Succeeded,
                record.SutName,
                Result: null,
                FailureReason: null));
        }
    }

    private sealed class ThrowingPipeline : ISystemMtAsyncPipeline
    {
        private readonly string _message;
        public ThrowingPipeline(string message) => _message = message;
        public Task<JobExecutionOutcome> ExecuteJobAsync(
            Guid jobId, SystemMtJobRequest request,
            IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
            => throw new InvalidOperationException(_message);
    }

    private sealed class InternalOceThrowingPipeline : ISystemMtAsyncPipeline
    {
        public Task<JobExecutionOutcome> ExecuteJobAsync(
            Guid jobId, SystemMtJobRequest request,
            IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
        {
            using var internalCts = new CancellationTokenSource();
            internalCts.Cancel();
            throw new OperationCanceledException(internalCts.Token);   // unrelated token
        }
    }

    private sealed class NonTerminalPipeline : ISystemMtAsyncPipeline
    {
        public Task<JobExecutionOutcome> ExecuteJobAsync(
            Guid jobId, SystemMtJobRequest request,
            IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
            => Task.FromResult(new JobExecutionOutcome(SystemMtJobState.Asserting, "openmc", null, null));
    }

    private sealed class ProgressThenThrowPipeline : ISystemMtAsyncPipeline
    {
        private readonly int _percent;
        public ProgressThenThrowPipeline(int percent) => _percent = percent;
        public Task<JobExecutionOutcome> ExecuteJobAsync(
            Guid jobId, SystemMtJobRequest request,
            IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
        {
            progress?.Report(new SystemMtJobProgress(SystemMtJobState.RunningSource, "running-source", _percent));
            throw new InvalidOperationException("boom mid-run");
        }
    }
}
