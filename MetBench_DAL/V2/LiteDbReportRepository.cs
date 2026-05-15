using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbReportRepository
    : LiteDbGuidPkRepositoryBase<Report>, IReportRepository
{
    protected override string CollectionKey => _dbConfig.Reports_Key;

    public ObservableCollection<Report> GetByScope(string scope)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Report>(CollectionKey);
        return new ObservableCollection<Report>(col.Find(x => x.Scope == scope));
    }

    public ObservableCollection<Report> GetByDateRange(DateTime from, DateTime to)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Report>(CollectionKey);
        return new ObservableCollection<Report>(
            col.Find(x => x.GeneratedAt >= from && x.GeneratedAt < to));
    }
}
