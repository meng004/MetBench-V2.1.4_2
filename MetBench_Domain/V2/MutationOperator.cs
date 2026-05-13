using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 变异算子 — 描述如何对 SUT runner / adapter / 源码注入一个具体变异。
/// </summary>
/// <remarks>
/// Category 分类：
///   runner-level          — 改 SUT runner.py 的输出（如 hardcoded k_eff）
///   input-adapter-level   — 改输入 adapter 的字段操作（如 scale 错字段）
///   code-level            — 改 SUT 源码（C++ / Fortran，用 mutmut / AST patch）
///   case-level            — 改输入案例文件（pathological）
///
/// PredictedClass：
///   semantic     — 应被某个 MR 检出
///   equivalent   — 等价变异（不改变 SUT 行为）；Mut00 是此类用作假阳性控制
///   pathological — 触发 SUT 异常 / 超时
///
/// RelatedMetaPattern：预测哪个 MetaPattern 类的 MR 应该敏感（如 m_mono / m_inv）。
///
/// 索引：Code 唯一, Category。
/// </remarks>
public class MutationOperator
{
    [BsonId] public int IdOperator { get; set; }

    /// <summary>变异算子简码（如 "Mut02-runner-sigt-from-siga"）。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>类别：runner-level / input-adapter-level / code-level / case-level。</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>变异目标文件类型描述（如 "<sut>_runner.py" / "<sut>.cpp"）。</summary>
    public string TargetFileType { get; set; } = string.Empty;

    /// <summary>变异规则描述（patch / regex / AST rule）。</summary>
    public string ApplicationSpec { get; set; } = string.Empty;

    /// <summary>预测分类：semantic / equivalent / pathological。</summary>
    public string PredictedClass { get; set; } = "semantic";

    /// <summary>预测应被哪个 MetaPattern 检出（可空）。</summary>
    public string? RelatedMetaPattern { get; set; }

    /// <summary>详细描述。</summary>
    public string Description { get; set; } = string.Empty;
}
