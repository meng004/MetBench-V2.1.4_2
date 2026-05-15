using MetBench_Domain;

namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// Anomaly 业务服务 — 查询 / 过滤 / 共性分析 / 状态转移 / 链 KnownBug。
/// </summary>
public interface IAnomalyService
{
    /// <summary>列出符合过滤条件的 Anomaly。</summary>
    IReadOnlyList<MetBench_Domain.Anomaly> List(AnomalyFilter filter);

    /// <summary>共性分析 — 输入一组 Anomaly → CommonalityReport。</summary>
    CommonalityReport AnalyzeCommonalities(IReadOnlyList<MetBench_Domain.Anomaly> anomalies);

    /// <summary>转移状态（new → investigating → known/confirmed-bug/false-positive）+ 写 AuditLog。</summary>
    bool TransitionStatus(Guid anomalyId, string newStatus, string? notes, string actor);

    /// <summary>把 Anomaly 链到已知 KnownBug。</summary>
    bool LinkToKnownBug(Guid anomalyId, int knownBugId, string actor);

    /// <summary>取消 KnownBug 关联（重新打开调查）。</summary>
    bool UnlinkKnownBug(Guid anomalyId, string actor);
}
