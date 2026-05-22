namespace MetBench_BLL.SystemMT;

/// <summary>
/// <see cref="ApproxEqualAssertion"/> 的容差。等式判定采用 numpy <c>isclose</c> 风格，
/// 绝对与相对合一：
/// <code>
///   |flw - src| &lt;= AbsoluteTolerance + RelativeTolerance * |src|
/// </code>
/// 小量由绝对容差兜底，大量由相对容差缩放。
/// </summary>
/// <remarks>
/// 系统配置参数：WPF 侧可经 <c>appsettings.json</c> 绑定覆盖、改值不重编译。
/// BLL.Core 不读配置 —— 只持有此 record；调用方未注入时回退 <see cref="Default"/>，
/// 保证 BLL.Core / 测试 / CI 零配置可用。沿用 <c>AnomalySeverityThresholds</c> 同一模式。
/// </remarks>
public sealed record EqualityThresholds(
    double AbsoluteTolerance = 1e-5,
    double RelativeTolerance = 0.05)
{
    /// <summary>缺省容差 atol=1e-5 / rtol=0.05。</summary>
    public static readonly EqualityThresholds Default = new();
}
