using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// MR 候选 — Discovery 子系统产出的待审 MR 提议。
/// 必须通过 ≥2 个 Validator 才能 promote 进 MetamorphicRelations collection。
/// </summary>
/// <remarks>
/// 生命周期：
///   pending → (validated | rejected | duplicate)
///   validated → promoted (写入 MetamorphicRelations 表后)
///
/// 索引：DiscoveryRunId, Status, PromotedToMRId。
/// </remarks>
public class CandidateMR
{
    [BsonId] public Guid IdCandidate { get; set; }

    /// <summary>→ DiscoveryRuns.IdRun。</summary>
    public Guid DiscoveryRunId { get; set; }

    /// <summary>方法提议的 Code（如 "MR-T-auto-001"）。</summary>
    public string ProposedCode { get; set; } = string.Empty;

    /// <summary>方法提议的人类可读名。</summary>
    public string ProposedName { get; set; } = string.Empty;

    /// <summary>建议的 IMRTransformation 实现名（如 "ScaleField"）。</summary>
    public string? SuggestedTransformationName { get; set; }

    /// <summary>建议的断言类型代码（如 "less-noise-aware"）。</summary>
    public string? SuggestedAssertionTypeCode { get; set; }

    /// <summary>建议检查的值名（如 "k_eff"）。</summary>
    public string ProposedValueName { get; set; } = string.Empty;

    /// <summary>方法给的合理性说明（一段 Markdown）。</summary>
    public string Rationale { get; set; } = string.Empty;

    /// <summary>方法给的 confidence 分数（0-1）。</summary>
    public double Confidence { get; set; }

    /// <summary>状态：pending / validated / promoted / rejected / duplicate。</summary>
    public string Status { get; set; } = "pending";

    /// <summary>若 promoted，则指向 MetamorphicRelations.IdMR。</summary>
    public int? PromotedToMRId { get; set; }

    /// <summary>若 rejected，原因。</summary>
    public string? RejectionReason { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
