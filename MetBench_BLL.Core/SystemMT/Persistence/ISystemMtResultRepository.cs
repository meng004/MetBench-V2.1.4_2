namespace MetBench_BLL.SystemMT.Persistence;

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
    Task<string> SaveAsync(string scenarioName, SystemMtResult result, CancellationToken cancellationToken = default);

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
    Task<IReadOnlyList<SystemMtResultRecord>> ListByScenarioAsync(string scenarioName, int limit = 100, CancellationToken cancellationToken = default);
}
