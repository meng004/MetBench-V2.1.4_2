using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>MRInstance CRUD 接口。</summary>
public interface IMRInstanceRepository : IRepository<MRInstance>
{
    /// <summary>列出可复用实例（IsReusable=true，出现在 Scenarios 列表）。</summary>
    ObservableCollection<MRInstance> GetReusable();

    /// <summary>列出指定 Binding 的全部实例。</summary>
    ObservableCollection<MRInstance> GetByBinding(int mrBindingId);

    /// <summary>按可读名查（仅 reusable）。找不到返回 null。</summary>
    MRInstance? GetByName(string name);
}
