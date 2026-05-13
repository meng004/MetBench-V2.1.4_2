using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>DiscoveryRun CRUD 接口（Guid PK）。</summary>
public interface IDiscoveryRunRepository : IGuidRepository<DiscoveryRun>
{
    /// <summary>按 Method 列出。</summary>
    ObservableCollection<DiscoveryRun> GetByMethod(int methodId);

    /// <summary>按目标 SUT 列出。</summary>
    ObservableCollection<DiscoveryRun> GetByTargetApplication(int applicationId);
}
