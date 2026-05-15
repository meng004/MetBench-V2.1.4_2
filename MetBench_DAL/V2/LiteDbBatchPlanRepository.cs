using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbBatchPlanRepository
    : LiteDbIntPkRepositoryBase<BatchPlan>, IBatchPlanRepository
{
    protected override string CollectionKey => _dbConfig.BatchPlans_Key;

    public BatchPlan? GetByName(string name)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<BatchPlan>(CollectionKey);
        return col.FindOne(x => x.Name == name);
    }

    public ObservableCollection<BatchPlan> GetScheduledEnabled()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<BatchPlan>(CollectionKey);
        return new ObservableCollection<BatchPlan>(
            col.Find(x => x.Enabled && x.Schedule != null));
    }
}
