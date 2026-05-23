using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 渲染过的报告记录（不存内容，存路径 + 元数据）。
/// </summary>
/// <remarks>
/// Scope 取值（注：weekly / monthly 已于 next-stage P0 整体下线 — Trend 子系统删除）：
///   single-execution  — 单次 Execution 的详细报告
///   single-campaign   — 单次 MutationCampaign 的报告
///   anomaly           — Anomaly 报告
///   coverage          — Coverage 报告
///   ad-hoc            — 手动生成
///   paper-package     — 论文复现包（含 catalog snapshot + artifacts + figures）
///
/// 索引：(Scope, GeneratedAt)。
/// 历史 BSON 文档可能仍含 Scope="weekly" 字符串；schema 兼容，不会反序列化错误。
/// </remarks>
public class Report
{
    [BsonId] public Guid IdReport { get; set; }

    /// <summary>生成时间。</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>报告范围：见 remarks。</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Scope 引用 ID（ExecutionId / CampaignId / 时间范围）。</summary>
    public string? ScopeRefId { get; set; }

    /// <summary>格式：html / pdf。</summary>
    public string Format { get; set; } = "html";

    /// <summary>渲染产物文件路径。</summary>
    public string ContentPath { get; set; } = string.Empty;

    /// <summary>备注。</summary>
    public string? Notes { get; set; }
}
