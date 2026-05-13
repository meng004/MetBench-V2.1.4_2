using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 一次对 CandidateMR 的验证运行。
/// </summary>
/// <remarks>
/// Day-1 三类 validator：
///   • empirical          — 在 N 个 baseline 上跑该候选 MR，看是否一致成立
///   • theoretical-llm    — LLM 反向问"这个 MR 在物理 / 数学上合理吗"
///   • adversarial-mutmut — 注入 mutation 看 MR 是否能检出（防 vacuous）
///
/// 至少 2 个 validator 通过才能 promote。
///
/// 索引：CandidateMRId, (ValidatorName, Passed)。
/// </remarks>
public class ValidationRun
{
    [BsonId] public Guid IdValidation { get; set; }

    /// <summary>→ CandidateMRs.IdCandidate。</summary>
    public Guid CandidateMRId { get; set; }

    /// <summary>验证器名：empirical / theoretical-llm / adversarial-mutmut / ...</summary>
    public string ValidatorName { get; set; } = string.Empty;

    /// <summary>执行时间。</summary>
    public DateTime RunAt { get; set; }

    /// <summary>是否通过。</summary>
    public bool Passed { get; set; }

    /// <summary>详情 JSON（validator 特有的统计数据，如 baseline pass rate / LLM 回答）。</summary>
    public string DetailsJson { get; set; } = "{}";
}
