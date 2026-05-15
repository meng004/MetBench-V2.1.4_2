namespace MetBench_BLL.SystemMT.Assertions;

/// <summary>
/// 断言计算的输入数据 — 由 Pipeline 从 Result 提取后调 AssertionEvaluator。
/// </summary>
/// <param name="SourceValue">主断言值的 source 值（MR.ValueName 字段）。</param>
/// <param name="SourceStd">source 标准差（确定性 SUT 为 0）。</param>
/// <param name="FollowupValue">主断言值的 followup 值。</param>
/// <param name="FollowupStd">followup 标准差。</param>
/// <param name="ValueName">主断言字段名（如 "k_eff"）。</param>
/// <param name="ExtraValues">
/// 额外数据：
/// <list type="bullet">
///   <item><c>refinement_factor</c> 用于 variance-ratio 断言</item>
///   <item><c>reference_value</c> / <c>reference_std</c> 用于 cross-program-agree</item>
///   <item><c>source_flux_0..N</c> / <c>followup_flux_0..N</c> 用于 flux-pointwise-approx</item>
/// </list>
/// </param>
public sealed record AssertionInput(
    double SourceValue,
    double SourceStd,
    double FollowupValue,
    double FollowupStd,
    string ValueName,
    IReadOnlyDictionary<string, double>? ExtraValues = null);

/// <summary>
/// 容差配置（BLL 层 record；与 MetBench_Domain.ToleranceConfig LiteDB 实体对应）。
/// </summary>
public sealed record AssertionTolerance(
    bool NoiseAware = false,
    double ToleranceRel = 0.0,
    double ToleranceAbs = 0.0,
    double NoiseMultiplier = 3.0);
