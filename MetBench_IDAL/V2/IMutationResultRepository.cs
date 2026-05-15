using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>MutationResult CRUD 接口（Guid PK）。</summary>
public interface IMutationResultRepository : IGuidRepository<MutationResult>
{
    /// <summary>按 Campaign 列出全部 cell（矩阵报告用）。</summary>
    ObservableCollection<MutationResult> GetByCampaign(Guid campaignId);

    /// <summary>按 (mutant, mrBinding) 唯一组合查询。</summary>
    MutationResult? GetByMutantBinding(int mutantId, int mrBindingId);

    /// <summary>按 Outcome 列出（detected / missed / error / not-affected）。</summary>
    ObservableCollection<MutationResult> GetByOutcome(Guid campaignId, string outcome);
}
