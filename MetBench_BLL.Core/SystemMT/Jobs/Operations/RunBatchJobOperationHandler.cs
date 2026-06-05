using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>Executes a durable RunBatch job through the existing launcher batch API.</summary>
public sealed class RunBatchJobOperationHandler : ISystemMtJobOperationDispatcher
{
    private readonly ISystemMtLauncher _launcher;
    private readonly IExecutionEvidenceRepository? _evidenceRepository;

    public RunBatchJobOperationHandler(
        ISystemMtLauncher launcher,
        IExecutionEvidenceRepository? evidenceRepository = null)
    {
        _launcher = launcher;
        _evidenceRepository = evidenceRepository;
    }

    public async Task<JobExecutionOutcome> ExecuteAsync(
        Guid jobId,
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (record.Kind != SystemMtJobKind.RunBatch)
            throw new NotSupportedException($"Unsupported job kind for batch handler: {record.Kind}.");
        if (record.BatchItems.Count == 0)
            return new JobExecutionOutcome(
                SystemMtJobState.Failed,
                string.Empty,
                Result: null,
                FailureReason: "RunBatch job has no MR ids.");

        var items = record.BatchItems.ToList();
        progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "preparing-batch", 5));

        var batchProgress = new InlineBatchProgress(p =>
        {
            var index = items.FindIndex(item => string.Equals(item.MrId, p.CurrentMrId, StringComparison.Ordinal));
            if (index < 0)
                return;

            items[index] = p.LastResult is null
                ? items[index] with { State = SystemMtBatchItemState.Running }
                : ToItem(p.LastResult);

            var percent = p.Total == 0 ? 100 : 10 + (int)Math.Round(80.0 * p.Completed / p.Total);
            progress?.Report(new SystemMtJobProgress(
                SystemMtJobState.RunningSource,
                $"batch {p.Completed}/{p.Total}: {p.CurrentMrId}",
                Math.Clamp(percent, 10, 90),
                items.ToList()));
        });

        try
        {
            var requests = items
                .Select(item => new BatchMrRunRequest(item.MrId, record.ParameterOverrides))
                .ToList();
            var results = await _launcher.RunBatchAsync(requests, batchProgress, cancellationToken);

            foreach (var result in results)
            {
                var index = items.FindIndex(item => string.Equals(item.MrId, result.MrId, StringComparison.Ordinal));
                if (index >= 0)
                    items[index] = ToItem(result);
            }

            var runtimeFailureKind = await ResolveRuntimeFailureKindAsync(results, cancellationToken);
            var hasInfrastructureFailure =
                runtimeFailureKind is not null ||
                results.Any(IsSynthesizedInfrastructureFailure);
            return new JobExecutionOutcome(
                hasInfrastructureFailure ? SystemMtJobState.Failed : SystemMtJobState.Succeeded,
                SutName: string.Empty,
                Result: null,
                FailureReason: hasInfrastructureFailure ? "one or more batch MR runs failed due to infrastructure errors" : null,
                FailureKind: runtimeFailureKind,
                BatchItems: items);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].State is SystemMtBatchItemState.Running or SystemMtBatchItemState.Pending)
                    items[i] = items[i] with { State = SystemMtBatchItemState.Cancelled, FailureReason = "cancellation requested" };
            }

            return new JobExecutionOutcome(
                SystemMtJobState.Cancelled,
                SutName: string.Empty,
                Result: null,
                FailureReason: "cancellation requested",
                BatchItems: items);
        }
    }

    private static SystemMtBatchJobItem ToItem(MrRunResult result) => new(
        result.MrId,
        result.Passed ? SystemMtBatchItemState.Succeeded : SystemMtBatchItemState.Failed,
        result.Passed ? null : result.FailureReason,
        string.IsNullOrWhiteSpace(result.RecordId) ? null : result.RecordId);

    private static bool IsSynthesizedInfrastructureFailure(MrRunResult result) =>
        !result.Passed &&
        string.IsNullOrWhiteSpace(result.RecordId) &&
        result.FailureReason.StartsWith("Run threw ", StringComparison.Ordinal);

    private async Task<string?> ResolveRuntimeFailureKindAsync(
        IReadOnlyList<MrRunResult> results,
        CancellationToken cancellationToken)
    {
        if (_evidenceRepository is null)
            return null;

        foreach (var result in results)
        {
            if (result.Passed || !Guid.TryParse(result.RecordId, out var executionId))
                continue;

            var evidence = await _evidenceRepository.GetByExecutionAsync(executionId, cancellationToken);
            var runtime = evidence?.RuntimeEvidence;
            if (runtime is { Passed: false })
                return runtime.FailureKind;
        }

        return null;
    }

    private sealed class InlineBatchProgress : IProgress<BatchProgress>
    {
        private readonly Action<BatchProgress> _handler;
        public InlineBatchProgress(Action<BatchProgress> handler) => _handler = handler;
        public void Report(BatchProgress value) => _handler(value);
    }
}
