using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>BatchPlan CRUD 接口。</summary>
public interface IBatchPlanRepository : IRepository<BatchPlan>
{
    /// <summary>按 Name 唯一索引查询。找不到返回 null。</summary>
    BatchPlan? GetByName(string name);

    /// <summary>列出 Enabled=true 且 Schedule 非空的 plans（scheduler 调度入口）。</summary>
    ObservableCollection<BatchPlan> GetScheduledEnabled();
}
