using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>CandidateMR CRUD 接口（Guid PK）。</summary>
public interface ICandidateMRRepository : IGuidRepository<CandidateMR>
{
    /// <summary>按 DiscoveryRun 列出本次提议的候选。</summary>
    ObservableCollection<CandidateMR> GetByDiscoveryRun(Guid discoveryRunId);

    /// <summary>按 Status 列出（pending / validated / promoted / rejected / duplicate）。</summary>
    ObservableCollection<CandidateMR> GetByStatus(string status);

    /// <summary>找到 promoted 后对应的 MR 行。可空，意味着尚未 promote。</summary>
    int? GetPromotedMRId(Guid candidateId);
}
