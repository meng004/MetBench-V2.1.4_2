using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>ValidationRun CRUD 接口（Guid PK）。</summary>
public interface IValidationRunRepository : IGuidRepository<ValidationRun>
{
    /// <summary>按 Candidate 列出全部验证记录（含 fail 的）。</summary>
    ObservableCollection<ValidationRun> GetByCandidate(Guid candidateMRId);

    /// <summary>统计某候选通过的 validator 数（≥2 才能 promote）。</summary>
    int CountPassedValidators(Guid candidateMRId);
}
