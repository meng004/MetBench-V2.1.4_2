using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbCandidateMRRepository
    : LiteDbGuidPkRepositoryBase<CandidateMR>, ICandidateMRRepository
{
    protected override string CollectionKey => _dbConfig.CandidateMRs_Key;

    public ObservableCollection<CandidateMR> GetByDiscoveryRun(Guid discoveryRunId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<CandidateMR>(CollectionKey);
        return new ObservableCollection<CandidateMR>(col.Find(x => x.DiscoveryRunId == discoveryRunId));
    }

    public ObservableCollection<CandidateMR> GetByStatus(string status)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<CandidateMR>(CollectionKey);
        return new ObservableCollection<CandidateMR>(col.Find(x => x.Status == status));
    }

    public int? GetPromotedMRId(Guid candidateId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<CandidateMR>(CollectionKey);
        return col.FindById(candidateId)?.PromotedToMRId;
    }
}
