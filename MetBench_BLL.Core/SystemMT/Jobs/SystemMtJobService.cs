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
            Kind = SystemMtJobKind.RunMr,
            MrId = request.MrId,
            ParameterOverrides = CopyOverrides(request.ParameterOverrides),
            SutName = string.Empty,   // worker 解析 MR → SUT 后回填（MrSummary.SutName）
            State = SystemMtJobState.Queued,
            CurrentPhase = "queued",
            ProgressPercent = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        return await CreateAndEnqueueAsync(record, now, cancellationToken);
    }

    public async Task<SystemMtJobHandle> SubmitOperationAsync(
        SystemMtOperationJobRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        ValidateOperation(request);

        var now = _utcNow();
        var id = Guid.NewGuid();
        var record = new SystemMtJobRecord
        {
            JobId = id,
            Kind = request.Kind,
            MrId = ResolvePrimaryMrId(request),
            ParameterOverrides = CopyOverrides(request.ParameterOverrides),
            State = SystemMtJobState.Queued,
            CurrentPhase = "queued",
            ProgressPercent = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PackageRoot = request.PackageRoot,
            StagingRoot = request.StagingRoot,
            ExportRoot = request.ExportRoot,
            ExecutionId = request.ExecutionId,
            ExecutionIds = request.ExecutionIds is { Count: > 0 }
                ? request.ExecutionIds.ToList()
                : null,
            BatchItems = request.Kind == SystemMtJobKind.RunBatch
                ? request.MrIds!.Select(id => new SystemMtBatchJobItem(id, SystemMtBatchItemState.Pending)).ToList()
                : Array.Empty<SystemMtBatchJobItem>(),
        };
        return await CreateAndEnqueueAsync(record, now, cancellationToken);
    }

    private async Task<SystemMtJobHandle> CreateAndEnqueueAsync(
        SystemMtJobRecord record,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        await _store.CreateAsync(record, cancellationToken);

        try
        {
            await _queue.EnqueueAsync(record.JobId, cancellationToken);
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

        return new SystemMtJobHandle(record.JobId, createdAtUtc);
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
            BatchItems = CancelPendingBatchItems(rec.BatchItems),
        }, cancellationToken);
    }

    private static void ValidateOperation(SystemMtOperationJobRequest request)
    {
        switch (request.Kind)
        {
            case SystemMtJobKind.RunMr:
                RequireNonBlank(request.MrId, nameof(request.MrId));
                break;
            case SystemMtJobKind.RunBatch:
                if (request.MrIds is null || request.MrIds.Count == 0)
                    throw new ArgumentException("MrIds must contain at least one MR id.", nameof(request));
                for (var i = 0; i < request.MrIds.Count; i++)
                    RequireNonBlank(request.MrIds[i], $"{nameof(request.MrIds)}[{i}]");
                break;
            case SystemMtJobKind.ImportAssets:
                RequireNonBlank(request.PackageRoot, nameof(request.PackageRoot));
                RequireNonBlank(request.StagingRoot, nameof(request.StagingRoot));
                break;
            case SystemMtJobKind.ExportAssets:
                RequireNonBlank(request.PackageRoot, nameof(request.PackageRoot));
                RequireNonBlank(request.ExportRoot, nameof(request.ExportRoot));
                break;
            case SystemMtJobKind.ExportExecutionArtifacts:
                ValidateExecutionExportTargets(request);
                RequireNonBlank(request.ExportRoot, nameof(request.ExportRoot));
                break;
            case SystemMtJobKind.ExportReport:
                if (request.ExecutionId is null || request.ExecutionId == Guid.Empty)
                    throw new ArgumentException("ExecutionId must be a non-empty Guid.", nameof(request));
                RequireNonBlank(request.ExportRoot, nameof(request.ExportRoot));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.Kind, "Unsupported job kind.");
        }
    }

    private static string ResolvePrimaryMrId(SystemMtOperationJobRequest request) =>
        request.Kind switch
        {
            SystemMtJobKind.RunMr => request.MrId!,
            SystemMtJobKind.RunBatch => request.MrIds![0],
            _ => string.Empty,
        };

    private static void RequireNonBlank(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} must be non-blank.", name);
    }

    /// <summary>
    /// Execution-artifact export accepts either a single <c>ExecutionId</c> or a non-empty
    /// <c>ExecutionIds</c> batch (each id non-empty). Exactly one mode must be supplied; the
    /// batch list takes precedence when both are present.
    /// </summary>
    private static void ValidateExecutionExportTargets(SystemMtOperationJobRequest request)
    {
        if (request.ExecutionIds is { Count: > 0 } ids)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == Guid.Empty)
                    throw new ArgumentException($"ExecutionIds[{i}] must be a non-empty Guid.", nameof(request));
            }
            return;
        }

        if (request.ExecutionId is null || request.ExecutionId == Guid.Empty)
            throw new ArgumentException(
                "Execution-artifact export requires a non-empty ExecutionId or a non-empty ExecutionIds batch.",
                nameof(request));
    }

    private static Dictionary<string, string>? CopyOverrides(IReadOnlyDictionary<string, string>? overrides) =>
        overrides is null ? null : new Dictionary<string, string>(overrides, StringComparer.Ordinal);

    private static IReadOnlyList<SystemMtBatchJobItem> CancelPendingBatchItems(
        IReadOnlyList<SystemMtBatchJobItem> items)
    {
        if (items.Count == 0)
            return items;

        return items
            .Select(item => item.State is SystemMtBatchItemState.Pending or SystemMtBatchItemState.Running
                ? item with { State = SystemMtBatchItemState.Cancelled, FailureReason = "cancellation requested" }
                : item)
            .ToList();
    }
}
