using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_DAL.V2;

public sealed class LiteDbResultRepository
    : LiteDbGuidPkRepositoryBase<Result>, IResultRepository
{
    protected override string CollectionKey => _dbConfig.Results_Key;

    public Result? GetByExecution(Guid executionId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<Result>(CollectionKey);
        return col.FindOne(x => x.ExecutionId == executionId);
    }
}
