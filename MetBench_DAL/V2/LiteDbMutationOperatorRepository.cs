using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbMutationOperatorRepository
    : LiteDbIntPkRepositoryBase<MutationOperator>, IMutationOperatorRepository
{
    protected override string CollectionKey => _dbConfig.MutationOperators_Key;

    public MutationOperator? GetByCode(string code)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MutationOperator>(CollectionKey);
        return col.FindOne(x => x.Code == code);
    }

    public ObservableCollection<MutationOperator> GetByCategory(string category)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MutationOperator>(CollectionKey);
        return new ObservableCollection<MutationOperator>(col.Find(x => x.Category == category));
    }
}
