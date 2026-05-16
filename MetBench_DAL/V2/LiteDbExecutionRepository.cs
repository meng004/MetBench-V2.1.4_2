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

    public ObservableCollection<Execution> GetByStatusKeyset(
        string status, DateTime? afterQueuedAt, Guid? afterId, int pageSize)
    {
        if (pageSize <= 0) return new ObservableCollection<Execution>();
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Execution>(CollectionKey);
        // 同集合内可能存在相同 QueuedAt 的行，所以需要 (QueuedAt, IdExecution) 复合 cursor。
        // LiteDB 在内存里 LINQ 过滤完后才 OrderBy + Limit，所以 status 索引仍是关键剪枝。
        IEnumerable<Execution> rows = col.Find(x => x.Status == status);
        if (afterQueuedAt.HasValue && afterId.HasValue)
        {
            var cursorAt = afterQueuedAt.Value;
            var cursorId = afterId.Value;
            rows = rows.Where(x =>
                x.QueuedAt < cursorAt ||
                (x.QueuedAt == cursorAt && x.IdExecution.CompareTo(cursorId) < 0));
        }
        var page = rows
            .OrderByDescending(x => x.QueuedAt)
            .ThenByDescending(x => x.IdExecution)
            .Take(pageSize)
            .ToList();
        return new ObservableCollection<Execution>(page);
    }

    public ObservableCollection<Execution> GetByDateRange(DateTime from, DateTime to)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Execution>(CollectionKey);
        return new ObservableCollection<Execution>(
            col.Find(x => x.QueuedAt >= from && x.QueuedAt < to));
    }
}
