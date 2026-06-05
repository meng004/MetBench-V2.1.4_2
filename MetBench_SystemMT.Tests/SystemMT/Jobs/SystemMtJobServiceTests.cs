using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class SystemMtJobServiceTests
{
    private static SystemMtJobService Build(IJobStore store, IJobQueue queue) => new(store, queue);

    [Fact]
    public async Task CancelAsync_signals_cancellation_registry_and_marks_store()
    {
        var store = new InMemoryJobStore();
        var registry = new JobCancellationRegistry();
        var svc = new SystemMtJobService(store, new ChannelJobQueue(), registry);
        var handle = await svc.SubmitAsync(new SystemMtJobRequest("mr"), default);
        var workerToken = registry.Register(handle.JobId);   // the running worker's token

        await svc.CancelAsync(handle.JobId, default);

        Assert.True(workerToken.IsCancellationRequested);     // worker is actually interrupted
        var status = await svc.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Cancelled, status!.State);   // store also flipped
    }

    [Fact]
    public async Task SubmitAsync_persists_Queued_and_enqueues_before_worker_runs()
    {
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var svc = Build(store, queue);

        var handle = await svc.SubmitAsync(new SystemMtJobRequest("openmc-q-value"), default);

        Assert.NotEqual(Guid.Empty, handle.JobId);
        var status = await svc.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Queued, status!.State);
        Assert.Equal("openmc-q-value", status.MrId);
        // 队列里确有该 id（worker 尚未起）
        Assert.Equal(handle.JobId, await queue.DequeueAsync(default));
    }

    [Fact]
    public async Task SubmitAsync_old_mr_request_persists_run_mr_job_kind()
    {
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var svc = Build(store, queue);

        var handle = await svc.SubmitAsync(new SystemMtJobRequest("openmc-q-value"), default);

        var record = await store.GetAsync(handle.JobId, default);
        var status = await svc.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobKind.RunMr, record!.Kind);
        Assert.Equal(SystemMtJobKind.RunMr, status!.Kind);
        Assert.Equal("openmc-q-value", status.MrId);
    }

    [Fact]
    public async Task SubmitOperationAsync_rejects_export_without_export_root()
    {
        var svc = Build(new InMemoryJobStore(), new ChannelJobQueue());
        var request = new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportAssets,
            PackageRoot: "/tmp/source-package");

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SubmitOperationAsync(request, default));

        Assert.Contains("ExportRoot", ex.Message);
    }

    [Fact]
    public async Task SubmitOperationAsync_rejects_execution_artifact_export_until_operation_handler_exists()
    {
        var svc = Build(new InMemoryJobStore(), new ChannelJobQueue());
        var request = new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportExecutionEvidence,
            ExecutionId: Guid.NewGuid(),
            ExportRoot: "/tmp/export");

        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => svc.SubmitOperationAsync(request, default));

        Assert.Contains("matching operation handler", ex.Message);
    }

    [Fact]
    public async Task SubmitOperationAsync_rejects_empty_batch_mr_ids()
    {
        var svc = Build(new InMemoryJobStore(), new ChannelJobQueue());
        var request = new SystemMtOperationJobRequest(
            SystemMtJobKind.RunBatch,
            MrIds: Array.Empty<string>());

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SubmitOperationAsync(request, default));

        Assert.Contains("MrIds", ex.Message);
    }

    [Fact]
    public async Task GetStatusAsync_reads_store_snapshot_only()
    {
        var store = new InMemoryJobStore();
        var svc = Build(store, new ChannelJobQueue());
        var handle = await svc.SubmitAsync(new SystemMtJobRequest("mr"), default);

        var rec = await store.GetAsync(handle.JobId, default);
        await store.UpdateStatusAsync(rec! with { State = SystemMtJobState.RunningSource }, default);

        var status = await svc.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.RunningSource, status!.State);
    }

    [Fact]
    public async Task GetResultAsync_returns_persisted_result()
    {
        var store = new InMemoryJobStore();
        var svc = Build(store, new ChannelJobQueue());
        var handle = await svc.SubmitAsync(new SystemMtJobRequest("mr-pass"), default);
        await store.SaveResultAsync(handle.JobId, JobsTestData.Result("mr-pass", passed: true), default);

        var result = await svc.GetResultAsync(handle.JobId, default);
        Assert.NotNull(result);
        Assert.True(result!.Passed);
    }

    [Fact]
    public async Task GetStatusAsync_unknown_job_returns_null()
        => Assert.Null(await Build(new InMemoryJobStore(), new ChannelJobQueue())
            .GetStatusAsync(Guid.NewGuid(), default));

    [Fact]
    public async Task SubmitAsync_blank_mrId_throws()
        => await Assert.ThrowsAsync<ArgumentException>(
            () => Build(new InMemoryJobStore(), new ChannelJobQueue())
                .SubmitAsync(new SystemMtJobRequest("  "), default));

    [Fact]
    public async Task CancelAsync_marks_non_terminal_job_cancelled()
    {
        var store = new InMemoryJobStore();
        var svc = Build(store, new ChannelJobQueue());
        var handle = await svc.SubmitAsync(new SystemMtJobRequest("mr"), default);

        await svc.CancelAsync(handle.JobId, default);

        var status = await svc.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Cancelled, status!.State);
    }

    [Fact]
    public async Task CancelAsync_marks_pending_batch_items_cancelled()
    {
        var store = new InMemoryJobStore();
        var svc = Build(store, new ChannelJobQueue());
        var handle = await svc.SubmitOperationAsync(
            new SystemMtOperationJobRequest(SystemMtJobKind.RunBatch, MrIds: new[] { "mr-a", "mr-b" }),
            default);

        await svc.CancelAsync(handle.JobId, default);

        var status = await svc.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Cancelled, status!.State);
        Assert.All(status.BatchItems!, item => Assert.Equal(SystemMtBatchItemState.Cancelled, item.State));
    }

    [Fact]
    public async Task SubmitAsync_marks_record_Failed_if_enqueue_throws()
    {
        var store = new InMemoryJobStore();
        var queue = new CapturingThrowingQueue();
        var svc = Build(store, queue);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.SubmitAsync(new SystemMtJobRequest("mr"), default));

        Assert.NotNull(queue.Captured);   // record was created + enqueue attempted
        var rec = await store.GetAsync(queue.Captured!.Value, default);
        Assert.Equal(SystemMtJobState.Failed, rec!.State);   // not a phantom Queued
    }

    private sealed class CapturingThrowingQueue : IJobQueue
    {
        public Guid? Captured;
        public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
        {
            Captured = jobId;
            throw new InvalidOperationException("queue closed");
        }
        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    [Fact]
    public async Task CancelAsync_does_not_overwrite_terminal_job()
    {
        var store = new InMemoryJobStore();
        var svc = Build(store, new ChannelJobQueue());
        var handle = await svc.SubmitAsync(new SystemMtJobRequest("mr"), default);
        var rec = await store.GetAsync(handle.JobId, default);
        await store.UpdateStatusAsync(rec! with { State = SystemMtJobState.Succeeded }, default);

        await svc.CancelAsync(handle.JobId, default);

        var status = await svc.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);   // unchanged
    }
}
