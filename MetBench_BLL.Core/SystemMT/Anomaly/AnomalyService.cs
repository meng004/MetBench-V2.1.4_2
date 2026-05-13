using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// AnomalyService 默认实现 — 通过 Repository 操作 LiteDB，附带 AuditLog 写入。
/// </summary>
public sealed class AnomalyService : IAnomalyService
{
    private readonly IAnomalyRepository _anomalies;
    private readonly IAuditLogRepository _auditLog;

    public AnomalyService(IAnomalyRepository anomalies, IAuditLogRepository auditLog)
    {
        _anomalies = anomalies;
        _auditLog = auditLog;
    }

    // ====== Query ======

    public IReadOnlyList<MetBench_Domain.Anomaly> List(AnomalyFilter filter)
    {
        var all = _anomalies.GetAll();
        var query = all.AsEnumerable();

        if (!string.IsNullOrEmpty(filter.Severity))
            query = query.Where(a => a.Severity == filter.Severity);
        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(a => a.Status == filter.Status);
        if (!string.IsNullOrEmpty(filter.Category))
            query = query.Where(a => a.Category == filter.Category);
        if (filter.LinkedKnownBugId.HasValue)
            query = query.Where(a => a.LinkedKnownBugId == filter.LinkedKnownBugId.Value);
        if (filter.DiscoveredFrom.HasValue)
            query = query.Where(a => a.DiscoveredAt >= filter.DiscoveredFrom.Value);
        if (filter.DiscoveredUntil.HasValue)
            query = query.Where(a => a.DiscoveredAt < filter.DiscoveredUntil.Value);

        return query.ToList();
    }

    // ====== Commonality analysis ======

    public CommonalityReport AnalyzeCommonalities(IReadOnlyList<MetBench_Domain.Anomaly> anomalies)
    {
        if (anomalies.Count == 0)
            return EmptyReport();

        var bySeverity = anomalies
            .GroupBy(a => a.Severity ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());
        var byCategory = anomalies
            .GroupBy(a => a.Category ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());
        var byStatus = anomalies
            .GroupBy(a => a.Status ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        var dominantSeverity = bySeverity
            .OrderByDescending(kv => kv.Value)
            .FirstOrDefault().Key;
        var dominantCategory = byCategory
            .OrderByDescending(kv => kv.Value)
            .FirstOrDefault().Key;

        var noiseCount = bySeverity.GetValueOrDefault("noise", 0);
        var majorOrCritical = bySeverity.GetValueOrDefault("major", 0)
                            + bySeverity.GetValueOrDefault("critical", 0);
        var linkedToKnownBugCount = anomalies.Count(a => a.LinkedKnownBugId.HasValue);

        var hypothesis = BuildHypothesis(
            anomalies.Count, dominantSeverity, dominantCategory,
            noiseCount, majorOrCritical, linkedToKnownBugCount);

        return new CommonalityReport(
            Total: anomalies.Count,
            BySeverity: bySeverity,
            ByCategory: byCategory,
            ByStatus: byStatus,
            DominantSeverity: dominantSeverity,
            DominantCategory: dominantCategory,
            NoiseFloorCount: noiseCount,
            MajorOrCriticalCount: majorOrCritical,
            LinkedToKnownBugCount: linkedToKnownBugCount,
            Hypothesis: hypothesis);
    }

    // ====== State transitions ======

    public bool TransitionStatus(Guid anomalyId, string newStatus, string? notes, string actor)
    {
        var anomaly = _anomalies.Get(anomalyId);
        if (anomaly is null) return false;

        var oldStatus = anomaly.Status;
        anomaly.Status = newStatus;
        if (!string.IsNullOrEmpty(notes))
            anomaly.Notes = notes;

        var ok = _anomalies.Modify(anomaly);
        if (ok) WriteAudit(actor, "anomaly.status-change",
            "Anomaly", anomaly.IdAnomaly.ToString(),
            $"{{\"from\":\"{oldStatus}\",\"to\":\"{newStatus}\"}}");
        return ok;
    }

    public bool LinkToKnownBug(Guid anomalyId, int knownBugId, string actor)
    {
        var anomaly = _anomalies.Get(anomalyId);
        if (anomaly is null) return false;

        anomaly.LinkedKnownBugId = knownBugId;
        var ok = _anomalies.Modify(anomaly);
        if (ok) WriteAudit(actor, "anomaly.link-known-bug",
            "Anomaly", anomaly.IdAnomaly.ToString(),
            $"{{\"knownBugId\":{knownBugId}}}");
        return ok;
    }

    public bool UnlinkKnownBug(Guid anomalyId, string actor)
    {
        var anomaly = _anomalies.Get(anomalyId);
        if (anomaly is null) return false;

        var oldLink = anomaly.LinkedKnownBugId;
        anomaly.LinkedKnownBugId = null;
        var ok = _anomalies.Modify(anomaly);
        if (ok) WriteAudit(actor, "anomaly.unlink-known-bug",
            "Anomaly", anomaly.IdAnomaly.ToString(),
            $"{{\"oldKnownBugId\":{oldLink}}}");
        return ok;
    }

    // ====== Helpers ======

    private void WriteAudit(string actor, string action, string entityType, string entityId, string detailsJson)
    {
        _auditLog.Add(new AuditLog
        {
            IdLog = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Actor = actor,
            Action = action,
            TargetEntityType = entityType,
            TargetEntityId = entityId,
            DetailsJson = detailsJson,
        });
    }

    private static CommonalityReport EmptyReport() => new(
        Total: 0,
        BySeverity: new Dictionary<string, int>(),
        ByCategory: new Dictionary<string, int>(),
        ByStatus: new Dictionary<string, int>(),
        DominantSeverity: null,
        DominantCategory: null,
        NoiseFloorCount: 0,
        MajorOrCriticalCount: 0,
        LinkedToKnownBugCount: 0,
        Hypothesis: "No anomalies to analyze.");

    private static string BuildHypothesis(
        int total, string? domSev, string? domCat,
        int noise, int major, int linked)
    {
        var parts = new List<string>();
        parts.Add($"{total} anomalies analyzed.");

        if (noise > 0)
            parts.Add($"{noise}/{total} are MC noise-floor — not real bugs.");
        if (major > 0)
            parts.Add($"{major}/{total} are major/critical (real-bug candidates).");
        if (linked > 0)
            parts.Add($"{linked}/{total} reproduce known bugs (LinkedKnownBugId set).");

        if (!string.IsNullOrEmpty(domSev) && total > 0)
            parts.Add($"Dominant severity: {domSev}.");
        if (!string.IsNullOrEmpty(domCat) && total > 0)
            parts.Add($"Dominant category: {domCat}.");

        // 启发式分类
        if (domCat == "basin")
            parts.Add("→ Hypothesis: SUT has non-physical convergence basin at specific parameter slivers.");
        else if (domCat == "mc-floor")
            parts.Add("→ Hypothesis: MR sensitivity floor reached; consider raising particle count.");
        else if (domCat == "cross-program")
            parts.Add("→ Hypothesis: Solver implementation difference — investigate m_cmp.");

        return string.Join(" ", parts);
    }
}
