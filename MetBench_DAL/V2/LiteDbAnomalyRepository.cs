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

    public ObservableCollection<Anomaly> GetByStatus(string status)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Anomaly>(CollectionKey);
        return new ObservableCollection<Anomaly>(col.Find(x => x.Status == status));
    }

    public ObservableCollection<Anomaly> GetByLinkedBug(int knownBugId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Anomaly>(CollectionKey);
        return new ObservableCollection<Anomaly>(col.Find(x => x.LinkedKnownBugId == knownBugId));
    }
}
