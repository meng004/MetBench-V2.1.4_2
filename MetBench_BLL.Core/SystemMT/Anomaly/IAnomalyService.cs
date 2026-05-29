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

    /// <summary>
    /// 转移状态（<c>new → investigating → {known | confirmed-bug | false-positive | fixed-upstream}</c>）+ 写 AuditLog。
    /// anomaly 不存在返回 <c>false</c>；请求的转移不在状态机里抛
    /// <see cref="InvalidAnomalyStatusTransitionException"/>（合法转移表见 <see cref="MetBench_Domain.AnomalyStatuses.CanTransition"/>）。
    /// </summary>
    bool TransitionStatus(Guid anomalyId, AnomalyStatus newStatus, string? notes, string actor);

    /// <summary>把 Anomaly 链到已知 KnownBug。</summary>
    bool LinkToKnownBug(Guid anomalyId, int knownBugId, string actor);

    /// <summary>取消 KnownBug 关联（重新打开调查）。</summary>
    bool UnlinkKnownBug(Guid anomalyId, string actor);

    /// <summary>
    /// 记录一条失败 run 为 Anomaly（System MT launcher → Anomaly 桥，UC-B7）。
    /// 在 Status="new" 起手，DiscoveredAt=UtcNow，写一条 anomaly.created 审计。
    /// </summary>
    /// <param name="resultId">SystemMtResultRecord.Id，Guid 字符串。</param>
    /// <param name="typedVerificationSummary">
    /// 可选的 typed verification 文本摘要（由 SystemMtLauncher 从 PipelineOutcome.TypedVerification
    /// 投影），格式示例：<c>"typed=Failed predicate=k_eff-greater (BinaryComparison) residual=0.62 tolerance=1E-06"</c>。
    /// 提供时会写入 <see cref="MetBench_Domain.Anomaly.Notes"/> 与 anomaly.created 审计的 detailsJson；
    /// 不提供时 Notes 留空，detailsJson 不增加该字段，与 PR-130 之前一致。
    /// </param>
    Task<MetBench_Domain.Anomaly> RecordAnomalyAsync(
        string mrName,
        string resultId,
        string severity,
        string category,
        string? typedVerificationSummary = null,
        CancellationToken cancellationToken = default);
}
