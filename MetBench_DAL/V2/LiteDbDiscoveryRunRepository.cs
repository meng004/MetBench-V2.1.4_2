using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbDiscoveryRunRepository
    : LiteDbGuidPkRepositoryBase<DiscoveryRun>, IDiscoveryRunRepository
{
    protected override string CollectionKey => _dbConfig.DiscoveryRuns_Key;

    public ObservableCollection<DiscoveryRun> GetByMethod(int methodId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<DiscoveryRun>(CollectionKey);
        return new ObservableCollection<DiscoveryRun>(col.Find(x => x.MethodId == methodId));
    }

    public ObservableCollection<DiscoveryRun> GetByTargetApplication(int applicationId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<DiscoveryRun>(CollectionKey);
        return new ObservableCollection<DiscoveryRun>(
            col.Find(x => x.TargetApplicationId == applicationId));
    }
}
