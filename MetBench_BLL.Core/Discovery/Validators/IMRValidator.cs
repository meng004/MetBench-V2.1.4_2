using MetBench_Domain;

namespace MetBench_BLL.Discovery.Validators;

/// <summary>
/// CandidateMR 验证器 —— 根据某种证据类型判断候选是否值得 promote。
/// </summary>
/// <remarks>
/// Day-1 实现：
/// <list type="bullet">
///   <item><c>EmpiricalValidator</c>           — baseline 上跑该 MR，看一致成立。</item>
///   <item><c>TheoreticalLlmValidator</c>      — LLM 反向问"物理/数学上合理吗"。</item>
///   <item><c>AdversarialMutmutValidator</c>   — 注入 mutation 看 MR 是否能检出（防 vacuous）。</item>
/// </list>
/// ≥ 2 个 validator 通过才能 promote 进 <see cref="MetamorphicRelation"/> 表。
/// </remarks>
public interface IMRValidator
{
    /// <summary>Validator 唯一名（写入 ValidationRun.ValidatorName）。</summary>
    string Name { get; }

    /// <summary>对一个候选做验证 → 通过 / 失败 + 详情 JSON。</summary>
    Task<ValidationOutcome> ValidateAsync(CandidateMR candidate, CancellationToken ct = default);
}

/// <summary>验证器返回值。</summary>
public sealed record ValidationOutcome(bool Passed, string DetailsJson);
