namespace MetBench_BLL.Trend;

/// <summary>
/// 周报 DTO —— 一周内 MetBench 活动的关键指标 + WoW 对比 + 异常爆发标记。
/// </summary>
public sealed record WeeklyReport(
    DateTime WeekStartUtc,
    DateTime WeekEndUtc,
    int Executions,
    int ExecutionsWoW,
    int Anomalies,
    int AnomaliesWoW,
    int NewKnownBugs,
    int PromotedMRs,
    double AnomalyRate,
    double AnomalyRateWoW,
    IReadOnlyList<AnomalyBurst> Bursts,
    string Headline)
{
    /// <summary>本周执行数相对上周的增长（百分点；正=增）。</summary>
    public double ExecutionsDelta => ExecutionsWoW == 0 ? 0.0 :
        100.0 * (Executions - ExecutionsWoW) / ExecutionsWoW;

    /// <summary>本周异常率相对上周差异（绝对百分点）。</summary>
    public double AnomalyRatePoints => AnomalyRate - AnomalyRateWoW;
}

/// <summary>异常爆发 —— 某 SUT 或某 MR 在本周异常远高于历史均值。</summary>
public sealed record AnomalyBurst(
    string Dimension,  // "sut" | "mr-code" | "metapattern"
    string Key,
    int Count,
    double Baseline,
    double SigmasAbove);
