using MetBench_BLL.Trend;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Trend;

/// <summary>
/// F6 测试：TrendAnalysisService.DetectBurstsByDimension 多维度 burst 检测。
/// </summary>
public sealed class MultiDimBurstDetectionTests
{
    private static readonly DateTime WeekStart =
        new(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DetectBurstsByDimension_supports_arbitrary_key_selector()
    {
        // 模拟 sut 维度：用户事先 join 好 Anomaly→Execution→Application 把 sut 名塞 Notes 字段
        var anomalies = new List<MetBench_Domain.Anomaly>();
        for (int w = 1; w <= 4; w++)
            anomalies.Add(MakeAnomaly(WeekStart.AddDays(-7 * w), notes: "openmoc"));
        for (int i = 0; i < 10; i++)
            anomalies.Add(MakeAnomaly(WeekStart.AddDays(i % 7), notes: "openmoc"));

        var bursts = TrendAnalysisService.DetectBurstsByDimension(
            anomalies, WeekStart, WeekStart.AddDays(7),
            dimensionName: "sut",
            keySelector: a => a.Notes ?? "unknown");

        Assert.Single(bursts);
        Assert.Equal("sut", bursts[0].Dimension);
        Assert.Equal("openmoc", bursts[0].Key);
    }

    [Fact]
    public void DetectBurstsMultiDim_combines_results_from_multiple_dimensions()
    {
        var anomalies = new List<MetBench_Domain.Anomaly>();
        // sut=openmoc burst this week (10 vs baseline 1)
        for (int w = 1; w <= 4; w++)
            anomalies.Add(MakeAnomaly(WeekStart.AddDays(-7 * w), notes: "openmoc", category: "noise"));
        for (int i = 0; i < 10; i++)
            anomalies.Add(MakeAnomaly(WeekStart.AddDays(i % 7), notes: "openmoc", category: "basin"));

        var bursts = TrendAnalysisService.DetectBurstsMultiDim(
            anomalies, WeekStart, WeekStart.AddDays(7),
            dimensions: new (string, Func<MetBench_Domain.Anomaly, string>)[]
            {
                ("sut", a => a.Notes ?? "unknown"),
                ("category", a => a.Category ?? "uncategorized"),
            });

        // sut=openmoc 跑出 burst，category=basin 也跑出 burst
        Assert.Equal(2, bursts.Count);
        Assert.Contains(bursts, b => b.Dimension == "sut" && b.Key == "openmoc");
        Assert.Contains(bursts, b => b.Dimension == "category" && b.Key == "basin");
    }

    [Fact]
    public void DetectBurstsByDimension_quiet_dimension_yields_no_bursts()
    {
        var anomalies = new List<MetBench_Domain.Anomaly>();
        for (int w = 0; w <= 4; w++)
            for (int j = 0; j < 5; j++)
                anomalies.Add(MakeAnomaly(WeekStart.AddDays(-7 * w + j), notes: "openmc"));

        var bursts = TrendAnalysisService.DetectBurstsByDimension(
            anomalies, WeekStart, WeekStart.AddDays(7),
            dimensionName: "sut",
            keySelector: a => a.Notes ?? "unknown");

        Assert.Empty(bursts);  // 5 → 5 → 5 → 5 → 5，每周一致，无 burst
    }

    [Fact]
    public void Backwards_compat_DetectBursts_still_works_via_category()
    {
        var anomalies = new List<MetBench_Domain.Anomaly>();
        for (int w = 1; w <= 4; w++)
            anomalies.Add(MakeAnomaly(WeekStart.AddDays(-7 * w), category: "basin"));
        for (int i = 0; i < 10; i++)
            anomalies.Add(MakeAnomaly(WeekStart.AddDays(i % 7), category: "basin"));

        var bursts = TrendAnalysisService.DetectBursts(
            anomalies, WeekStart, WeekStart.AddDays(7));

        Assert.Single(bursts);
        Assert.Equal("category", bursts[0].Dimension);
        Assert.Equal("basin", bursts[0].Key);
    }

    private static MetBench_Domain.Anomaly MakeAnomaly(
        DateTime when,
        string category = "uncategorized",
        string? notes = null)
        => new()
        {
            IdAnomaly = Guid.NewGuid(),
            DiscoveredAt = when,
            Category = category,
            Severity = "major",
            Status = "new",
            Notes = notes,
        };
}
