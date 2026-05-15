using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbMRBindingRepository
    : LiteDbIntPkRepositoryBase<MRBinding>, IMRBindingRepository
{
    protected override string CollectionKey => _dbConfig.MRBindings_Key;

    public ObservableCollection<MRBinding> GetByMR(int mrId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MRBinding>(CollectionKey);
        return new ObservableCollection<MRBinding>(col.Find(x => x.MRId == mrId));
    }

    public ObservableCollection<MRBinding> GetByApplication(int applicationId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MRBinding>(CollectionKey);
        return new ObservableCollection<MRBinding>(col.Find(x => x.ApplicationId == applicationId));
    }

    public ObservableCollection<MRBinding> GetActive()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MRBinding>(CollectionKey);
        return new ObservableCollection<MRBinding>(col.Find(x => x.IsActive));
    }
}
