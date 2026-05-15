using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Discovery.Validators;

/// <summary>
/// 真实的 <see cref="MutantProbeSampler"/> 实现 —— 从 LiteDB <c>MutationResults</c> 表
/// 拉历史 mutation campaign 数据，按 mutant 分组判定该候选 MR 是否能检出。
/// </summary>
/// <remarks>
/// 替代 App.xaml.cs DI 中曾经的 hardcoded 5-mutant stub。
/// 找不到 campaign 数据时返回 empty list；AdversarialMutmutValidator 上层视为 fail（更保守）。
///
/// 当前简化：拉最近 N 个 MutationResult（不限定 binding 即同一 MR）。
/// follow-up：需 MR → MRBinding → MutationResult.MRBindingId 反查（更准确）。
/// </remarks>
public sealed class AdversarialCampaignSampler
{
    private readonly IMutationResultRepository _mutationResults;
    private readonly int _maxMutants;

    public AdversarialCampaignSampler(IMutationResultRepository mutationResults, int maxMutants = 20)
    {
        _mutationResults = mutationResults;
        _maxMutants = maxMutants;
    }

    /// <summary>
    /// 暴露为 MutantProbeSampler delegate 形参兼容的方法。
    /// </summary>
    public Task<IReadOnlyList<MutantProbe>> SampleAsync(CandidateMR candidate, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // 按 mutant 分组：任一 cell 为 "detected" → AssertionStillHolds=false（该 mutant 被该 MR 抓住）；
        // 全是 "missed"/"not-affected" → AssertionStillHolds=true。
        var all = _mutationResults.GetAll();
        var probes = all
            .GroupBy(r => r.MutantId)
            .Take(_maxMutants)
            .Select(g => new MutantProbe(
                MutantId: g.Key,
                AssertionStillHolds: !g.Any(r => r.Outcome == "detected")))
            .ToList();

        return Task.FromResult<IReadOnlyList<MutantProbe>>(probes);
    }
}
