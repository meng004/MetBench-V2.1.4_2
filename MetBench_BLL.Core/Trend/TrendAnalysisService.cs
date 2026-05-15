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

    internal static IReadOnlyList<AnomalyBurst> DetectBursts(
        IReadOnlyList<MetBench_Domain.Anomaly> all,
        DateTime weekStartUtc,
        DateTime weekEndUtc,
        int historyWeeks = 4,
        double sigmaThreshold = 2.0)
    {
        var bursts = new List<AnomalyBurst>();
        var thisWeek = all.Where(a => a.DiscoveredAt >= weekStartUtc && a.DiscoveredAt < weekEndUtc).ToList();

        // 按 category 分（最近实际可用的"维度"字段）；m_pattern / sut 等需 join，先用 Category 兜底
        foreach (var g in thisWeek.GroupBy(a => a.Category ?? "uncategorized"))
        {
            var historyCounts = new List<double>();
            for (int i = 1; i <= historyWeeks; i++)
            {
                var s = weekStartUtc.AddDays(-7 * i);
                var e = s.AddDays(7);
                historyCounts.Add(all.Count(a => a.Category == g.Key
                    && a.DiscoveredAt >= s && a.DiscoveredAt < e));
            }
            var mean = historyCounts.Count == 0 ? 0.0 : historyCounts.Average();
            var stddev = StdDev(historyCounts, mean);
            var sigmas = stddev == 0 ? (g.Count() > mean ? double.PositiveInfinity : 0.0)
                                     : (g.Count() - mean) / stddev;
            if (sigmas >= sigmaThreshold)
            {
                bursts.Add(new AnomalyBurst(
                    Dimension: "category",
                    Key: g.Key,
                    Count: g.Count(),
                    Baseline: mean,
                    SigmasAbove: double.IsInfinity(sigmas) ? 99.9 : Math.Round(sigmas, 2)));
            }
        }
        return bursts;
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
