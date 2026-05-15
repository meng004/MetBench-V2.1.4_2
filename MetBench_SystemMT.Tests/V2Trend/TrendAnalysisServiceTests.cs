using MetBench_BLL.Trend;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Trend;

public sealed class TrendAnalysisServiceTests
{
    private static readonly DateTime WeekStart =
        new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ComputeWeekly_counts_executions_and_anomalies_in_window()
    {
        var (svc, fakes) = MakeService();
        AddExec(fakes, WeekStart.AddDays(1));
        AddExec(fakes, WeekStart.AddDays(3));
        AddExec(fakes, WeekStart.AddDays(-1)); // prev week
        AddAnomaly(fakes, WeekStart.AddDays(2));
        AddAnomaly(fakes, WeekStart.AddDays(-2)); // prev week

        var w = svc.ComputeWeekly(WeekStart);

        Assert.Equal(2, w.Executions);
        Assert.Equal(1, w.ExecutionsWoW);
        Assert.Equal(1, w.Anomalies);
        Assert.Equal(1, w.AnomaliesWoW);
    }

    [Fact]
    public void ComputeWeekly_anomaly_rate_computed_from_window()
    {
        var (svc, fakes) = MakeService();
        AddExec(fakes, WeekStart.AddDays(1));
        AddExec(fakes, WeekStart.AddDays(2));
        AddExec(fakes, WeekStart.AddDays(3));
        AddExec(fakes, WeekStart.AddDays(4));
        AddAnomaly(fakes, WeekStart.AddDays(2));

        var w = svc.ComputeWeekly(WeekStart);

        Assert.Equal(0.25, w.AnomalyRate, 3);
    }

    [Fact]
    public void ComputeWeekly_empty_window_returns_zero_rate()
    {
        var (svc, _) = MakeService();
        var w = svc.ComputeWeekly(WeekStart);
        Assert.Equal(0, w.Executions);
        Assert.Equal(0.0, w.AnomalyRate, 3);
        Assert.NotNull(w.Headline);
    }

    [Fact]
    public void DetectBursts_flags_category_above_two_sigma()
    {
        // baseline (4 prev weeks): "basin" 1, 1, 1, 1; this week 10 → way above 2σ
        var all = new List<MetBench_Domain.Anomaly>();
        for (int i = 1; i <= 4; i++)
            all.Add(MakeAnomaly(WeekStart.AddDays(-7 * i + 1), "basin"));
        for (int i = 0; i < 10; i++)
            all.Add(MakeAnomaly(WeekStart.AddDays(i % 7), "basin"));

        var bursts = TrendAnalysisService.DetectBursts(all,
            WeekStart, WeekStart.AddDays(7));

        Assert.Single(bursts);
        Assert.Equal("basin", bursts[0].Key);
        Assert.True(bursts[0].SigmasAbove >= 2.0);
    }

    [Fact]
    public void DetectBursts_no_burst_when_within_baseline()
    {
        var all = new List<MetBench_Domain.Anomaly>();
        for (int week = 1; week <= 4; week++)
            for (int j = 0; j < 5; j++)
                all.Add(MakeAnomaly(WeekStart.AddDays(-7 * week + j), "mc-floor"));
        for (int j = 0; j < 5; j++)
            all.Add(MakeAnomaly(WeekStart.AddDays(j), "mc-floor"));

        var bursts = TrendAnalysisService.DetectBursts(all,
            WeekStart, WeekStart.AddDays(7));

        Assert.Empty(bursts);
    }

    [Fact]
    public void ComputeWeekly_headline_includes_burst_warning()
    {
        var (svc, fakes) = MakeService();
        for (int prev = 1; prev <= 4; prev++)
            fakes.Anomalies.Data.Add(MakeAnomaly(WeekStart.AddDays(-7 * prev), "basin"));
        for (int i = 0; i < 8; i++)
            fakes.Anomalies.Data.Add(MakeAnomaly(WeekStart.AddDays(i % 7), "basin"));
        AddExec(fakes, WeekStart.AddDays(1));

        var w = svc.ComputeWeekly(WeekStart);

        Assert.Contains("burst", w.Headline);
    }

    [Fact]
    public void ExecutionsDelta_is_zero_when_prev_week_zero()
    {
        var w = new WeeklyReport(WeekStart, WeekStart.AddDays(7), 5, 0, 0, 0, 0, 0, 0, 0,
            new List<AnomalyBurst>(), "n/a");
        Assert.Equal(0.0, w.ExecutionsDelta);
    }

    // ===== fixtures =====

    private static (TrendAnalysisService, FakeBundle) MakeService()
    {
        var fakes = new FakeBundle();
        var svc = new TrendAnalysisService(fakes.Executions, fakes.Anomalies, fakes.Candidates, fakes.Bugs);
        return (svc, fakes);
    }

    private static void AddExec(FakeBundle fakes, DateTime when)
        => fakes.Executions.Data.Add(new Execution
        {
            IdExecution = Guid.NewGuid(),
            QueuedAt = when,
            Status = "ok",
        });

    private static void AddAnomaly(FakeBundle fakes, DateTime when)
        => fakes.Anomalies.Data.Add(MakeAnomaly(when, "uncategorized"));

    private static MetBench_Domain.Anomaly MakeAnomaly(DateTime when, string category)
        => new()
        {
            IdAnomaly = Guid.NewGuid(),
            DiscoveredAt = when,
            Category = category,
            Severity = "major",
            Status = "new",
        };
}

internal sealed class FakeBundle
{
    public FakeExecutionRepository Executions { get; } = new();
    public FakeAnomalyRepoT Anomalies { get; } = new();
    public FakeCandidateRepoT Candidates { get; } = new();
    public FakeBugRepoT Bugs { get; } = new();
}
