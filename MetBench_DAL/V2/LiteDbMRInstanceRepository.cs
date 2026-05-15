using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbMRInstanceRepository
    : LiteDbIntPkRepositoryBase<MRInstance>, IMRInstanceRepository
{
    protected override string CollectionKey => _dbConfig.MRInstances_Key;

    public ObservableCollection<MRInstance> GetReusable()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MRInstance>(CollectionKey);
        return new ObservableCollection<MRInstance>(col.Find(x => x.IsReusable));
    }

    public ObservableCollection<MRInstance> GetByBinding(int mrBindingId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MRInstance>(CollectionKey);
        return new ObservableCollection<MRInstance>(col.Find(x => x.MRBindingId == mrBindingId));
    }

    public MRInstance? GetByName(string name)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MRInstance>(CollectionKey);
        return col.FindOne(x => x.IsReusable && x.Name == name);
    }
}
