using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_DAL.V2;

public sealed class LiteDbRuntimeRepository
    : LiteDbIntPkRepositoryBase<MetBench_Domain.Runtime>, IRuntimeRepository
{
    protected override string CollectionKey => _dbConfig.Runtimes_Key;

    public MetBench_Domain.Runtime? GetByName(string name)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetBench_Domain.Runtime>(CollectionKey);
        return col.FindOne(x => x.Name == name);
    }
}
