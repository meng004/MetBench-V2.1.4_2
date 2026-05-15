using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>
/// 泛型仓库接口（Guid 主键版本）— 增删查改。
/// </summary>
/// <typeparam name="T">实体类型（必须有 Guid PK）。</typeparam>
/// <remarks>
/// 用于 v2 高频生成的实体：Execution / Result / Anomaly / DiscoveryRun /
/// CandidateMR / ValidationRun / MutationCampaign / MutationResult /
/// AuditLog / Batch / Report。
///
/// 设计上对应既有 v1 <see cref="IRepository{T}"/>（int PK 版本），保持方法名
/// 一致以便调用方风格统一。
/// </remarks>
public interface IGuidRepository<T> where T : class
{
    /// <summary>列出全部行（无 limit；上层若需要分页，调 <see cref="GetPage"/>）。</summary>
    ObservableCollection<T> GetAll();

    /// <summary>按 Guid PK 取一行。找不到返回 null。</summary>
    T? Get(Guid id);

    /// <summary>条件查询：以实体非空字段为模板查匹配行。</summary>
    ObservableCollection<T> Get(T template);

    /// <summary>插入。成功返回 true。</summary>
    bool Add(T entity);

    /// <summary>更新。成功返回 true。</summary>
    bool Modify(T entity);

    /// <summary>删除。成功返回 true。</summary>
    bool Remove(T entity);

    /// <summary>分页列出（推荐用于高频实体的 UI 列表）。</summary>
    ObservableCollection<T> GetPage(int pageIndex, int pageSize);

    /// <summary>总行数（分页 UI 用）。</summary>
    int Count();
}
