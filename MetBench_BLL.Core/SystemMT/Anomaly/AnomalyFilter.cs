using MetBench_Domain;

namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// Anomaly 列表过滤条件（WPF UI 下拉 / 文本框对应）。
/// 任意字段为 null = 不过滤；非空字段串联为 AND。
/// </summary>
public sealed record AnomalyFilter(
    string? Severity = null,
    AnomalyStatus? Status = null,
    string? Category = null,
    int? RelatedApplicationId = null,
    int? LinkedKnownBugId = null,
    DateTime? DiscoveredFrom = null,
    DateTime? DiscoveredUntil = null);
