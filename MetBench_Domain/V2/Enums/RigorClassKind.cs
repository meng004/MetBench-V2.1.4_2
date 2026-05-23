namespace MetBench_Domain.V2.Enums;

/// <summary>
/// MR 严格程度（A/B/C 三档），表征 MR 来源证据与正确性置信。
/// </summary>
public enum RigorClassKind
{
    /// <summary>未指定。</summary>
    Unspecified,
    /// <summary>A 级：来自方程的解析或闭式恒等式，数学上严格成立。</summary>
    A,
    /// <summary>B 级：来自数值方法的性质（如 Richardson 收敛），在合理参数范围成立。</summary>
    B,
    /// <summary>C 级：来自启发式 / LLM 提议，需经验证或反例检验。</summary>
    C,
}
