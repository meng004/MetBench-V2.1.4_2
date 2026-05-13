namespace MetBench_BLL.Coverage;

/// <summary>
/// 4-维 coverage 报告 —— 让用户一眼看到当前 MR suite 的"全面性"。
/// </summary>
/// <remarks>
/// 4 个子维度：
/// <list type="bullet">
///   <item><see cref="MetaPattern"/>: 8 个 MetaPattern 中实际有几个被 MR 覆盖。</item>
///   <item><see cref="SutMr"/>: SUT × MR 笛卡尔积里有 binding 的格子数。</item>
///   <item><see cref="Bug"/>: KnownBug 里有几个被某次 Anomaly 复现过（reproduced/linked）。</item>
///   <item><see cref="Mutation"/>: Mutation 中被任意 MR 检出的格子数。</item>
/// </list>
/// </remarks>
public sealed record CoverageReport(
    DateTime GeneratedAt,
    MetaPatternCoverage MetaPattern,
    SutMrCoverage SutMr,
    BugCoverage Bug,
    MutationCoverage Mutation);

/// <summary>8 个 MetaPattern 中实际有 MR 落地的比例。</summary>
public sealed record MetaPatternCoverage(
    int TotalMetaPatterns,
    int CoveredMetaPatterns,
    IReadOnlyList<string> CoveredCodes,
    IReadOnlyList<string> UncoveredCodes)
{
    public double Ratio => TotalMetaPatterns == 0 ? 0.0 : (double)CoveredMetaPatterns / TotalMetaPatterns;
}

/// <summary>SUT × MR 二维稀疏度。</summary>
public sealed record SutMrCoverage(
    int TotalSuts,
    int TotalMRs,
    int BoundCells,
    IReadOnlyList<UnboundCell> UnboundCells)
{
    public int PossibleCells => TotalSuts * TotalMRs;
    public double Density => PossibleCells == 0 ? 0.0 : (double)BoundCells / PossibleCells;
}

/// <summary>未绑定的 (SUT, MR) 一格。</summary>
public sealed record UnboundCell(int SutId, string SutName, int MRId, string MRCode);

/// <summary>已知 bug 复现率。</summary>
public sealed record BugCoverage(
    int TotalKnownBugs,
    int ReproducedBugs,
    IReadOnlyList<string> ReproducedCodes,
    IReadOnlyList<string> NotReproducedCodes)
{
    public double Ratio => TotalKnownBugs == 0 ? 0.0 : (double)ReproducedBugs / TotalKnownBugs;
}

/// <summary>Mutation 检测率（按 mutant 维度合并）。</summary>
public sealed record MutationCoverage(
    int CampaignsSampled,
    int TotalMutants,
    int DetectedMutants,
    int MissedMutants)
{
    /// <summary>detection-rate = detected / (detected + missed)；分母 0 时返回 0。</summary>
    public double DetectionRate =>
        (DetectedMutants + MissedMutants) == 0 ? 0.0 : (double)DetectedMutants / (DetectedMutants + MissedMutants);
}
