using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Anomaly;

/// <summary>
/// PR-1 (T5 anomaly workflow closure) acceptance: AnomalyOrphanSweeper removes
/// Anomaly rows whose ResultId no longer resolves to a SystemMtResultRecord,
/// leaves live anomalies untouched, is idempotent, and surfaces partial-failure
/// modes via the 3-segment AnomalyOrphanSweepResult.
/// </summary>
public sealed class AnomalyOrphanSweeperTests
{
    [Fact]
    public async Task SweepAsync_removes_orphan_anomalies_and_retains_live_ones()
    {
        var anomalies = new FakeAnomalyRepo();
        var results = new FakeResultRepo();
        var liveResultId = Guid.NewGuid();
        results.Seed(liveResultId.ToString());

        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = liveResultId });
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });

        var sweeper = new AnomalyOrphanSweeper(anomalies, results);
        var result = await sweeper.SweepAsync();

        Assert.Equal(3, result.SweptCount);
        Assert.Equal(1, result.RetainedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Single(anomalies.Data);
        Assert.Equal(liveResultId, anomalies.Data[0].ResultId);
    }

    [Fact]
    public async Task SweepAsync_when_all_live_returns_zero_swept()
    {
        var anomalies = new FakeAnomalyRepo();
        var results = new FakeResultRepo();
        var liveA = Guid.NewGuid();
        var liveB = Guid.NewGuid();
        results.Seed(liveA.ToString());
        results.Seed(liveB.ToString());
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = liveA });
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = liveB });

        var sweeper = new AnomalyOrphanSweeper(anomalies, results);
        var result = await sweeper.SweepAsync();

        Assert.Equal(0, result.SweptCount);
        Assert.Equal(2, result.RetainedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(2, anomalies.Data.Count);
    }

    [Fact]
    public async Task SweepAsync_when_all_orphan_clears_collection()
    {
        var anomalies = new FakeAnomalyRepo();
        var results = new FakeResultRepo();
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });

        var sweeper = new AnomalyOrphanSweeper(anomalies, results);
        var result = await sweeper.SweepAsync();

        Assert.Equal(2, result.SweptCount);
        Assert.Equal(0, result.RetainedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(anomalies.Data);
    }

    [Fact]
    public async Task SweepAsync_is_idempotent_when_run_twice()
    {
        var anomalies = new FakeAnomalyRepo();
        var results = new FakeResultRepo();
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });

        var sweeper = new AnomalyOrphanSweeper(anomalies, results);
        var first = await sweeper.SweepAsync();
        var second = await sweeper.SweepAsync();

        Assert.Equal(1, first.SweptCount);
        Assert.Equal(0, second.SweptCount);
        Assert.Equal(0, second.RetainedCount);
        Assert.Equal(0, second.FailedCount);
    }

    [Fact]
    public async Task SweepAsync_when_result_repo_throws_counts_failure_and_does_not_abort()
    {
        var anomalies = new FakeAnomalyRepo();
        var results = new ThrowingResultRepo();
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });
        anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });

        var sweeper = new AnomalyOrphanSweeper(anomalies, results);
        var result = await sweeper.SweepAsync();

        Assert.Equal(0, result.SweptCount);
        Assert.Equal(2, result.FailedCount);
        Assert.Equal(2, anomalies.Data.Count); // nothing deleted on failure
    }

    [Fact]
    public void Constructor_rejects_null_repos()
    {
        var results = new FakeResultRepo();
        var anomalies = new FakeAnomalyRepo();
        Assert.Throws<ArgumentNullException>(() => new AnomalyOrphanSweeper(null!, results));
        Assert.Throws<ArgumentNullException>(() => new AnomalyOrphanSweeper(anomalies, null!));
    }

    [Fact]
    public async Task SweepAsync_respects_cancellation_token()
    {
        var anomalies = new FakeAnomalyRepo();
        var results = new FakeResultRepo();
        for (int i = 0; i < 5; i++)
            anomalies.Data.Add(new MetBench_Domain.Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var sweeper = new AnomalyOrphanSweeper(anomalies, results);

        await Assert.ThrowsAsync<OperationCanceledException>(() => sweeper.SweepAsync(cts.Token));
    }

    // ====== Fakes ======

    private sealed class FakeAnomalyRepo : IAnomalyRepository
    {
        public List<MetBench_Domain.Anomaly> Data { get; } = new();
        public ObservableCollection<MetBench_Domain.Anomaly> GetAll() => new(Data);
        public MetBench_Domain.Anomaly? Get(Guid id) => Data.FirstOrDefault(a => a.IdAnomaly == id);
        public ObservableCollection<MetBench_Domain.Anomaly> Get(MetBench_Domain.Anomaly e) => new(Data);
        public bool Add(MetBench_Domain.Anomaly e) { Data.Add(e); return true; }
        public bool Modify(MetBench_Domain.Anomaly e) { var i = Data.FindIndex(x => x.IdAnomaly == e.IdAnomaly); if (i < 0) return false; Data[i] = e; return true; }
        public bool Remove(MetBench_Domain.Anomaly e) => Data.Remove(e);
        public ObservableCollection<MetBench_Domain.Anomaly> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
        public int Count() => Data.Count;
        public MetBench_Domain.Anomaly? GetByResult(Guid id) => Data.FirstOrDefault(a => a.ResultId == id);
        public ObservableCollection<MetBench_Domain.Anomaly> GetByStatus(string s) => new(Data.Where(a => a.Status == s).ToList());
        public ObservableCollection<MetBench_Domain.Anomaly> GetByLinkedBug(int id) => new(Data.Where(a => a.LinkedKnownBugId == id).ToList());
    }

    private class FakeResultRepo : ISystemMtResultRepository
    {
        protected readonly HashSet<string> _seeded = new();
        public void Seed(string id) => _seeded.Add(id);
        public virtual Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<SystemMtResultRecord?>(_seeded.Contains(id) ? new SystemMtResultRecord { Id = Guid.Parse(id) } : null);
        public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> SaveAsync(SystemMtResultRecord record, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SystemMtResultRecord>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SystemMtResultRecord>> ListByMrNameAsync(string mrName, int limit = 100, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> DeleteBatchAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ThrowingResultRepo : FakeResultRepo
    {
        public override Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("simulated repo failure");
    }
}
