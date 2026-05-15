namespace MetBench_BLL.Mutation;

/// <summary>
/// MutationCampaign 跑完后的摘要 —— Mutation Testing 的核心指标。
/// </summary>
/// <remarks>
/// 关键指标：
/// <list type="bullet">
///   <item><c>detection rate</c> = detected / (detected + missed)，衡量 MR suite 灵敏度</item>
///   <item><c>false positive rate</c> = identity-detected / identity-total（Mut00 上应为 0）</item>
///   <item><c>coverage</c> = (detected + missed) / total，排除 not-affected 后的活跃比例</item>
/// </list>
/// </remarks>
public sealed record MutationCampaignSummary(
    Guid CampaignId,
    int TotalCells,
    int Detected,
    int Missed,
    int Errors,
    int NotAffected)
{
    /// <summary>检出率 = detected / (detected + missed)；分母为 0 时返回 0。</summary>
    public double DetectionRate =>
        (Detected + Missed) == 0 ? 0.0 : (double)Detected / (Detected + Missed);

    /// <summary>覆盖率 = (detected + missed) / total；分母为 0 时返回 0。</summary>
    public double Coverage =>
        TotalCells == 0 ? 0.0 : (double)(Detected + Missed) / TotalCells;
}
