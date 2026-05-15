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

    /// <summary>按时间范围列出（趋势分析用）。</summary>
    ObservableCollection<Execution> GetByDateRange(DateTime from, DateTime to);
}
