using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>MRBinding CRUD 接口。</summary>
public interface IMRBindingRepository : IRepository<MRBinding>
{
    /// <summary>列出指定 MR 的全部绑定（含 IsActive=false 的历史绑定）。</summary>
    ObservableCollection<MRBinding> GetByMR(int mrId);

    /// <summary>列出指定 SUT 上的全部绑定。</summary>
    ObservableCollection<MRBinding> GetByApplication(int applicationId);

    /// <summary>列出全部 Status="active" 的绑定（catalog 公开视图）。</summary>
    ObservableCollection<MRBinding> GetActive();

    /// <summary>按指定 Status 列出（active / deprecated / archived / experimental）。</summary>
    ObservableCollection<MRBinding> GetByStatus(string status);
}
