namespace MetBench_BLL.SystemMT.Persistence;

using MetBench_BLL.Paging;

/// <summary>
/// Persistence contract for system-level metamorphic-test runs. The interface
/// owns the writeable scenario name (which a <see cref="SystemMtResult"/> alone
/// does not know) so the runner stays oblivious to which BDD scenario invoked
/// it. Implementations should be thread-safe at the database-handle level since
/// LiteDB allows concurrent reads but serialises writes internally.
/// </summary>
public interface ISystemMtResultRepository
{
    /// <summary>
    /// Persist a run result and return the assigned record id.
    /// </summary>
    Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a single record by id, or <c>null</c> if not found.
    /// </summary>
    Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Most-recent runs first (by <see cref="SystemMtResultRecord.RunAt"/>).
    /// </summary>
    Task<IReadOnlyList<SystemMtResultRecord>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Most-recent runs of a specific scenario, first.
    /// </summary>
    Task<IReadOnlyList<SystemMtResultRecord>> ListByMrNameAsync(string mrName, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// One page of results across all scenarios, most-recent first. The
    /// returned <see cref="PagedResult{T}.TotalCount"/> reflects the full
    /// collection size at query time, suitable for driving a pagination UI.
    /// </summary>
    Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// One page of results filtered to a single scenario, most-recent first.
    /// </summary>
    Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default);
}

