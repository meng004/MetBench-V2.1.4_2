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

    /// <summary>
    /// Keyset 分页 —— 用 PK 作为 cursor，生产实现 (LiteDB) 走索引 O(pageSize)；
    /// 默认实现走 <see cref="GetAll"/> + LINQ 过滤，给测试 fake 用。
    /// </summary>
    /// <param name="afterId">cursor：返回 ID 严格大于此值的 page；首页传 <c>null</c>。</param>
    /// <param name="pageSize">每页行数。</param>
    /// <remarks>
    /// 适合"加载更多"风格 UI（infinite scroll）。结果按 PK 升序排序，
    /// 调用方拿最后一行的 Id 作为下一页 cursor 即可。
    ///
    /// 默认实现依赖反射：找一个 <see cref="Guid"/> 类型且名字以 "Id" 开头的 public property
    /// 作为 PK。生产 <c>LiteDb*Repository</c> 会 override 这个走 <c>_id</c> 索引。
    /// </remarks>
    ObservableCollection<T> GetPageKeyset(Guid? afterId, int pageSize)
    {
        if (pageSize <= 0) return new ObservableCollection<T>();
        var pk = typeof(T).GetProperties()
            .FirstOrDefault(p => p.PropertyType == typeof(Guid)
                && (p.Name.StartsWith("Id", StringComparison.Ordinal)
                    || p.Name.Equals("Id", StringComparison.Ordinal)));
        if (pk is null) return new ObservableCollection<T>();
        var rows = GetAll();
        IEnumerable<T> filtered = rows;
        if (afterId.HasValue)
        {
            var cursor = afterId.Value;
            filtered = filtered.Where(x => ((Guid)pk.GetValue(x)!).CompareTo(cursor) > 0);
        }
        var page = filtered
            .OrderBy(x => (Guid)pk.GetValue(x)!)
            .Take(pageSize)
            .ToList();
        return new ObservableCollection<T>(page);
    }

    /// <summary>总行数（分页 UI 用）。</summary>
    int Count();
}
