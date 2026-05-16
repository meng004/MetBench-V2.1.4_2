using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>Execution CRUD + 查询接口（Guid PK）。</summary>
public interface IExecutionRepository : IGuidRepository<Execution>
{
    /// <summary>按 MRInstance 列出全部 Execution（含历史 replay）。</summary>
    ObservableCollection<Execution> GetByMRInstance(int mrInstanceId);

    /// <summary>按 Batch 列出。</summary>
    ObservableCollection<Execution> GetByBatch(Guid batchId);

    /// <summary>按 Status 分页（如 status='anomaly' 的最近 100 条）。</summary>
    ObservableCollection<Execution> GetByStatus(string status, int pageIndex, int pageSize);

    /// <summary>
    /// Keyset 分页（按 status 过滤 + 按 <c>QueuedAt</c> DESC 排序）。
    /// </summary>
    /// <param name="status">过滤的 status 值。</param>
    /// <param name="afterQueuedAt">cursor 时间戳：返回 <c>QueuedAt &lt;</c> 此值的行；首页传 <c>null</c>。</param>
    /// <param name="afterId">同一时间戳的 tiebreaker：返回 <c>IdExecution &lt;</c> 此值的行。</param>
    /// <param name="pageSize">每页行数。</param>
    /// <remarks>
    /// 用于 UI 的 "最近 N 条 anomaly" 类列表，O(pageSize) 复杂度。调用方拿上一页
    /// 最后一行的 <c>(QueuedAt, IdExecution)</c> 作为下一页的 cursor。
    /// </remarks>
    ObservableCollection<Execution> GetByStatusKeyset(
        string status, DateTime? afterQueuedAt, Guid? afterId, int pageSize)
    {
        if (pageSize <= 0) return new ObservableCollection<Execution>();
        // Default impl for test fakes: 走 GetAll → filter → 排序 → take。
        // 生产 LiteDb 实现 override 这个以走 Status 索引。
        IEnumerable<Execution> rows = GetAll().Where(x => x.Status == status);
        if (afterQueuedAt.HasValue && afterId.HasValue)
        {
            var cAt = afterQueuedAt.Value;
            var cId = afterId.Value;
            rows = rows.Where(x =>
                x.QueuedAt < cAt ||
                (x.QueuedAt == cAt && x.IdExecution.CompareTo(cId) < 0));
        }
        var page = rows
            .OrderByDescending(x => x.QueuedAt)
            .ThenByDescending(x => x.IdExecution)
            .Take(pageSize)
            .ToList();
        return new ObservableCollection<Execution>(page);
    }

    /// <summary>按时间范围列出（趋势分析用）。</summary>
    ObservableCollection<Execution> GetByDateRange(DateTime from, DateTime to);
}
