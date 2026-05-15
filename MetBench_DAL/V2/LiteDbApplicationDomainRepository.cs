using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbApplicationDomainRepository
    : LiteDbIntPkRepositoryBase<ApplicationDomain>, IApplicationDomainRepository
{
    protected override string CollectionKey => _dbConfig.ApplicationDomains_Key;

    public ObservableCollection<int> GetDomainIdsForApplication(int applicationId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<ApplicationDomain>(CollectionKey);
        return new ObservableCollection<int>(
            col.Find(x => x.ApplicationId == applicationId).Select(x => x.DomainId));
    }

    public ObservableCollection<int> GetApplicationIdsForDomain(int domainId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<ApplicationDomain>(CollectionKey);
        return new ObservableCollection<int>(
            col.Find(x => x.DomainId == domainId).Select(x => x.ApplicationId));
    }
}
