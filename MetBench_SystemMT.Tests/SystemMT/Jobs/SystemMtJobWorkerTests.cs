using MetBench_BLL.SystemMT.Jobs;
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
