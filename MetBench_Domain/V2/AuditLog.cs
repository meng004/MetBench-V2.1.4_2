using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 审计日志 — 平台关键操作的完整可追溯记录。
/// </summary>
/// <remarks>
/// 典型 Action 取值：
///   execution.start / execution.complete
///   catalog.update                        (MRSchema / MRBinding / Application CRUD)
///   config.change                         (Runtime / Adapter 配置改动)
///   mr.promote                            (CandidateMR → MRSchema)
///   anomaly.status-change                 (Anomaly Status 转移)
///   schema.migrate                        (LiteDB schema 迁移)
///   campaign.start / campaign.complete    (MutationCampaign)
///
/// 索引：(Timestamp, Action), TargetEntityId。
/// </remarks>
public class AuditLog
{
    [BsonId] public Guid IdLog { get; set; }

    /// <summary>事件时间。</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>触发者（用户名 / "system" / "ci-bot"）。</summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>动作代码（见 remarks）。</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>目标实体类型（如 "MetamorphicRelation" / "Execution"）。</summary>
    public string? TargetEntityType { get; set; }

    /// <summary>目标实体 ID（int 转字符串或 Guid 字符串）。</summary>
    public string? TargetEntityId { get; set; }

    /// <summary>详情 JSON（动作特有的元数据）。</summary>
    public string DetailsJson { get; set; } = "{}";
}
