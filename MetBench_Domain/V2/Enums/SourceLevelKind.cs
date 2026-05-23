namespace MetBench_Domain.V2.Enums;

/// <summary>
/// MR 来源层级，V3 MR 5D tag 第 4 维（How was the MR identified?）。
/// 与既有 <c>MetamorphicRelation.DiscoveryMethod</c> 字符串映射。
/// </summary>
public enum SourceLevelKind
{
    /// <summary>未指定（v2 默认值）。</summary>
    Unspecified,
    /// <summary>专家手工录入。</summary>
    Manual,
    /// <summary>从文献摘取（Cmrlibrary / PWR_MR_Analysis 等）。</summary>
    Literature,
    /// <summary>meta-prompt LLM 自动识别。</summary>
    MetaPrompt,
    /// <summary>多 LLM 共识识别。</summary>
    MultiLlmConsensus,
    /// <summary>语义因果图（SCG）启发式挖掘。</summary>
    ScgHeuristic,
}
