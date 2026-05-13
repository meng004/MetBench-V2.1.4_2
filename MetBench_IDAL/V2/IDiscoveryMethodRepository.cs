using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>DiscoveryMethod CRUD 接口。</summary>
public interface IDiscoveryMethodRepository : IRepository<DiscoveryMethod>
{
    /// <summary>列出 Enabled=true 的方法（DI 容器读取）。</summary>
    ObservableCollection<DiscoveryMethod> GetEnabled();

    /// <summary>按 (Name, Version) 唯一组合查询。找不到返回 null。</summary>
    DiscoveryMethod? GetByNameVersion(string name, string version);
}
