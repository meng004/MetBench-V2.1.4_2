using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbAuditLogRepository
    : LiteDbGuidPkRepositoryBase<AuditLog>, IAuditLogRepository
{
    protected override string CollectionKey => _dbConfig.AuditLog_Key;

    public ObservableCollection<AuditLog> GetByDateRange(DateTime from, DateTime to)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<AuditLog>(CollectionKey);
        return new ObservableCollection<AuditLog>(
            col.Find(x => x.Timestamp >= from && x.Timestamp < to));
    }

    public ObservableCollection<AuditLog> GetByAction(string action)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<AuditLog>(CollectionKey);
        return new ObservableCollection<AuditLog>(col.Find(x => x.Action == action));
    }

    public ObservableCollection<AuditLog> GetByTargetEntity(string entityType, string entityId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<AuditLog>(CollectionKey);
        return new ObservableCollection<AuditLog>(
            col.Find(x => x.TargetEntityType == entityType && x.TargetEntityId == entityId));
    }
}
