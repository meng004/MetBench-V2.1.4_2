namespace MetBench_Domain;

/// <summary>
/// 嵌入式容差配置。用于 MRBinding（默认）和 MRInstance（override）。
/// </summary>
/// <remarks>
/// 断言 evaluator 计算噪声底：
///   noise_floor = max(NoiseMultiplier * sqrt(σ_src² + σ_flw²),
///                     ToleranceRel * |source|)
/// 当 NoiseAware=true 时启用。
/// </remarks>
public class ToleranceConfig
{
    /// <summary>是否使用噪声感知断言（概率 SUT 必须 true）。</summary>
    public bool NoiseAware { get; set; }

    /// <summary>相对容差，0-1 之间（例 0.001 = 0.1%）。</summary>
    public double ToleranceRel { get; set; }

    /// <summary>绝对容差。</summary>
    public double ToleranceAbs { get; set; }

    /// <summary>σ 乘子，默认 3.0（即 3σ）。</summary>
    public double NoiseMultiplier { get; set; } = 3.0;
}
