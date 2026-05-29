using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbAnomalyRepository
    : LiteDbGuidPkRepositoryBase<Anomaly>, IAnomalyRepository
{
    protected override string CollectionKey => _dbConfig.Anomalies_Key;

    public Anomaly? GetByResult(Guid resultId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Anomaly>(CollectionKey);
        return col.FindOne(x => x.ResultId == resultId);
    }

    public ObservableCollection<Anomaly> GetByStatus(AnomalyStatus status)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Anomaly>(CollectionKey);
        // Status 持久化为 int（debt #5）。用显式 int 走 Status 索引匹配——LINQ 的
        // x.Status == status 会把 enum 常量按默认 enum-name 序列化，与 int 存储不匹配。
        return new ObservableCollection<Anomaly>(col.Find(Query.EQ("Status", (int)status)));
    }

    public ObservableCollection<Anomaly> GetByLinkedBug(int knownBugId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Anomaly>(CollectionKey);
        return new ObservableCollection<Anomaly>(col.Find(x => x.LinkedKnownBugId == knownBugId));
    }
}
