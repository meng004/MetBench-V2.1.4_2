namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 后台 worker：取一个 jobId，调 async pipeline，把进度 + 终止态写 store。
/// 状态机推进是确定性代码（接 CLAUDE.md §1.3）：终止态由 pipeline outcome / 取消 / 异常决定，
/// 不交给模型判断。异常不外抛 —— 转成 Failed 记录（fail closed，spec §10）。
/// </summary>
public sealed class SystemMtJobWorker
{
    private readonly IJobStore _store;
    private readonly ISystemMtAsyncPipeline _pipeline;
    private readonly Func<DateTime> _utcNow;

    public SystemMtJobWorker(IJobStore store, ISystemMtAsyncPipeline pipeline, Func<DateTime>? utcNow = null)
    {
        _store = store;
        _pipeline = pipeline;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public async Task RunJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var record = await _store.GetAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException($"Job {jobId} not found in store.");

        // Inline (synchronous, ordered) progress: the handler runs on the pipeline's
        // calling thread before ExecuteJobAsync continues, so intermediate progress writes
        // strictly precede the terminal Finalize write. A default Progress<T> would post to
        // the thread pool and could land AFTER Finalize, corrupting the terminal state.
        var progress = new InlineProgress<SystemMtJobProgress>(p =>
            _store.UpdateStatusAsync(record with
            {
                State = p.State,
                CurrentPhase = p.Phase,
                ProgressPercent = p.ProgressPercent,
                UpdatedAtUtc = _utcNow(),
            }, CancellationToken.None).GetAwaiter().GetResult());

        try
        {
            var outcome = await _pipeline.ExecuteJobAsync(jobId, ToRequest(record), progress, cancellationToken);

            if (outcome.FinalState == SystemMtJobState.Succeeded && outcome.Result is not null)
                await _store.SaveResultAsync(jobId, outcome.Result, CancellationToken.None);

            await FinalizeAsync(record, outcome.FinalState, outcome.SutName, outcome.FailureReason);
        }
        catch (OperationCanceledException)
        {
            await FinalizeAsync(record, SystemMtJobState.Cancelled, record.SutName, "cancellation requested");
        }
        catch (Exception ex)
        {
            await FinalizeAsync(record, SystemMtJobState.Failed, record.SutName, ex.Message);
        }
    }

    private static SystemMtJobRequest ToRequest(SystemMtJobRecord r) => new(r.MrId);

    private Task FinalizeAsync(SystemMtJobRecord record, SystemMtJobState state, string sutName, string? reason)
    {
        var now = _utcNow();
        return _store.UpdateStatusAsync(record with
        {
            State = state,
            SutName = string.IsNullOrEmpty(sutName) ? record.SutName : sutName,
            FailureReason = state == SystemMtJobState.Succeeded ? null : reason,
            ProgressPercent = state == SystemMtJobState.Succeeded ? 100 : record.ProgressPercent,
            CurrentPhase = state.ToString().ToLowerInvariant(),
            UpdatedAtUtc = now,
            FinishedAtUtc = now,
        }, CancellationToken.None);
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        public InlineProgress(Action<T> handler) => _handler = handler;
        public void Report(T value) => _handler(value);
    }
}
