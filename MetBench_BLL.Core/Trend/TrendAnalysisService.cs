using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Trend;

/// <summary>
/// 趋势分析 —— 给定一周窗口，对比上周、再计算异常爆发。
/// </summary>
/// <remarks>
/// 算法（粗）：
/// <list type="number">
///   <item>窗口本周 W = [start, start+7d)、上周 W-1 = [start-7d, start)</item>
///   <item>计 Executions、Anomalies、anomaly-rate = anomalies / executions</item>
///   <item>异常爆发：把异常按 (SUT / mr-code / metapattern) 分组，跟历史 4 周均值/方差比，
///     超 baseline + 2σ 标 burst</item>
///   <item>Headline 生成一句话总结，UI 直接展示</item>
/// </list>
/// 服务无状态、纯查询。
/// </remarks>
public sealed class TrendAnalysisService
{
    private readonly IExecutionRepository _executions;
    private readonly IAnomalyRepository _anomalies;
    private readonly ICandidateMRRepository _candidates;
    private readonly IKnownBugRepository _bugs;

    public TrendAnalysisService(
        IExecutionRepository executions,
        IAnomalyRepository anomalies,
        ICandidateMRRepository candidates,
        IKnownBugRepository bugs)
    {
        _executions = executions;
        _anomalies = anomalies;
        _candidates = candidates;
        _bugs = bugs;
    }

    public WeeklyReport ComputeWeekly(DateTime weekStartUtc)
    {
        var weekEnd = weekStartUtc.AddDays(7);
        var prevStart = weekStartUtc.AddDays(-7);

        var thisExecs = _executions.GetByDateRange(weekStartUtc, weekEnd).ToList();
        var prevExecs = _executions.GetByDateRange(prevStart, weekStartUtc).ToList();
        var allAnomalies = _anomalies.GetAll().ToList();
        var thisAnomalies = allAnomalies.Where(a => a.DiscoveredAt >= weekStartUtc && a.DiscoveredAt < weekEnd).ToList();
        var prevAnomalies = allAnomalies.Where(a => a.DiscoveredAt >= prevStart && a.DiscoveredAt < weekStartUtc).ToList();

        var newBugs = _bugs.GetAll().Count(b => b.IdBug != 0); // placeholder if Bugs has no CreatedAt
        var promoted = _candidates.GetByStatus("promoted").Count;

        var anomalyRate = thisExecs.Count == 0 ? 0.0 : (double)thisAnomalies.Count / thisExecs.Count;
        var prevAnomalyRate = prevExecs.Count == 0 ? 0.0 : (double)prevAnomalies.Count / prevExecs.Count;

        var bursts = DetectBursts(allAnomalies, weekStartUtc, weekEnd);
        var headline = BuildHeadline(thisExecs.Count, prevExecs.Count,
            thisAnomalies.Count, prevAnomalies.Count, anomalyRate, prevAnomalyRate, bursts);

        return new WeeklyReport(
            WeekStartUtc: weekStartUtc,
            WeekEndUtc: weekEnd,
            Executions: thisExecs.Count,
            ExecutionsWoW: prevExecs.Count,
            Anomalies: thisAnomalies.Count,
            AnomaliesWoW: prevAnomalies.Count,
            NewKnownBugs: newBugs,
            PromotedMRs: promoted,
            AnomalyRate: anomalyRate,
            AnomalyRateWoW: prevAnomalyRate,
            Bursts: bursts,
            Headline: headline);
    }

    /// <summary>原 API：按 Category 分维度。保留向后兼容。</summary>
    internal static IReadOnlyList<AnomalyBurst> DetectBursts(
        IReadOnlyList<MetBench_Domain.Anomaly> all,
        DateTime weekStartUtc,
        DateTime weekEndUtc,
        int historyWeeks = 4,
        double sigmaThreshold = 2.0)
        => DetectBurstsByDimension(all, weekStartUtc, weekEndUtc,
            dimensionName: "category",
            keySelector: a => a.Category ?? "uncategorized",
            historyWeeks, sigmaThreshold);

    /// <summary>
    /// F6 新 API：按任意维度（caller 提供 keySelector）算 burst。
    /// 推荐 dimensionName 取值：category / sut / mr-code / metapattern / severity。
    /// </summary>
    /// <remarks>
    /// 维度需要的字段（如 sut_id / mr_code）不在 Anomaly 实体上 — caller 应预先 join
    /// (Result → Execution → MRInstance → MRBinding → MR/Application) 把映射 dict 传入或写
    /// keySelector lambda 即可。本方法对数据层零依赖，便于单测。
    /// </remarks>
    public static IReadOnlyList<AnomalyBurst> DetectBurstsByDimension(
        IReadOnlyList<MetBench_Domain.Anomaly> all,
        DateTime weekStartUtc,
        DateTime weekEndUtc,
        string dimensionName,
        Func<MetBench_Domain.Anomaly, string> keySelector,
        int historyWeeks = 4,
        double sigmaThreshold = 2.0)
    {
        var bursts = new List<AnomalyBurst>();
        var thisWeek = all.Where(a => a.DiscoveredAt >= weekStartUtc && a.DiscoveredAt < weekEndUtc).ToList();

        foreach (var g in thisWeek.GroupBy(keySelector))
        {
            var historyCounts = new List<double>();
            for (int i = 1; i <= historyWeeks; i++)
            {
                var s = weekStartUtc.AddDays(-7 * i);
                var e = s.AddDays(7);
                historyCounts.Add(all.Count(a => keySelector(a) == g.Key
                    && a.DiscoveredAt >= s && a.DiscoveredAt < e));
            }
            var mean = historyCounts.Count == 0 ? 0.0 : historyCounts.Average();
            var stddev = StdDev(historyCounts, mean);
            var sigmas = stddev == 0 ? (g.Count() > mean ? double.PositiveInfinity : 0.0)
                                     : (g.Count() - mean) / stddev;
            if (sigmas >= sigmaThreshold)
            {
                bursts.Add(new AnomalyBurst(
                    Dimension: dimensionName,
                    Key: g.Key,
                    Count: g.Count(),
                    Baseline: mean,
                    SigmasAbove: double.IsInfinity(sigmas) ? 99.9 : Math.Round(sigmas, 2)));
            }
        }
        return bursts;
    }

    /// <summary>
    /// 多维度并发计算 — 一次过 caller 给的所有 (dimensionName, keySelector) 二元组。
    /// 重复 burst 被合并（dimension + key 唯一）。
    /// </summary>
    public static IReadOnlyList<AnomalyBurst> DetectBurstsMultiDim(
        IReadOnlyList<MetBench_Domain.Anomaly> all,
        DateTime weekStartUtc,
        DateTime weekEndUtc,
        IReadOnlyList<(string dimensionName, Func<MetBench_Domain.Anomaly, string> keySelector)> dimensions,
        int historyWeeks = 4,
        double sigmaThreshold = 2.0)
    {
        var combined = new List<AnomalyBurst>();
        foreach (var (dim, sel) in dimensions)
        {
            combined.AddRange(DetectBurstsByDimension(all, weekStartUtc, weekEndUtc, dim, sel, historyWeeks, sigmaThreshold));
        }
        return combined;
    }

    private static double StdDev(IReadOnlyList<double> xs, double mean)
    {
        if (xs.Count < 2) return 0.0;
        var sumSq = xs.Sum(x => (x - mean) * (x - mean));
        return Math.Sqrt(sumSq / (xs.Count - 1));
    }

    private static string BuildHeadline(
        int execs, int prevExecs, int anomalies, int prevAnomalies,
        double rate, double prevRate, IReadOnlyList<AnomalyBurst> bursts)
    {
        var parts = new List<string>();
        parts.Add($"{execs} executions this week ({Delta(execs, prevExecs)} vs prev).");
        parts.Add($"{anomalies} anomalies ({Delta(anomalies, prevAnomalies)}).");
        parts.Add($"anomaly-rate {rate:P1} ({(rate - prevRate):+0.0%;-0.0%;0.0%} vs prev).");
        if (bursts.Count > 0)
            parts.Add($"⚠ {bursts.Count} burst(s) detected: " +
                string.Join(", ", bursts.Select(b => $"{b.Dimension}={b.Key}({b.SigmasAbove}σ)")));
        return string.Join(" ", parts);
    }

    private static string Delta(int a, int b)
        => b == 0 ? (a > 0 ? "+∞%" : "0%") : $"{(a - b) * 100.0 / b:+0;-0;0}%";
}
