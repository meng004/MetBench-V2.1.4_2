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
    private readonly IJobCancellationRegistry? _cancellation;
    private readonly ISystemMtJobOperationDispatcher? _operationDispatcher;
    private readonly Func<DateTime> _utcNow;

    public SystemMtJobWorker(
        IJobStore store,
        ISystemMtAsyncPipeline pipeline,
        IJobCancellationRegistry? cancellation = null,
        Func<DateTime>? utcNow = null,
        ISystemMtJobOperationDispatcher? operationDispatcher = null)
    {
        _store = store;
        _pipeline = pipeline;
        _cancellation = cancellation;
        _operationDispatcher = operationDispatcher;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public async Task RunJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var record = await _store.GetAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException($"Job {jobId} not found in store.");

        var lastProgress = record.ProgressPercent;

        // P1-cancel: a per-job token, linked with the host (shutdown) token, lets
        // ISystemMtJobService.CancelAsync co-operatively interrupt the running pipeline instead of
        // only flipping the store record (which would leave the SUT running + orphan a result).
        var jobToken = _cancellation?.Register(jobId) ?? CancellationToken.None;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, jobToken);
        var runToken = linked.Token;

        // Inline (synchronous, ordered) progress: the handler runs on the pipeline's calling thread
        // before ExecuteJobAsync continues, so intermediate progress writes strictly precede the
        // terminal Finalize write (a default Progress<T> would post to the thread pool and could
        // land AFTER Finalize, corrupting the terminal state).
        var progress = new InlineProgress<SystemMtJobProgress>(p =>
        {
            lastProgress = p.ProgressPercent;
            _store.UpdateStatusAsync(record with
            {
                State = p.State,
                CurrentPhase = p.Phase,
                ProgressPercent = p.ProgressPercent,
                UpdatedAtUtc = _utcNow(),
                BatchItems = p.BatchItems ?? record.BatchItems,
            }, CancellationToken.None).GetAwaiter().GetResult();
        });

        try
        {
            var outcome = record.Kind == SystemMtJobKind.RunMr
                ? await _pipeline.ExecuteJobAsync(jobId, ToRequest(record), progress, runToken)
                : await ExecuteOperationAsync(jobId, record, progress, runToken);

            // P1-orphan: if a concurrent cancel (or any other writer) already drove the job to a
            // terminal state while the pipeline was running, honor it — do NOT save an orphaned
            // result nor try to overwrite the terminal state. (Terminal-immutable in the store also
            // blocks the overwrite, but skipping SaveResultAsync is what prevents the orphan.)
            var current = await _store.GetAsync(jobId, CancellationToken.None);
            if (current is not null && current.State.IsTerminal())
                return;

            var finalState = outcome.FinalState;
            var reason = outcome.FailureReason;
            var failureKind = outcome.FailureKind;

            if (!finalState.IsTerminal())
            {
                reason = $"pipeline returned non-terminal final state {outcome.FinalState}";
                failureKind = null;
                finalState = SystemMtJobState.Failed;
            }

            if (finalState == SystemMtJobState.Succeeded && outcome.Result is not null)
                await _store.SaveResultAsync(jobId, outcome.Result, CancellationToken.None);

            await FinalizeAsync(record, finalState, outcome.SutName, reason, failureKind, lastProgress, outcome);
        }
        catch (OperationCanceledException) when (runToken.IsCancellationRequested)
        {
            // Cancellation requested on OUR (host OR per-job) token. An OCE from an unrelated token
            // (e.g. a launcher-internal timeout source) leaves runToken un-cancelled and falls
            // through to the generic catch below → Failed, not silently Cancelled.
            await FinalizeAsync(record, SystemMtJobState.Cancelled, record.SutName, "cancellation requested", failureKind: null, lastProgress);
        }
        catch (Exception ex)
        {
            await FinalizeAsync(record, SystemMtJobState.Failed, record.SutName, ex.Message, failureKind: null, lastProgress);
        }
        finally
        {
            _cancellation?.Release(jobId);
        }
    }

    private Task<JobExecutionOutcome> ExecuteOperationAsync(
        Guid jobId,
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (_operationDispatcher is null)
        {
            return Task.FromResult(new JobExecutionOutcome(
                SystemMtJobState.Failed,
                record.SutName,
                Result: null,
                FailureReason: $"No operation dispatcher is registered for job kind {record.Kind}."));
        }

        return _operationDispatcher.ExecuteAsync(jobId, record, progress, cancellationToken);
    }

    private static SystemMtJobRequest ToRequest(SystemMtJobRecord r) => new(r.MrId, r.ParameterOverrides);

    private Task FinalizeAsync(
        SystemMtJobRecord record,
        SystemMtJobState state,
        string sutName,
        string? reason,
        string? failureKind,
        int lastProgress,
        JobExecutionOutcome? outcome = null)
    {
        var now = _utcNow();
        return _store.UpdateStatusAsync(record with
        {
            State = state,
            SutName = string.IsNullOrEmpty(sutName) ? record.SutName : sutName,
            FailureReason = state == SystemMtJobState.Succeeded ? null : reason,
            FailureKind = state == SystemMtJobState.Succeeded ? null : failureKind,
            ProgressPercent = state == SystemMtJobState.Succeeded ? 100 : lastProgress,
            CurrentPhase = state.ToString().ToLowerInvariant(),
            UpdatedAtUtc = now,
            FinishedAtUtc = now,
            ArtifactPath = outcome?.ArtifactPath ?? record.ArtifactPath,
            BatchItems = outcome?.BatchItems ?? record.BatchItems,
        }, CancellationToken.None);
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        public InlineProgress(Action<T> handler) => _handler = handler;
        public void Report(T value) => _handler(value);
    }
}
