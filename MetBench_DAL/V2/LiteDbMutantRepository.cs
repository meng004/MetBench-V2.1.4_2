using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbMutantRepository
    : LiteDbIntPkRepositoryBase<Mutant>, IMutantRepository
{
    protected override string CollectionKey => _dbConfig.Mutants_Key;

    public ObservableCollection<Mutant> GetByOperator(int operatorId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Mutant>(CollectionKey);
        return new ObservableCollection<Mutant>(col.Find(x => x.OperatorId == operatorId));
    }

    public ObservableCollection<Mutant> GetByApplication(int applicationId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Mutant>(CollectionKey);
        return new ObservableCollection<Mutant>(col.Find(x => x.ApplicationId == applicationId));
    }

    public ObservableCollection<Mutant> GetActive()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Mutant>(CollectionKey);
        return new ObservableCollection<Mutant>(col.Find(x => x.Status == "active"));
    }
}
