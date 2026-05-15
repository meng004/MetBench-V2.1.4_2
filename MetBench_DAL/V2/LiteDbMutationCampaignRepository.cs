using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbMutationCampaignRepository
    : LiteDbGuidPkRepositoryBase<MutationCampaign>, IMutationCampaignRepository
{
    protected override string CollectionKey => _dbConfig.MutationCampaigns_Key;

    public ObservableCollection<MutationCampaign> GetByStatus(string status)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MutationCampaign>(CollectionKey);
        return new ObservableCollection<MutationCampaign>(col.Find(x => x.Status == status));
    }

    public ObservableCollection<MutationCampaign> GetRecent(int limit)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MutationCampaign>(CollectionKey);
        var query = col.Query()
            .OrderByDescending(x => x.StartedAt)
            .Limit(limit)
            .ToList();
        return new ObservableCollection<MutationCampaign>(query);
    }
}
