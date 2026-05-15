using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>Application × Domain junction CRUD 接口。</summary>
public interface IApplicationDomainRepository : IRepository<ApplicationDomain>
{
    /// <summary>列出指定 Application 关联的全部 Domain id。</summary>
    ObservableCollection<int> GetDomainIdsForApplication(int applicationId);

    /// <summary>列出指定 Domain 下的全部 Application id。</summary>
    ObservableCollection<int> GetApplicationIdsForDomain(int domainId);
}
