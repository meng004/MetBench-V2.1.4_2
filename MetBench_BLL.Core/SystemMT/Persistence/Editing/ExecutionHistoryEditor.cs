using MetBench_BLL.Paging;

namespace MetBench_BLL.SystemMT.Persistence.Editing;

public sealed class ExecutionHistoryEditor : IExecutionHistoryEditor
{
    private readonly ISystemMtResultRepository _results;
    private readonly IExecutionEvidenceRepository _evidence;

    public ExecutionHistoryEditor(
        ISystemMtResultRepository results,
        IExecutionEvidenceRepository evidence)
    {
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
    }

    public Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        _results.ListPagedAsync(request, cancellationToken);

    public Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default) =>
        _results.ListPagedByMrNameAsync(mrName, request, cancellationToken);

    public Task<ExecutionEvidence?> GetEvidenceAsync(Guid executionId, CancellationToken cancellationToken = default) =>
        _evidence.GetByExecutionAsync(executionId, cancellationToken);

    public async Task<ExecutionHistoryDeleteResult> DeleteAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        // Step 1 — Evidence first. We accept "no row to delete" (legacy executions
        // that predate the evidence-write-through wiring) as success-equivalent
        // and proceed to the result row.
        bool evidenceRemovedOrAbsent;
        try
        {
            evidenceRemovedOrAbsent = true;
            await _evidence.DeleteByExecutionIdAsync(executionId, cancellationToken).ConfigureAwait(false);
            // Note: a `false` return means "no row matched", which is fine —
            // we still proceed to delete the result. The only failure mode is
            // an exception, captured below.
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            evidenceRemovedOrAbsent = false;
        }

        if (!evidenceRemovedOrAbsent)
            return new ExecutionHistoryDeleteResult(Deleted: 0, EvidenceOnly: 0, Failed: 1);

        // Step 2 — Result row.
        bool resultDeleted;
        try
        {
            resultDeleted = await _results.DeleteAsync(executionId.ToString(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            resultDeleted = false;
        }

        return resultDeleted
            ? new ExecutionHistoryDeleteResult(Deleted: 1, EvidenceOnly: 0, Failed: 0)
            : new ExecutionHistoryDeleteResult(Deleted: 0, EvidenceOnly: 1, Failed: 0);
    }

    public async Task<ExecutionHistoryDeleteResult> DeleteBatchAsync(IEnumerable<Guid> executionIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executionIds);
        int deleted = 0, evidenceOnly = 0, failed = 0;
        foreach (var id in executionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var r = await DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            deleted += r.Deleted;
            evidenceOnly += r.EvidenceOnly;
            failed += r.Failed;
        }
        return new ExecutionHistoryDeleteResult(deleted, evidenceOnly, failed);
    }
}
