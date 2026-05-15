using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>Batch CRUD 接口（Guid PK）。</summary>
public interface IBatchRepository : IGuidRepository<Batch>
{
    /// <summary>按 Plan 列出（看 plan 触发的全部 batch 历史）。</summary>
    ObservableCollection<Batch> GetByPlan(int planId);

    /// <summary>按 Status 列出。</summary>
    ObservableCollection<Batch> GetByStatus(string status);
}
