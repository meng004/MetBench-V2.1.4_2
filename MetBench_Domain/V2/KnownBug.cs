using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 已知 bug 库 — R-Case 集合。
/// Anomaly 可以通过 LinkedKnownBugId 链接到这里，标记为"已知 bug 复现"。
/// </summary>
/// <remarks>
/// Status 状态机：
///   open            — 仍存在于上游 SUT
///   fixed-upstream  — 上游已修复（fix commit 已合并）
///   fixed-metbench  — MetBench 侧绕开了（如 SUT 升级）
///   wontfix         — 不打算修复（如 narrow basin 是预防性 fix）
///
/// 索引：Code 唯一, Status, RelatedApplicationId。
/// </remarks>
public class KnownBug
{
    [BsonId] public int IdBug { get; set; }

    /// <summary>Code（如 "R-Case-6"）。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>简短标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>关联 SUT → Applications.IdApplication（可空，跨 SUT bug 则 null）。</summary>
    public int? RelatedApplicationId { get; set; }

    /// <summary>上游 fix commit SHA（若存在）。</summary>
    public string? UpstreamFixCommit { get; set; }

    /// <summary>状态：open / fixed-upstream / fixed-metbench / wontfix。</summary>
    public string Status { get; set; } = "open";

    /// <summary>详细描述（Markdown）。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>记录时间。</summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
