namespace MetBench_Domain;

/// <summary>
/// 嵌入式采样规格（参数扫描 / 蒙特卡洛抽样的策略）。
/// 用于 MRInstance（仅在批量执行 / sweep 时设置）。
/// </summary>
/// <remarks>
/// 典型用途：
///   • 固定列表："fixed-list" + {values: [1.1, 1.25, 1.5, 1.75, 2.0]}
///   • 均匀分布："uniform" + {min: 0.5, max: 2.0}
///   • 对数均匀："log-uniform" + {min: 0.1, max: 10.0}
///   • 正态："normal" + {mean: 1.0, std: 0.1}
///   • 对数正态："log-normal" + {mu: 0, sigma: 0.5}
/// </remarks>
public class SamplingSpec
{
    /// <summary>分布类型。</summary>
    public string Distribution { get; set; } = "fixed-list";

    /// <summary>样本点数（应 ≥ 5 以验证趋势）。</summary>
    public int SampleCount { get; set; }

    /// <summary>分布参数（与 Distribution 配对）。</summary>
    public Dictionary<string, object> DistributionParams { get; set; } = new();
}
