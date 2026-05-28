using MetBench_BLL.Paging;

namespace MetBench_BLL.SystemMT.Persistence.Editing;

/// <summary>
/// Read + delete surface for the execution-history page. Create / Update are
/// intentionally absent — execution records are pipeline ground truth and
/// must not be hand-edited (CLAUDE.md §0.5 ANTI-UNREQUESTED-EDIT for the
/// production audit log).
/// </summary>
/// <remarks>
/// Cross-collection deletes (<see cref="DeleteAsync"/> / <see cref="DeleteBatchAsync"/>)
/// are sequenced Evidence-first → Result. LiteDB v5 has no transactional API
/// spanning collections, so a successful Evidence delete followed by a failed
/// Result delete returns an <c>evidenceOnly</c> count via
/// <see cref="ExecutionHistoryDeleteResult"/> rather than rolling back. The
/// orphan Result row remains visible for the user to retry.
/// </remarks>
public interface IExecutionHistoryEditor
{
    /// <summary>
    /// One page across all MRs, most-recent first. Delegates to
    /// <see cref="ISystemMtResultRepository.ListPagedAsync"/>.
    /// </summary>
    Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// One page filtered to a specific MR (exact, case-sensitive match).
    /// Delegates to <see cref="ISystemMtResultRepository.ListPagedByMrNameAsync"/>.
    /// </summary>
    Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch the linked evidence row for a result (if any). Delegates to
    /// <see cref="IExecutionEvidenceRepository.GetByExecutionAsync"/>.
    /// </summary>
    Task<ExecutionEvidence?> GetEvidenceAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete one execution, sequenced Evidence-first → Result.
    /// Returns:
    /// <list type="bullet">
    /// <item><c>Deleted=1</c> when both collections were updated;</item>
    /// <item><c>EvidenceOnly=1</c> when evidence was deleted but result-row delete
    /// failed (result row left visible for retry);</item>
    /// <item><c>Failed=1</c> when evidence delete failed (result row not touched).</item>
    /// </list>
    /// When the evidence row simply does not exist (legacy rows without an
    /// evidence companion), the result-row delete still runs; success counts
    /// as <c>Deleted=1</c>.
    /// </summary>
    Task<ExecutionHistoryDeleteResult> DeleteAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete multiple executions sequentially using the same Evidence-first
    /// strategy as <see cref="DeleteAsync"/>. Per-row outcomes are summed
    /// into a single <see cref="ExecutionHistoryDeleteResult"/>.
    /// </summary>
    Task<ExecutionHistoryDeleteResult> DeleteBatchAsync(IEnumerable<Guid> executionIds, CancellationToken cancellationToken = default);
}
