using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 变异活动 — 一次 mutants × MRBindings × sample cases 的批量验证。
/// </summary>
/// <remarks>
/// 用途：
///   • 评估 MR 套件的检出率（detection rate）
///   • 评估 Mut00（identity）的假阳性率（必须为 0）
///   • 跨 SUT 同 MR 的差分分析（Cohen's κ）
///
/// ScopeSpec JSON 描述本 Campaign 跑哪些 (mutant, mrBinding, sampleCase) 组合。
///
/// 索引：(Status, StartedAt)。
/// </remarks>
public class MutationCampaign
{
    [BsonId] public Guid IdCampaign { get; set; }

    /// <summary>Campaign 名（如 "Phase-2-2026-05-12-OpenMOC-vs-OpenMC"）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Scope 规格 JSON：{mutants: [...], mrBindings: [...], sampleCases: [...]}。</summary>
    public string ScopeSpecJson { get; set; } = "{}";

    /// <summary>启动时 catalog 的 git SHA（审计可追溯）。</summary>
    public string CatalogVersionSha { get; set; } = string.Empty;

    /// <summary>开始时间。</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>完成时间。</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>状态：running / ok / cancelled / failed。</summary>
    public string Status { get; set; } = "running";

    /// <summary>创建者。</summary>
    public string? CreatedBy { get; set; }
}
