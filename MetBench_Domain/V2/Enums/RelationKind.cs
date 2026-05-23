namespace MetBench_Domain.V2.Enums;

/// <summary>
/// MR 关系类型 — 描述源/衍生输出之间的数学关系形态。
/// </summary>
public enum RelationKind
{
    /// <summary>未指定。</summary>
    Unspecified,
    /// <summary>不等式（GreaterThan / LessThan，单调性）。</summary>
    Ordinal,
    /// <summary>等式（ApproxEqual，守恒 / 收敛）。</summary>
    Equality,
    /// <summary>缩放等式（output_followup ≈ k · output_source，线性系数已知）。</summary>
    ScaledEquality,
    /// <summary>分布等价（KS / χ² 等统计检验）。</summary>
    DistributionEquivalence,
    /// <summary>跨程序一致性（OpenMOC vs OpenMC 类）。</summary>
    CrossProgramAgreement,
}
