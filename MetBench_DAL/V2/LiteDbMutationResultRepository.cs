using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbMutationResultRepository
    : LiteDbGuidPkRepositoryBase<MutationResult>, IMutationResultRepository
{
    protected override string CollectionKey => _dbConfig.MutationResults_Key;

    public ObservableCollection<MutationResult> GetByCampaign(Guid campaignId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MutationResult>(CollectionKey);
        return new ObservableCollection<MutationResult>(col.Find(x => x.CampaignId == campaignId));
    }

    public MutationResult? GetByMutantBinding(int mutantId, int mrBindingId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MutationResult>(CollectionKey);
        return col.FindOne(x => x.MutantId == mutantId && x.MRBindingId == mrBindingId);
    }

    public ObservableCollection<MutationResult> GetByOutcome(Guid campaignId, string outcome)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MutationResult>(CollectionKey);
        return new ObservableCollection<MutationResult>(
            col.Find(x => x.CampaignId == campaignId && x.Outcome == outcome));
    }
}
