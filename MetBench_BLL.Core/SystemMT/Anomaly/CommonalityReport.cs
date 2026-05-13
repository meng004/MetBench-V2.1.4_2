namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// 共性分析报告 — 输入一组 Anomaly，统计主导属性 + 给出可读 hypothesis。
/// </summary>
/// <param name="Total">总 Anomaly 数。</param>
/// <param name="BySeverity">Severity 分布（key=severity，value=count）。</param>
/// <param name="ByCategory">Category 分布。</param>
/// <param name="ByStatus">Status 分布。</param>
/// <param name="DominantSeverity">最常见 severity 类（null 若并列）。</param>
/// <param name="DominantCategory">最常见 category 类。</param>
/// <param name="NoiseFloorCount">Severity = "noise" 的子集计数（MC 噪声底，非真 bug）。</param>
/// <param name="MajorOrCriticalCount">Severity = "major" / "critical" 的子集计数（真 bug 候选）。</param>
/// <param name="LinkedToKnownBugCount">已链 KnownBug 的子集计数（重现已知 bug）。</param>
/// <param name="Hypothesis">人类可读 hypothesis 段（模板填充）。</param>
public sealed record CommonalityReport(
    int Total,
    IReadOnlyDictionary<string, int> BySeverity,
    IReadOnlyDictionary<string, int> ByCategory,
    IReadOnlyDictionary<string, int> ByStatus,
    string? DominantSeverity,
    string? DominantCategory,
    int NoiseFloorCount,
    int MajorOrCriticalCount,
    int LinkedToKnownBugCount,
    string Hypothesis);
