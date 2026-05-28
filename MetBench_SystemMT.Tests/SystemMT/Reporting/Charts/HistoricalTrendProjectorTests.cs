using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting.Charts;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Reporting.Charts;

public sealed class HistoricalTrendProjectorTests
{
    /// <summary>In-memory stub: returns canned records by MR name, most-recent first.</summary>
    private sealed class FakeRepository : ISystemMtResultRepository
    {
        public Dictionary<string, List<SystemMtResultRecord>> ByMr { get; } = new(StringComparer.Ordinal);

        public Task<IReadOnlyList<SystemMtResultRecord>> ListByMrNameAsync(
            string mrName, int limit = 100, CancellationToken cancellationToken = default)
        {
            if (!ByMr.TryGetValue(mrName, out var list))
            {
                return Task.FromResult<IReadOnlyList<SystemMtResultRecord>>(Array.Empty<SystemMtResultRecord>());
            }
            // Repository contract: most-recent first; cap at `limit`.
            var capped = list.Count <= limit ? list : list.GetRange(0, limit);
            return Task.FromResult<IReadOnlyList<SystemMtResultRecord>>(capped);
        }

        // Unused contract methods — return defaults to satisfy the interface.
        public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);
        public Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<SystemMtResultRecord?>(null);
        public Task<IReadOnlyList<SystemMtResultRecord>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SystemMtResultRecord>>(Array.Empty<SystemMtResultRecord>());
        public Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<SystemMtResultRecord>(Array.Empty<SystemMtResultRecord>(), 0, request.PageIndex, request.PageSize));
        public Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<SystemMtResultRecord>(Array.Empty<SystemMtResultRecord>(), 0, request.PageIndex, request.PageSize));
        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
        public Task<int> DeleteBatchAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }

    private static SystemMtResultRecord MakeRecord(double followUp, DateTimeOffset runAt, string valueName = "k_eff")
        => new()
        {
            Id = Guid.NewGuid(),
            MrName = "mr-x",
            ValueName = valueName,
            SourceValue = 0.0,
            FollowUpValue = followUp,
            RunAt = runAt,
        };

    [Fact]
    public async Task ProjectAsync_returns_points_in_chronological_order_oldest_first()
    {
        var repo = new FakeRepository();
        // Repository returns most-recent first; we feed 3 records newest → oldest.
        repo.ByMr["mr-x"] = new List<SystemMtResultRecord>
        {
            MakeRecord(3.0, new DateTimeOffset(2026, 5, 27, 12, 0, 0, TimeSpan.Zero)),
            MakeRecord(2.0, new DateTimeOffset(2026, 5, 26, 12, 0, 0, TimeSpan.Zero)),
            MakeRecord(1.0, new DateTimeOffset(2026, 5, 25, 12, 0, 0, TimeSpan.Zero)),
        };

        var projector = new HistoricalTrendProjector(repo);
        var fig = await projector.ProjectAsync("mr-x", lookbackRuns: 10);

        Assert.Equal(ChartFigureKind.HistoricalTrend, fig.Kind);
        var pts = fig.SeriesList[0].Points;
        Assert.Equal(3, pts.Count);
        Assert.Equal(1.0, pts[0].Y);
        Assert.Equal(2.0, pts[1].Y);
        Assert.Equal(3.0, pts[2].Y);
    }

    [Fact]
    public async Task ProjectAsync_returns_empty_series_when_repository_is_empty()
    {
        var repo = new FakeRepository();

        var projector = new HistoricalTrendProjector(repo);
        var fig = await projector.ProjectAsync("never-seen", lookbackRuns: 5);

        Assert.Equal(ChartFigureKind.HistoricalTrend, fig.Kind);
        Assert.Empty(fig.SeriesList[0].Points);
    }

    [Fact]
    public async Task ProjectAsync_respects_lookback_runs_limit()
    {
        var repo = new FakeRepository();
        var records = new List<SystemMtResultRecord>();
        for (var i = 0; i < 10; i++)
        {
            records.Add(MakeRecord(i, new DateTimeOffset(2026, 5, 27, 12, i, 0, TimeSpan.Zero)));
        }
        repo.ByMr["mr-x"] = records;

        var projector = new HistoricalTrendProjector(repo);
        var fig = await projector.ProjectAsync("mr-x", lookbackRuns: 3);

        Assert.Equal(3, fig.SeriesList[0].Points.Count);
    }

    [Fact]
    public async Task ProjectAsync_labels_each_point_with_iso8601_utc_run_at()
    {
        var repo = new FakeRepository();
        var runAt = new DateTimeOffset(2026, 5, 27, 9, 30, 15, TimeSpan.Zero);
        repo.ByMr["mr-x"] = new List<SystemMtResultRecord>
        {
            MakeRecord(1.0, runAt),
            MakeRecord(0.5, runAt.AddDays(-1)),
        };

        var projector = new HistoricalTrendProjector(repo);
        var fig = await projector.ProjectAsync("mr-x", lookbackRuns: 5);

        // Oldest-first ordering → index 1 is the newer record.
        // DateTime.ToString("o") on a UTC DateTime renders the "Z" zone designator.
        Assert.Equal("2026-05-27T09:30:15.0000000Z", fig.SeriesList[0].Points[1].Label);
    }

    [Fact]
    public async Task ProjectAsync_uses_record_value_name_for_y_axis_label()
    {
        var repo = new FakeRepository();
        repo.ByMr["mr-x"] = new List<SystemMtResultRecord>
        {
            MakeRecord(1.0, DateTimeOffset.UtcNow, valueName: "k_eff_std"),
        };

        var projector = new HistoricalTrendProjector(repo);
        var fig = await projector.ProjectAsync("mr-x", lookbackRuns: 5);

        Assert.Equal("k_eff_std", fig.YAxisLabel);
    }

    [Fact]
    public async Task ProjectAsync_uses_follow_up_value_when_empty_repo_so_no_record_value_name()
    {
        var repo = new FakeRepository();

        var projector = new HistoricalTrendProjector(repo);
        var fig = await projector.ProjectAsync("ghost", lookbackRuns: 5);

        Assert.Equal("follow_up_value", fig.YAxisLabel);
    }

    [Fact]
    public async Task ProjectAsync_throws_on_blank_mr_id()
    {
        var projector = new HistoricalTrendProjector(new FakeRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => projector.ProjectAsync("", 5));
        await Assert.ThrowsAsync<ArgumentException>(() => projector.ProjectAsync("   ", 5));
    }

    [Fact]
    public async Task ProjectAsync_throws_on_non_positive_lookback()
    {
        var projector = new HistoricalTrendProjector(new FakeRepository());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => projector.ProjectAsync("mr-x", 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => projector.ProjectAsync("mr-x", -1));
    }

    [Fact]
    public void Constructor_throws_on_null_repository()
    {
        Assert.Throws<ArgumentNullException>(() => new HistoricalTrendProjector(null!));
    }
}
