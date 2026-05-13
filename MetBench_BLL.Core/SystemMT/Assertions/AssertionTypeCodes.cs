namespace MetBench_BLL.SystemMT.Assertions;

/// <summary>
/// 断言类型代码集合（与 MetamorphicRelation.AssertionTypeCode / .feature tag /
/// AssertionEvaluator switch 分派 三处共享同一字符串）。
/// </summary>
public static class AssertionTypeCodes
{
    /// <summary>普通小于（FluentAssertions 原生 BeLessThan）。</summary>
    public const string Less = "less";

    /// <summary>普通大于（FluentAssertions 原生 BeGreaterThan）。</summary>
    public const string Greater = "greater";

    /// <summary>普通近似（FluentAssertions 原生 BeApproximately）。</summary>
    public const string Approx = "approx";

    /// <summary>噪声感知小于（用于概率 SUT 的 m_mono MR）。</summary>
    public const string LessNoiseAware = "less-noise-aware";

    /// <summary>噪声感知大于。</summary>
    public const string GreaterNoiseAware = "greater-noise-aware";

    /// <summary>MT 不变性 (|followup - source| ≤ noise_floor)。</summary>
    public const string ApproxInvariant = "approx-invariant";

    /// <summary>MT 收敛性 (σ_followup / σ_source ≈ 1/√N，用于 MR12 RefineParticles)。</summary>
    public const string VarianceRatio = "variance-ratio";

    /// <summary>逐元素近似（数组对比，如 flux 分布）。</summary>
    public const string FluxPointwiseApprox = "flux-pointwise-approx";

    /// <summary>跨程序一致性（m_cmp，如 OpenMOC vs OpenMC）。</summary>
    public const string CrossProgramAgree = "cross-program-agree";

    /// <summary>支持的全部断言代码（UI 下拉框 / 校验用）。</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        Less, Greater, Approx,
        LessNoiseAware, GreaterNoiseAware, ApproxInvariant,
        VarianceRatio, FluxPointwiseApprox, CrossProgramAgree,
    };
}
