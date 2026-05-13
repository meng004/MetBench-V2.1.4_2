using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

public sealed class LiteDbValidationRunRepository
    : LiteDbGuidPkRepositoryBase<ValidationRun>, IValidationRunRepository
{
    protected override string CollectionKey => _dbConfig.ValidationRuns_Key;

    public ObservableCollection<ValidationRun> GetByCandidate(Guid candidateMRId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<ValidationRun>(CollectionKey);
        return new ObservableCollection<ValidationRun>(col.Find(x => x.CandidateMRId == candidateMRId));
    }

    public int CountPassedValidators(Guid candidateMRId)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<ValidationRun>(CollectionKey);
        return col.Count(x => x.CandidateMRId == candidateMRId && x.Passed);
    }
}
