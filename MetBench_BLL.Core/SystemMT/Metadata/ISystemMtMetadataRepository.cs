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

    // ===== mr-architecture P0 — EquationFunctionRecipe(L1 数据驱动方程函数) =====

    /// <summary>
    /// 插入新 recipe 或按 <c>(EquationKey, FunctionName)</c> 复合键就地更新。
    /// 校验由调用方(catalog service)做;本方法只做存储与查重。
    /// </summary>
    Task UpsertRecipeAsync(EquationFunctionRecipe recipe, CancellationToken cancellationToken = default);

    /// <summary>按 <c>(EquationKey, FunctionName)</c> 取一行,缺失返回 <c>null</c>。</summary>
    Task<EquationFunctionRecipe?> GetRecipeAsync(string equationKey, string functionName,
        CancellationToken cancellationToken = default);

    /// <summary>列出指定方程下的全部 L1 recipe,按 FunctionName 升序。</summary>
    Task<IReadOnlyList<EquationFunctionRecipe>> ListRecipesByEquationAsync(string equationKey,
        CancellationToken cancellationToken = default);

    /// <summary>列出全部 L1 recipe,按 (EquationKey, FunctionName) 升序。</summary>
    Task<IReadOnlyList<EquationFunctionRecipe>> ListRecipesAsync(CancellationToken cancellationToken = default);

    // ===== G-10 — Delete 路径补齐 =====

    /// <summary>按 EquationKey 删除一行。返回是否实际删除（不存在时返回 false）。</summary>
    Task<bool> DeleteEquationAsync(string equationKey, CancellationToken cancellationToken = default);

    /// <summary>按 MrId 删除一行。返回是否实际删除。</summary>
    Task<bool> DeleteMrAsync(string mrId, CancellationToken cancellationToken = default);

    /// <summary>按 (EquationKey, FunctionName) 删除一行 recipe。返回是否实际删除。</summary>
    Task<bool> DeleteRecipeAsync(string equationKey, string functionName,
        CancellationToken cancellationToken = default);
}
