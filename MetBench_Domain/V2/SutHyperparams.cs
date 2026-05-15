namespace MetBench_Domain;

/// <summary>
/// 嵌入式 SUT 超参数（不是 MR 输入变换的参数，而是 SUT 自身的运行参数）。
/// 用于 MRBinding（默认）和 MRInstance（override）。
/// </summary>
/// <remarks>
/// 典型用途：
///   • 蒙特卡洛粒子数 (Particles)
///   • 随机种子 (Seed)
///   • 收敛迭代数 (MaxIterations)
///   • 批次数 (Batches)
///   • 演化算法适应度函数 (FitnessFunction)
/// 对确定性 SUT（如 OpenMOC pin-cell 默认配置），全部字段可空。
/// </remarks>
public class SutHyperparams
{
    /// <summary>随机种子（null = 让 SUT 自选）。</summary>
    public long? Seed { get; set; }

    /// <summary>蒙特卡洛粒子 / 样本数。</summary>
    public int? Particles { get; set; }

    /// <summary>批次数。</summary>
    public int? Batches { get; set; }

    /// <summary>最大迭代数。</summary>
    public int? MaxIterations { get; set; }

    /// <summary>适应度评价函数名（演化算法 SUT）。</summary>
    public string? FitnessFunction { get; set; }

    /// <summary>SUT 特有的其他超参（通用扩展槽）。</summary>
    public Dictionary<string, string>? OtherParams { get; set; }
}
