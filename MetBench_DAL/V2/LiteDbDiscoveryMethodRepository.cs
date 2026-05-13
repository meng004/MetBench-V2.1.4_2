using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbDiscoveryMethodRepository
    : LiteDbIntPkRepositoryBase<DiscoveryMethod>, IDiscoveryMethodRepository
{
    protected override string CollectionKey => _dbConfig.DiscoveryMethods_Key;

    public ObservableCollection<DiscoveryMethod> GetEnabled()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<DiscoveryMethod>(CollectionKey);
        return new ObservableCollection<DiscoveryMethod>(col.Find(x => x.Enabled));
    }

    public DiscoveryMethod? GetByNameVersion(string name, string version)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<DiscoveryMethod>(CollectionKey);
        return col.FindOne(x => x.Name == name && x.Version == version);
    }
}
