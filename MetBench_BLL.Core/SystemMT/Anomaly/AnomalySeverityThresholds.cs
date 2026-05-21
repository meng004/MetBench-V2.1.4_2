namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// Anomaly severity 分段函数的区间端点（单位：|Δk%| 百分比）。
/// </summary>
/// <remarks>
/// severity 是 |Δk%| 的分段函数，半开区间：
/// <code>
///   [0, NoiseMaxPercent)             → noise
///   [NoiseMaxPercent, MinorMaxPercent) → minor
///   [MinorMaxPercent, MajorMaxPercent) → major
///   [MajorMaxPercent, ∞)             → critical
/// </code>
/// 系统配置参数：WPF 侧可经 <c>appsettings.json</c> 绑定覆盖。
/// <see cref="MetBench_BLL.SystemMT"/> 业务层本身不读配置 —— 只持有此 record；
/// 调用方未注入时回退 <see cref="Default"/>，保证 BLL.Core / 测试 / CI 零配置可用。
/// </remarks>
public sealed record AnomalySeverityThresholds(
    double NoiseMaxPercent = 1.0,
    double MinorMaxPercent = 10.0,
    double MajorMaxPercent = 50.0)
{
    /// <summary>缺省端点 {1, 10, 50}，与 <c>Anomaly.cs</c> 文档化的分级一致。</summary>
    public static readonly AnomalySeverityThresholds Default = new();
}
