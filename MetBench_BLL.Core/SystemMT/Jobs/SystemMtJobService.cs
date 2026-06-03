using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 默认 job service。Submit 落 <see cref="SystemMtJobState.Queued"/> + 入队即返回；
/// polling 只读 store。<see cref="CancelAsync"/> 既标记 store Cancelled，又（若注入了
/// <see cref="IJobCancellationRegistry"/>）触发运行中 worker 的 per-job token，使取消真正中断
/// 在跑的 SUT，而不只是翻转记录。
/// </summary>
public sealed class SystemMtJobService : ISystemMtJobService
{
    private readonly IJobStore _store;
    private readonly IJobQueue _queue;
    private readonly IJobCancellationRegistry? _cancellation;
    private readonly Func<DateTime> _utcNow;

    public SystemMtJobService(
        IJobStore store,
        IJobQueue queue,
        IJobCancellationRegistry? cancellation = null,
        Func<DateTime>? utcNow = null)
    {
        _store = store;
        _queue = queue;
        _cancellation = cancellation;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public async Task<SystemMtJobHandle> SubmitAsync(SystemMtJobRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.MrId))
            throw new ArgumentException("MrId must be non-blank.", nameof(request));

        var now = _utcNow();
        var id = Guid.NewGuid();
        var record = new SystemMtJobRecord
        {
            JobId = id,
            MrId = request.MrId,
            SutName = string.Empty,   // worker 解析 MR → SUT 后回填（MrSummary.SutName）
            State = SystemMtJobState.Queued,
            CurrentPhase = "queued",
            ProgressPercent = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        await _store.CreateAsync(record, cancellationToken);

        try
        {
            await _queue.EnqueueAsync(id, cancellationToken);
        }
        catch
        {
            // The record is already persisted Queued; if enqueue fails (e.g. queue closed at
            // shutdown) no worker will ever pick it up. Mark it Failed rather than leaving a
            // phantom Queued record that polls forever as "waiting" (接 §6 显式报错), then rethrow.
            var failedAt = _utcNow();
            await _store.UpdateStatusAsync(record with
            {
                State = SystemMtJobState.Failed,
                FailureReason = "failed to enqueue job for execution",
                CurrentPhase = "failed",
                UpdatedAtUtc = failedAt,
                FinishedAtUtc = failedAt,
            }, CancellationToken.None);
            throw;
        }

        return new SystemMtJobHandle(id, now);
    }

    public async Task<SystemMtJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
        => (await _store.GetAsync(jobId, cancellationToken))?.ToStatus();

    public Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken = default)
        => _store.GetResultAsync(jobId, cancellationToken);

    public async Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var rec = await _store.GetAsync(jobId, cancellationToken);
        if (rec is null || rec.State.IsTerminal()) return;

        // Interrupt the running worker first (co-operative), then mark the durable record. The
        // worker's re-read guard + the store's terminal-immutable invariant keep the two consistent
        // regardless of which lands first.
        _cancellation?.Cancel(jobId);

        var now = _utcNow();
        await _store.UpdateStatusAsync(rec with
        {
            State = SystemMtJobState.Cancelled,
            FailureReason = "cancellation requested",
            CurrentPhase = "cancelled",
            UpdatedAtUtc = now,
            FinishedAtUtc = now,
        }, cancellationToken);
    }
}
