using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbKnownBugRepository
    : LiteDbIntPkRepositoryBase<KnownBug>, IKnownBugRepository
{
    protected override string CollectionKey => _dbConfig.KnownBugs_Key;

    public KnownBug? GetByCode(string code)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<KnownBug>(CollectionKey);
        return col.FindOne(x => x.Code == code);
    }

    public ObservableCollection<KnownBug> GetByStatus(string status)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<KnownBug>(CollectionKey);
        return new ObservableCollection<KnownBug>(col.Find(x => x.Status == status));
    }
}
