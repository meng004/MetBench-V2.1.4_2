using System.Collections.ObjectModel;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Anomaly;

/// <summary>
/// TDD 验证 P6.1 AnomalyService — 用 fake Repository 注入数据，验证
/// List/Filter/CommonalityAnalysis/状态转移/链 KnownBug 行为。
/// </summary>
public sealed class AnomalyServiceTests
{
    [Fact]
    public void List_filters_by_severity()
    {
        var data = new[]
        {
            MakeAnomaly("noise"),
            MakeAnomaly("major"),
            MakeAnomaly("major"),
            MakeAnomaly("critical"),
        };
        var svc = MakeService(data);

        var majors = svc.List(new AnomalyFilter(Severity: "major"));
        Assert.Equal(2, majors.Count);
    }

    [Fact]
    public void List_filters_by_status()
    {
        var data = new[]
        {
            MakeAnomaly(status: "new"),
            MakeAnomaly(status: "confirmed-bug"),
            MakeAnomaly(status: "confirmed-bug"),
        };
        var svc = MakeService(data);

        var confirmed = svc.List(new AnomalyFilter(Status: "confirmed-bug"));
        Assert.Equal(2, confirmed.Count);
    }

    [Fact]
    public void List_filters_by_linked_bug()
    {
        var data = new[]
        {
            MakeAnomaly(linkedBugId: 1),
            MakeAnomaly(linkedBugId: 1),
            MakeAnomaly(linkedBugId: 2),
            MakeAnomaly(),
        };
        var svc = MakeService(data);

        var linked1 = svc.List(new AnomalyFilter(LinkedKnownBugId: 1));
        Assert.Equal(2, linked1.Count);
    }

    [Fact]
    public void List_filters_by_date_range()
    {
        var data = new[]
        {
            MakeAnomaly(discoveredAt: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)),
            MakeAnomaly(discoveredAt: new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc)),
            MakeAnomaly(discoveredAt: new DateTime(2026, 5, 13, 0, 0, 0, DateTimeKind.Utc)),
        };
        var svc = MakeService(data);

        var lastWeek = svc.List(new AnomalyFilter(
            DiscoveredFrom: new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            DiscoveredUntil: new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(2, lastWeek.Count);
    }

    [Fact]
    public void AnalyzeCommonalities_empty_returns_zero_total()
    {
        var svc = MakeService(Array.Empty<MetBench_Domain.Anomaly>());
        var report = svc.AnalyzeCommonalities(Array.Empty<MetBench_Domain.Anomaly>());
        Assert.Equal(0, report.Total);
        Assert.Contains("No anomalies", report.Hypothesis);
    }

    [Fact]
    public void AnalyzeCommonalities_dominant_severity_picked()
    {
        var data = new[]
        {
            MakeAnomaly("major"),
            MakeAnomaly("major"),
            MakeAnomaly("noise"),
        };
        var svc = MakeService(data);
        var report = svc.AnalyzeCommonalities(data);
        Assert.Equal("major", report.DominantSeverity);
        Assert.Equal(2, report.MajorOrCriticalCount);
        Assert.Equal(1, report.NoiseFloorCount);
    }

    [Fact]
    public void AnalyzeCommonalities_hypothesis_includes_basin_when_dominant()
    {
        var data = new[]
        {
            MakeAnomaly("major", category: "basin"),
            MakeAnomaly("major", category: "basin"),
            MakeAnomaly("noise", category: "mc-floor"),
        };
        var svc = MakeService(data);
        var report = svc.AnalyzeCommonalities(data);
        Assert.Equal("basin", report.DominantCategory);
        Assert.Contains("convergence basin", report.Hypothesis);
    }

    [Fact]
    public void AnalyzeCommonalities_hypothesis_includes_mcfloor_when_dominant()
    {
        var data = new[]
        {
            MakeAnomaly("noise", category: "mc-floor"),
            MakeAnomaly("noise", category: "mc-floor"),
        };
        var svc = MakeService(data);
        var report = svc.AnalyzeCommonalities(data);
        Assert.Contains("particle count", report.Hypothesis);
    }

    [Fact]
    public void AnalyzeCommonalities_counts_linked_known_bugs()
    {
        var data = new[]
        {
            MakeAnomaly("major", linkedBugId: 1),
            MakeAnomaly("major"),
            MakeAnomaly("major", linkedBugId: 6),
        };
        var svc = MakeService(data);
        var report = svc.AnalyzeCommonalities(data);
        Assert.Equal(2, report.LinkedToKnownBugCount);
    }

    [Fact]
    public void TransitionStatus_updates_and_writes_audit()
    {
        var anomalyId = Guid.NewGuid();
        var data = new[] { MakeAnomalyWithId(anomalyId, status: "new") };
        var auditRepo = new FakeAuditLogRepository();
        var svc = MakeService(data, auditRepo);

        var ok = svc.TransitionStatus(anomalyId, "investigating", "Looking into Case 6", "alice");
        Assert.True(ok);
        Assert.Equal("investigating", data[0].Status);
        Assert.Equal("Looking into Case 6", data[0].Notes);
        Assert.Single(auditRepo.Logs);
        Assert.Equal("anomaly.status-change", auditRepo.Logs[0].Action);
    }

    [Fact]
    public void LinkToKnownBug_sets_id_and_writes_audit()
    {
        var anomalyId = Guid.NewGuid();
        var data = new[] { MakeAnomalyWithId(anomalyId) };
        var auditRepo = new FakeAuditLogRepository();
        var svc = MakeService(data, auditRepo);

        var ok = svc.LinkToKnownBug(anomalyId, knownBugId: 6, "alice");
        Assert.True(ok);
        Assert.Equal(6, data[0].LinkedKnownBugId);
        Assert.Single(auditRepo.Logs);
        Assert.Equal("anomaly.link-known-bug", auditRepo.Logs[0].Action);
    }

    [Fact]
    public void UnlinkKnownBug_clears_id_and_writes_audit()
    {
        var anomalyId = Guid.NewGuid();
        var data = new[] { MakeAnomalyWithId(anomalyId, linkedBugId: 6) };
        var auditRepo = new FakeAuditLogRepository();
        var svc = MakeService(data, auditRepo);

        var ok = svc.UnlinkKnownBug(anomalyId, "alice");
        Assert.True(ok);
        Assert.Null(data[0].LinkedKnownBugId);
        Assert.Equal("anomaly.unlink-known-bug", auditRepo.Logs[0].Action);
    }

    [Fact]
    public void TransitionStatus_missing_anomaly_returns_false()
    {
        var svc = MakeService(Array.Empty<MetBench_Domain.Anomaly>());
        var ok = svc.TransitionStatus(Guid.NewGuid(), "investigating", null, "alice");
        Assert.False(ok);
    }

    // =================== fixtures ===================

    private static MetBench_Domain.Anomaly MakeAnomaly(
        string severity = "minor",
        string status = "new",
        string category = "uncategorized",
        int? linkedBugId = null,
        DateTime? discoveredAt = null)
        => MakeAnomalyWithId(Guid.NewGuid(), severity, status, category, linkedBugId, discoveredAt);

    private static MetBench_Domain.Anomaly MakeAnomalyWithId(
        Guid id,
        string severity = "minor",
        string status = "new",
        string category = "uncategorized",
        int? linkedBugId = null,
        DateTime? discoveredAt = null)
        => new()
        {
            IdAnomaly = id,
            ResultId = Guid.NewGuid(),
            Severity = severity,
            Status = status,
            Category = category,
            LinkedKnownBugId = linkedBugId,
            DiscoveredAt = discoveredAt ?? DateTime.UtcNow,
        };

    private static AnomalyService MakeService(
        IReadOnlyList<MetBench_Domain.Anomaly> data,
        FakeAuditLogRepository? auditRepo = null)
    {
        var anomalyRepo = new FakeAnomalyRepository(data);
        return new AnomalyService(anomalyRepo, auditRepo ?? new FakeAuditLogRepository());
    }
}

// =================== Fake repositories ===================

internal sealed class FakeAnomalyRepository : IAnomalyRepository
{
    private readonly List<MetBench_Domain.Anomaly> _data;

    public FakeAnomalyRepository(IEnumerable<MetBench_Domain.Anomaly> data)
    {
        _data = data.ToList();
    }

    public ObservableCollection<MetBench_Domain.Anomaly> GetAll() => new(_data);
    public MetBench_Domain.Anomaly? Get(Guid id) => _data.FirstOrDefault(a => a.IdAnomaly == id);
    public ObservableCollection<MetBench_Domain.Anomaly> Get(MetBench_Domain.Anomaly template) => new(_data);

    public bool Add(MetBench_Domain.Anomaly entity) { _data.Add(entity); return true; }

    public bool Modify(MetBench_Domain.Anomaly entity)
    {
        var idx = _data.FindIndex(a => a.IdAnomaly == entity.IdAnomaly);
        if (idx < 0) return false;
        _data[idx] = entity;
        return true;
    }

    public bool Remove(MetBench_Domain.Anomaly entity) => _data.Remove(entity);

    public ObservableCollection<MetBench_Domain.Anomaly> GetPage(int pageIndex, int pageSize)
        => new(_data.Skip(pageIndex * pageSize).Take(pageSize).ToList());

    public int Count() => _data.Count;

    public MetBench_Domain.Anomaly? GetByResult(Guid resultId)
        => _data.FirstOrDefault(a => a.ResultId == resultId);

    public ObservableCollection<MetBench_Domain.Anomaly> GetByStatus(string status)
        => new(_data.Where(a => a.Status == status).ToList());

    public ObservableCollection<MetBench_Domain.Anomaly> GetByLinkedBug(int knownBugId)
        => new(_data.Where(a => a.LinkedKnownBugId == knownBugId).ToList());
}

internal sealed class FakeAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> Logs { get; } = new();

    public ObservableCollection<AuditLog> GetAll() => new(Logs);
    public AuditLog? Get(Guid id) => Logs.FirstOrDefault(l => l.IdLog == id);
    public ObservableCollection<AuditLog> Get(AuditLog template) => new(Logs);

    public bool Add(AuditLog entity) { Logs.Add(entity); return true; }
    public bool Modify(AuditLog entity) => true;
    public bool Remove(AuditLog entity) => Logs.Remove(entity);

    public ObservableCollection<AuditLog> GetPage(int p, int s)
        => new(Logs.Skip(p * s).Take(s).ToList());
    public int Count() => Logs.Count;

    public ObservableCollection<AuditLog> GetByDateRange(DateTime from, DateTime to)
        => new(Logs.Where(l => l.Timestamp >= from && l.Timestamp < to).ToList());
    public ObservableCollection<AuditLog> GetByAction(string action)
        => new(Logs.Where(l => l.Action == action).ToList());
    public ObservableCollection<AuditLog> GetByTargetEntity(string entityType, string entityId)
        => new(Logs.Where(l => l.TargetEntityType == entityType && l.TargetEntityId == entityId).ToList());
}
