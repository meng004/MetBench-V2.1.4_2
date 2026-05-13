using MetBench_BLL.Mutation;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Mutation;

public sealed class MutationCampaignServiceTests
{
    [Fact]
    public async Task RunAsync_5x5_matrix_writes_all_cells()
    {
        var (svc, campaigns, results, _) = MakeService();
        var spec = new MutationCampaignSpec(
            Name: "smoke-5x5",
            MutantIds: Enumerable.Range(1, 5).ToArray(),
            MRBindingIds: Enumerable.Range(1, 5).ToArray(),
            SampleCasePaths: new[] { "/tmp/case.in" });

        var summary = await svc.RunAsync(spec, FixedDetectionRunner(0.6), actor: "alice");

        Assert.Equal(25, summary.TotalCells);
        Assert.Single(campaigns.Data);
        Assert.Equal("ok", campaigns.Data[0].Status);
        Assert.Equal(25, results.Data.Count);
    }

    [Fact]
    public async Task RunAsync_detection_rate_arithmetic_correct()
    {
        var (svc, _, _, _) = MakeService();
        // 10 cells: 6 detected, 4 missed → detection-rate = 0.6
        var spec = new MutationCampaignSpec(
            Name: "math-check",
            MutantIds: Enumerable.Range(1, 10).ToArray(),
            MRBindingIds: new[] { 1 },
            SampleCasePaths: new[] { "/tmp/case.in" });

        var counter = 0;
        var summary = await svc.RunAsync(spec,
            cellRunner: (input, ct) =>
            {
                var outcome = counter++ < 6 ? "detected" : "missed";
                return Task.FromResult(new MutationCellOutcome(Guid.NewGuid(), outcome, 0.0, 0.0, null));
            },
            actor: "alice");

        Assert.Equal(10, summary.TotalCells);
        Assert.Equal(6, summary.Detected);
        Assert.Equal(4, summary.Missed);
        Assert.Equal(0.6, summary.DetectionRate, 3);
        Assert.Equal(1.0, summary.Coverage, 3);
    }

    [Fact]
    public async Task RunAsync_not_affected_excluded_from_detection_rate()
    {
        var (svc, _, _, _) = MakeService();
        var spec = new MutationCampaignSpec(
            Name: "n-a-check",
            MutantIds: Enumerable.Range(1, 10).ToArray(),
            MRBindingIds: new[] { 1 },
            SampleCasePaths: new[] { "/tmp/case.in" });

        var counter = 0;
        var summary = await svc.RunAsync(spec,
            cellRunner: (input, ct) =>
            {
                var n = counter++;
                var outcome = n < 4 ? "detected" : (n < 6 ? "missed" : "not-affected");
                return Task.FromResult(new MutationCellOutcome(Guid.NewGuid(), outcome, 0.0, 0.0, null));
            },
            actor: "alice");

        Assert.Equal(10, summary.TotalCells);
        Assert.Equal(4, summary.Detected);
        Assert.Equal(2, summary.Missed);
        Assert.Equal(4, summary.NotAffected);
        // detection rate excludes not-affected: 4 / 6 = 0.667
        Assert.Equal(0.667, summary.DetectionRate, 2);
        Assert.Equal(0.6, summary.Coverage, 2);
    }

    [Fact]
    public async Task RunAsync_records_outcome_categories_correctly()
    {
        var (svc, _, results, _) = MakeService();
        var spec = new MutationCampaignSpec(
            Name: "outcomes",
            MutantIds: new[] { 1, 2, 3, 4 },
            MRBindingIds: new[] { 1 },
            SampleCasePaths: new[] { "/tmp/c.in" });
        var outcomes = new[] { "detected", "missed", "error", "not-affected" };
        var i = 0;
        var summary = await svc.RunAsync(spec,
            cellRunner: (input, ct) =>
                Task.FromResult(new MutationCellOutcome(Guid.NewGuid(), outcomes[i++], null, null, null)),
            actor: "alice");

        Assert.Equal(1, summary.Detected);
        Assert.Equal(1, summary.Missed);
        Assert.Equal(1, summary.Errors);
        Assert.Equal(1, summary.NotAffected);
        Assert.Equal(4, results.Data.Count);
    }

    [Fact]
    public async Task RunAsync_runner_throw_records_error_cell_continues()
    {
        var (svc, _, results, _) = MakeService();
        var spec = new MutationCampaignSpec(
            Name: "throwing-runner",
            MutantIds: new[] { 1, 2, 3 },
            MRBindingIds: new[] { 1 },
            SampleCasePaths: new[] { "/tmp/c.in" });
        var i = 0;
        var summary = await svc.RunAsync(spec,
            cellRunner: (input, ct) =>
            {
                if (i++ == 1)
                    throw new InvalidOperationException("kaboom");
                return Task.FromResult(new MutationCellOutcome(Guid.NewGuid(), "detected", 0.0, 0.0, null));
            },
            actor: "alice");

        Assert.Equal(3, summary.TotalCells);
        Assert.Equal(2, summary.Detected);
        Assert.Equal(1, summary.Errors);
        Assert.Equal(3, results.Data.Count);
        Assert.Contains(results.Data, r => r.Outcome == "error" && r.Notes!.Contains("kaboom"));
    }

    [Fact]
    public async Task RunAsync_reports_progress_per_cell()
    {
        var (svc, _, _, _) = MakeService();
        var spec = new MutationCampaignSpec(
            Name: "progress",
            MutantIds: new[] { 1, 2 },
            MRBindingIds: new[] { 1 },
            SampleCasePaths: new[] { "/tmp/c.in" });

        var progressEvents = new List<MutationCellProgress>();
        var progress = new SyncProgress<MutationCellProgress>(progressEvents.Add);

        await svc.RunAsync(spec, FixedDetectionRunner(1.0), "alice", progress: progress);

        Assert.Equal(2, progressEvents.Count);
        Assert.Equal(2, progressEvents.Last().Total);
    }

    [Fact]
    public async Task RunAsync_cancellation_marks_campaign_cancelled()
    {
        var (svc, campaigns, _, _) = MakeService();
        var spec = new MutationCampaignSpec(
            Name: "cancel",
            MutantIds: Enumerable.Range(1, 20).ToArray(),
            MRBindingIds: new[] { 1 },
            SampleCasePaths: new[] { "/tmp/c.in" });
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(1);

        await svc.RunAsync(spec,
            cellRunner: async (input, ct) =>
            {
                await Task.Delay(20, ct);
                return new MutationCellOutcome(Guid.NewGuid(), "detected", 0.0, 0.0, null);
            },
            actor: "alice",
            ct: cts.Token);

        Assert.Single(campaigns.Data);
        Assert.Equal("cancelled", campaigns.Data[0].Status);
    }

    [Fact]
    public async Task RunAsync_writes_audit_log_on_completion()
    {
        var (svc, _, _, audit) = MakeService();
        var spec = new MutationCampaignSpec(
            Name: "audit-test",
            MutantIds: new[] { 1 },
            MRBindingIds: new[] { 1 },
            SampleCasePaths: new[] { "/tmp/c.in" });

        await svc.RunAsync(spec, FixedDetectionRunner(1.0), actor: "alice");

        Assert.Contains(audit.Logs, l => l.Action == "mutation-campaign.run");
    }

    // ===== fixtures =====

    private static (MutationCampaignService, FakeMutationCampaignRepository, FakeMutationResultRepository, FakeMutationAuditLogRepository) MakeService()
    {
        var campaigns = new FakeMutationCampaignRepository();
        var results = new FakeMutationResultRepository();
        var audit = new FakeMutationAuditLogRepository();
        return (new MutationCampaignService(campaigns, results, audit), campaigns, results, audit);
    }

    private sealed class SyncProgress<T> : IProgress<T>
    {
        private readonly Action<T> _action;
        public SyncProgress(Action<T> action) { _action = action; }
        public void Report(T value) => _action(value);
    }

    private static MutationCellRunner FixedDetectionRunner(double detectedFraction)
    {
        var counter = 0;
        return (input, ct) =>
        {
            counter++;
            var detected = (counter / (double)counter) <= detectedFraction;
            return Task.FromResult(new MutationCellOutcome(
                Guid.NewGuid(),
                detected ? "detected" : "missed",
                ObservedDelta: 0.1, ExpectedDelta: 0.0, Notes: null));
        };
    }
}
