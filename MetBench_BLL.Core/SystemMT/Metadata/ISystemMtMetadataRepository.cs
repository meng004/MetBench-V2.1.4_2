namespace MetBench_BLL.SystemMT.Metadata;

/// <summary>
/// Persistence contract for System-MT reference metadata: the equations SUTs
/// solve (<see cref="EquationMetadata"/>) and the metamorphic relations the
/// catalog exposes (<see cref="MrMetadata"/>). This is an additive layer keyed
/// by stable business slugs — it does not replace the hard-coded MR catalog,
/// it annotates it with structured, queryable descriptions.
/// </summary>
public interface ISystemMtMetadataRepository
{
    /// <summary>
    /// Insert a new equation or update the existing one with the same
    /// <see cref="EquationMetadata.EquationKey"/>. Upsert keeps seeding
    /// idempotent.
    /// </summary>
    Task UpsertEquationAsync(EquationMetadata equation, CancellationToken cancellationToken = default);

    /// <summary>Fetch one equation by its key, or <c>null</c> if absent.</summary>
    Task<EquationMetadata?> GetEquationAsync(string equationKey, CancellationToken cancellationToken = default);

    /// <summary>All persisted equations, ordered by key.</summary>
    Task<IReadOnlyList<EquationMetadata>> ListEquationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert a new MR metadata record or update the existing one with the
    /// same <see cref="MrMetadata.MrId"/>.
    /// </summary>
    Task UpsertMrAsync(MrMetadata mr, CancellationToken cancellationToken = default);

    /// <summary>Fetch one MR's metadata by its id, or <c>null</c> if absent.</summary>
    Task<MrMetadata?> GetMrAsync(string mrId, CancellationToken cancellationToken = default);

    /// <summary>All MR metadata for one equation, ordered by MR id.</summary>
    Task<IReadOnlyList<MrMetadata>> ListMrsByEquationAsync(string equationKey, CancellationToken cancellationToken = default);

    /// <summary>All persisted MR metadata, ordered by MR id.</summary>
    Task<IReadOnlyList<MrMetadata>> ListMrsAsync(CancellationToken cancellationToken = default);
}
