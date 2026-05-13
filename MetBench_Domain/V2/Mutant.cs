using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// MutationOperator 的一次具体应用 — 对某 SUT 注入特定的代码 patch。
/// </summary>
/// <remarks>
/// 一个 MutationOperator 可应用到多个 SUT（同样的变异规则 → 不同 SUT 的具体 diff）。
///
/// Status：
///   active        — 启用，参与 MutationCampaign
///   deprecated    — 历史保留，不参与新 Campaign
///   experimental  — 实验性，仅用于研究
///
/// 索引：(OperatorId, ApplicationId), Status。
/// </remarks>
public class Mutant
{
    [BsonId] public int IdMutant { get; set; }

    /// <summary>→ MutationOperators.IdOperator。</summary>
    public int OperatorId { get; set; }

    /// <summary>→ Applications.IdApplication；null = 通用 mutant（如改 adapter 而非 SUT）。</summary>
    public int? ApplicationId { get; set; }

    /// <summary>实际应用的 patch / diff 内容（unified diff 格式）。</summary>
    public string AppliedDiff { get; set; } = string.Empty;

    /// <summary>状态：active / deprecated / experimental。</summary>
    public string Status { get; set; } = "active";

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
