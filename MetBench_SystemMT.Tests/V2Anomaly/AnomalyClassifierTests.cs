using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Anomaly;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Anomaly;

/// <summary>
/// AnomalyClassifier — 失败 run → severity / category 分类。
/// severity 是 |Δk%| 的分段函数（半开区间）；category 区分 runner 崩溃 vs MR 反例。
/// </summary>
public sealed class AnomalyClassifierTests
{
    // =================== severity — 区间内取值 ===================

    [Theory]
    [InlineData(100.0, 100.5, "noise")]     // 0.5%
    [InlineData(100.0, 99.6, "noise")]      // 0.4%（下降方向，取绝对值）
    [InlineData(100.0, 105.0, "minor")]     // 5%
    [InlineData(100.0, 92.0, "minor")]      // 8%（下降方向）
    [InlineData(100.0, 130.0, "major")]     // 30%
    [InlineData(100.0, 70.0, "major")]      // 30%（下降方向）
    [InlineData(100.0, 180.0, "critical")]  // 80%
    [InlineData(100.0, 40.0, "critical")]   // 60%（下降方向）
    public void ClassifySeverity_buckets_by_delta_percent(
        double sourceValue, double followUpValue, string expected)
    {
        var result = FailingResult(sourceValue, followUpValue);

        var severity = AnomalyClassifier.ClassifySeverity(result, AnomalySeverityThresholds.Default);

        Assert.Equal(expected, severity);
    }

    // =================== severity — 半开区间 [lo,hi) 边界 ===================

    [Theory]
    [InlineData(100.0, 100.99, "noise")]    // 0.99% < 1   → noise
    [InlineData(100.0, 101.01, "minor")]    // 1.01% ≥ 1   → minor
    [InlineData(100.0, 109.9, "minor")]     // 9.9%  < 10  → minor
    [InlineData(100.0, 110.1, "major")]     // 10.1% ≥ 10  → major
    [InlineData(100.0, 149.0, "major")]     // 49%   < 50  → major
    [InlineData(100.0, 151.0, "critical")]  // 51%   ≥ 50  → critical
    public void ClassifySeverity_uses_half_open_intervals(
        double sourceValue, double followUpValue, string expected)
    {
        var result = FailingResult(sourceValue, followUpValue);

        var severity = AnomalyClassifier.ClassifySeverity(result, AnomalySeverityThresholds.Default);

        Assert.Equal(expected, severity);
    }

    // =================== severity — 守卫 ===================

    [Fact]
    public void ClassifySeverity_returns_critical_when_source_value_is_zero()
    {
        // SourceValue≈0 无法算相对变化 → critical
        var result = FailingResult(sourceValue: 0.0, followUpValue: 1.0);

        Assert.Equal("critical", AnomalyClassifier.ClassifySeverity(result, AnomalySeverityThresholds.Default));
    }

    [Theory]
    [InlineData(false, true)]   // source 崩溃
    [InlineData(true, false)]   // follow-up 崩溃
    [InlineData(false, false)]  // 双双崩溃
    public void ClassifySeverity_returns_critical_on_runner_crash_regardless_of_delta(
        bool sourceSucceeded, bool followUpSucceeded)
    {
        // 即便 Δk% 极小，runner 崩溃也直接判 critical
        var result = FailingResult(100.0, 100.001, sourceSucceeded, followUpSucceeded);

        Assert.Equal("critical", AnomalyClassifier.ClassifySeverity(result, AnomalySeverityThresholds.Default));
    }

    // =================== severity — 自定义阈值（系统配置参数）===================

    [Fact]
    public void ClassifySeverity_honors_non_default_thresholds()
    {
        var custom = new AnomalySeverityThresholds(
            NoiseMaxPercent: 2.0, MinorMaxPercent: 5.0, MajorMaxPercent: 20.0);

        // 1.5%：默认 {1,10,50} 下是 minor，自定义 {2,5,20} 下是 noise
        var r15 = FailingResult(100.0, 101.5);
        Assert.Equal("minor", AnomalyClassifier.ClassifySeverity(r15, AnomalySeverityThresholds.Default));
        Assert.Equal("noise", AnomalyClassifier.ClassifySeverity(r15, custom));

        // 8%：默认下是 minor，自定义下是 major
        var r8 = FailingResult(100.0, 108.0);
        Assert.Equal("minor", AnomalyClassifier.ClassifySeverity(r8, AnomalySeverityThresholds.Default));
        Assert.Equal("major", AnomalyClassifier.ClassifySeverity(r8, custom));
    }

    // =================== category ===================

    [Fact]
    public void ClassifyCategory_returns_single_point_when_both_runs_succeeded()
    {
        // 进程正常退出、是 MR 断言被违反
        var result = FailingResult(100.0, 105.0);

        Assert.Equal("single-point", AnomalyClassifier.ClassifyCategory(result));
    }

    [Theory]
    [InlineData(false, true)]   // source 崩溃
    [InlineData(true, false)]   // follow-up 崩溃
    [InlineData(false, false)]  // 双双崩溃
    public void ClassifyCategory_returns_runner_failure_on_process_crash(
        bool sourceSucceeded, bool followUpSucceeded)
    {
        var result = FailingResult(100.0, 105.0, sourceSucceeded, followUpSucceeded);

        Assert.Equal("runner-failure", AnomalyClassifier.ClassifyCategory(result));
    }

    // =================== 参数守卫 ===================

    [Fact]
    public void ClassifySeverity_rejects_null_arguments()
    {
        var result = FailingResult(100.0, 105.0);
        Assert.Throws<ArgumentNullException>(
            () => AnomalyClassifier.ClassifySeverity(null!, AnomalySeverityThresholds.Default));
        Assert.Throws<ArgumentNullException>(
            () => AnomalyClassifier.ClassifySeverity(result, null!));
    }

    [Fact]
    public void ClassifyCategory_rejects_null_result()
        => Assert.Throws<ArgumentNullException>(() => AnomalyClassifier.ClassifyCategory(null!));

    // =================== fixture ===================

    private static SystemMtResult FailingResult(
        double sourceValue,
        double followUpValue,
        bool sourceSucceeded = true,
        bool followUpSucceeded = true)
    {
        var sourceRun = new CliRunResult(
            "source", sourceSucceeded ? 0 : 1, "stdout", sourceSucceeded ? string.Empty : "boom",
            TimeSpan.FromSeconds(1), "/tmp/source/output.json",
            sourceSucceeded, sourceSucceeded ? string.Empty : "runner crashed");
        var followUpRun = new CliRunResult(
            "follow-up", followUpSucceeded ? 0 : 1, "stdout", followUpSucceeded ? string.Empty : "boom",
            TimeSpan.FromSeconds(1), "/tmp/followup/output.json",
            followUpSucceeded, followUpSucceeded ? string.Empty : "runner crashed");
        var empty = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var assertion = new SystemMtAssertionResult(
            "GreaterThan", "k_eff", sourceValue, followUpValue, Passed: false, "MR violated");
        return new SystemMtResult(
            sourceRun, followUpRun, empty, empty, assertion, Passed: false, "MR violated");
    }
}
