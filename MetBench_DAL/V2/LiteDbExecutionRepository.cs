using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbExecutionRepository
    : LiteDbGuidPkRepositoryBase<Execution>, IExecutionRepository
{
    protected override string CollectionKey => _dbConfig.Executions_Key;

    public ObservableCollection<Execution> GetByMRInstance(int mrInstanceId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Execution>(CollectionKey);
        return new ObservableCollection<Execution>(col.Find(x => x.MRInstanceId == mrInstanceId));
    }

    public ObservableCollection<Execution> GetByBatch(Guid batchId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Execution>(CollectionKey);
        return new ObservableCollection<Execution>(col.Find(x => x.BatchId == batchId));
    }

    public ObservableCollection<Execution> GetByStatus(string status, int pageIndex, int pageSize)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Execution>(CollectionKey);
        var query = col.Query()
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.QueuedAt)
            .Skip(pageIndex * pageSize)
            .Limit(pageSize)
            .ToList();
        return new ObservableCollection<Execution>(query);
    }

    public ObservableCollection<Execution> GetByDateRange(DateTime from, DateTime to)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Execution>(CollectionKey);
        return new ObservableCollection<Execution>(
            col.Find(x => x.QueuedAt >= from && x.QueuedAt < to));
    }
}
