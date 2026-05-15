using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbBatchRepository
    : LiteDbGuidPkRepositoryBase<Batch>, IBatchRepository
{
    protected override string CollectionKey => _dbConfig.Batches_Key;

    public ObservableCollection<Batch> GetByPlan(int planId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Batch>(CollectionKey);
        return new ObservableCollection<Batch>(col.Find(x => x.PlanId == planId));
    }

    public ObservableCollection<Batch> GetByStatus(string status)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Batch>(CollectionKey);
        return new ObservableCollection<Batch>(col.Find(x => x.Status == status));
    }
}
