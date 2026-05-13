using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// MutationCampaign 中的单格结果 — (mutant × MRBinding × sample case) 跑一次 Execution 的检测结论。
/// </summary>
/// <remarks>
/// Outcome：
///   detected      — MR 检测到了变异（断言失败 / 错误）
///   missed        — MR 未检测到（断言通过，但 mutant 是 semantic — 漏检）
///   error         — SUT / adapter / pipeline 失败（不算检测）
///   not-affected  — mutant 应用文件不在该 scenario 路径上（n/a）
///
/// 一个 Mut00（identity）的 MutationResult Outcome 应该总是 "missed"（正确）；
/// 如果出现 "detected"，是假阳性，需要调查。
///
/// 索引：CampaignId, (MutantId, MRBindingId) 复合唯一, Outcome。
/// </remarks>
public class MutationResult
{
    [BsonId] public Guid IdMutationResult { get; set; }

    /// <summary>→ MutationCampaigns.IdCampaign。</summary>
    public Guid CampaignId { get; set; }

    /// <summary>→ Mutants.IdMutant。</summary>
    public int MutantId { get; set; }

    /// <summary>→ MRBindings.IdMRBinding。</summary>
    public int MRBindingId { get; set; }

    /// <summary>该 cell 对应跑的 → Executions.IdExecution。</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>结果：detected / missed / error / not-affected。</summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>观察到的 Δ 值。</summary>
    public double? ObservedDelta { get; set; }

    /// <summary>对照（无变异时）应有的 Δ 值。</summary>
    public double? ExpectedDelta { get; set; }

    /// <summary>备注。</summary>
    public string? Notes { get; set; }
}
