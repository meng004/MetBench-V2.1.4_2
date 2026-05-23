using LiteDB;
using MetBench_Domain.V2.Enums;

namespace MetBench_Domain.V2;

/// <summary>
/// V3 MR 实体：5D tag schema（Equation / ProgramType / MetaPattern / SourceLevel /
/// FailureCorrelation）+ RigorClass + RelationType + Tolerance。Stage 8 主线
/// 地基（plan §15.2 S8-P5）。与既有 v1/v2 共用的 <see cref="MetamorphicRelation"/>
/// 并存，<see cref="MetamorphicRelationV3"/> 是 5D-aware 索引视图，由 BDD .feature
/// canonical tag（@MR.Equation=... 等）或 V2→V3 migration 投影而来。
/// </summary>
/// <remarks>
/// MR 业务唯一键：<see cref="MrCode"/>（与 v2 MR.Code 对齐）。Id 由 LiteDB 分配。
/// </remarks>
public sealed class MetamorphicRelationV3
{
    /// <summary>LiteDB 行 id（自增）。</summary>
    [BsonId] public Guid IdV3 { get; set; }

    /// <summary>MR 业务键 — 与 v2 <c>MetamorphicRelation.Code</c> 对齐，作 V3 唯一标识。</summary>
    public string MrCode { get; set; } = string.Empty;

    /// <summary>MR 描述（可从 v2 row 复制）。</summary>
    public string Description { get; set; } = string.Empty;

    // ===== 5D tags =====

    /// <summary>第 1 维：所属方程。</summary>
    public EquationKind Equation { get; set; } = EquationKind.Unspecified;

    /// <summary>第 2 维：SUT 求解器类型。</summary>
    public ProgramKind ProgramType { get; set; } = ProgramKind.Unspecified;

    /// <summary>第 3 维：NOETHER 元模式。</summary>
    public MetaPatternKind MetaPattern { get; set; } = MetaPatternKind.Unspecified;

    /// <summary>第 4 维：MR 来源层级。</summary>
    public SourceLevelKind SourceLevel { get; set; } = SourceLevelKind.Unspecified;

    /// <summary>第 5 维：失败相关性。</summary>
    public FailureCorrelationKind FailureCorrelation { get; set; } = FailureCorrelationKind.Unspecified;

    // ===== 辅助分类 =====

    /// <summary>严格程度（A/B/C）。</summary>
    public RigorClassKind RigorClass { get; set; } = RigorClassKind.Unspecified;

    /// <summary>关系类型（不等 / 等式 / 缩放等式 / 跨程序）。</summary>
    public RelationKind RelationType { get; set; } = RelationKind.Unspecified;

    /// <summary>断言绝对容差（仅对 Equality / ScaledEquality 类有效，否则忽略）。</summary>
    public double Tolerance { get; set; }

    /// <summary>同步时间戳（最后 sync from .feature canonical 的 UTC 时间）。</summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
