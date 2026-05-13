using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using FluentAssertions.Numeric;

namespace MetBench_BLL.SystemMT.Assertions;

/// <summary>
/// FluentAssertions extension methods carrying MetBench-specific MT semantics.
/// API 风格与 FluentAssertions 原生 100% 一致：value.Should().BeXxx(...)；
/// 失败时 throw AssertionFailedException（在 .NET 8 上是 Xunit.SdkException 或类似）。
/// </summary>
/// <remarks>
/// 按 docs/design/assertion-extensions.md §5 实施。底层公式：
///   noise_floor = max(NoiseMultiplier · √(σ_src² + σ_flw²),
///                     ToleranceRel · |source|)
/// </remarks>
public static class MetbenchAssertionExtensions
{
    // ===== 噪声感知单调性 =====

    /// <summary>MT 单调下降断言：followup &lt; source − noise_floor。</summary>
    public static AndConstraint<NumericAssertions<double>> BeLessThanWithNoiseFloor(
        this NumericAssertions<double> assertions,
        double source,
        double sourceStd = 0.0,
        double followupStd = 0.0,
        double toleranceRel = 0.0,
        double noiseMultiplier = 3.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sigmaComposite = Math.Sqrt(sourceStd * sourceStd + followupStd * followupStd);
        var noise = Math.Max(noiseMultiplier * sigmaComposite, toleranceRel * Math.Abs(source));
        var threshold = source - noise;
        var actual = (double)assertions.Subject!;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual < threshold)
            .FailWith(
                "Expected followup value to be less than {0:F5} " +
                "(= source {1:F5} − noise {2:F5}) {reason}, but found {3:F5}.",
                threshold, source, noise, actual);

        return new AndConstraint<NumericAssertions<double>>(assertions);
    }

    /// <summary>MT 单调上升断言：followup &gt; source + noise_floor。</summary>
    public static AndConstraint<NumericAssertions<double>> BeGreaterThanWithNoiseFloor(
        this NumericAssertions<double> assertions,
        double source,
        double sourceStd = 0.0,
        double followupStd = 0.0,
        double toleranceRel = 0.0,
        double noiseMultiplier = 3.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sigmaComposite = Math.Sqrt(sourceStd * sourceStd + followupStd * followupStd);
        var noise = Math.Max(noiseMultiplier * sigmaComposite, toleranceRel * Math.Abs(source));
        var threshold = source + noise;
        var actual = (double)assertions.Subject!;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual > threshold)
            .FailWith(
                "Expected followup value to be greater than {0:F5} " +
                "(= source {1:F5} + noise {2:F5}) {reason}, but found {3:F5}.",
                threshold, source, noise, actual);

        return new AndConstraint<NumericAssertions<double>>(assertions);
    }

    // ===== 不变性 =====

    /// <summary>MT 不变性断言：|followup − source| ≤ noise_floor。</summary>
    public static AndConstraint<NumericAssertions<double>> BeApproximatelyEqualUnderTransform(
        this NumericAssertions<double> assertions,
        double source,
        double sourceStd = 0.0,
        double followupStd = 0.0,
        double toleranceRel = 0.001,
        double noiseMultiplier = 3.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sigmaComposite = Math.Sqrt(sourceStd * sourceStd + followupStd * followupStd);
        var bound = Math.Max(noiseMultiplier * sigmaComposite, toleranceRel * Math.Abs(source));
        var actual = (double)assertions.Subject!;
        var delta = actual - source;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Math.Abs(delta) <= bound)
            .FailWith(
                "Expected followup value ≈ {0:F5} within ±{1:F5} {reason}, " +
                "but found {2:F5} (Δ = {3:F5}).",
                source, bound, actual, delta);

        return new AndConstraint<NumericAssertions<double>>(assertions);
    }

    // ===== 收敛性 =====

    /// <summary>MT 收敛率断言：σ_followup / σ_source ≈ 1/√(refinementFactor)。</summary>
    public static AndConstraint<NumericAssertions<double>> HaveVarianceRatio(
        this NumericAssertions<double> followupStdAssertions,
        double sourceStd,
        double refinementFactor,
        double tolerance = 0.1,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(sourceStd > 0)
            .FailWith("HaveVarianceRatio requires source stdev > 0, got {0}.", sourceStd);

        var expectedRatio = 1.0 / Math.Sqrt(refinementFactor);
        var actualRatio = (double)followupStdAssertions.Subject! / sourceStd;
        var delta = Math.Abs(actualRatio - expectedRatio);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(delta <= tolerance)
            .FailWith(
                "Expected σ-ratio ≈ {0:F3} (= 1/√{1}) within ±{2:F3} {reason}, " +
                "but found {3:F3} (Δ = {4:F3}).",
                expectedRatio, refinementFactor, tolerance, actualRatio, delta);

        return new AndConstraint<NumericAssertions<double>>(followupStdAssertions);
    }

    // ===== 数组逐元素近似 =====

    /// <summary>MT 数组逐元素近似（如 per-cell flux 分布）。</summary>
    public static AndConstraint<GenericCollectionAssertions<double>> BePointwiseApproximately(
        this GenericCollectionAssertions<double> assertions,
        IEnumerable<double> source,
        double toleranceRel = 0.01,
        double toleranceAbs = 0.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sourceArr = source.ToArray();
        var actualArr = assertions.Subject.ToArray();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(sourceArr.Length == actualArr.Length)
            .FailWith(
                "Pointwise approx requires equal lengths {reason}, got {0} vs {1}.",
                sourceArr.Length, actualArr.Length);

        for (int i = 0; i < sourceArr.Length; i++)
        {
            var bound = Math.Max(toleranceAbs, toleranceRel * Math.Abs(sourceArr[i]));
            var delta = Math.Abs(actualArr[i] - sourceArr[i]);
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(delta <= bound)
                .FailWith(
                    "Pointwise mismatch at index {0} {reason}: expected ≈ {1:F5} ± {2:F5}, " +
                    "found {3:F5} (Δ = {4:F5}).",
                    i, sourceArr[i], bound, actualArr[i], delta);
        }

        return new AndConstraint<GenericCollectionAssertions<double>>(assertions);
    }

    // ===== 跨程序一致性（m_cmp） =====

    /// <summary>MT 跨程序一致性 (m_cmp)：|actual − reference| ≤ noise_floor。</summary>
    public static AndConstraint<NumericAssertions<double>> AgreeWithReference(
        this NumericAssertions<double> assertions,
        double reference,
        double actualStd = 0.0,
        double referenceStd = 0.0,
        double toleranceRel = 0.01,
        double noiseMultiplier = 3.0,
        string because = "",
        params object[] becauseArgs)
    {
        var sigmaComposite = Math.Sqrt(actualStd * actualStd + referenceStd * referenceStd);
        var bound = Math.Max(noiseMultiplier * sigmaComposite, toleranceRel * Math.Abs(reference));
        var actual = (double)assertions.Subject!;
        var delta = actual - reference;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Math.Abs(delta) <= bound)
            .FailWith(
                "Expected value ≈ reference {0:F5} within ±{1:F5} " +
                "(toleranceRel={2:F4}, 3σ={3:F5}) {reason}, but found {4:F5} (Δ = {5:F5}).",
                reference, bound, toleranceRel, noiseMultiplier * sigmaComposite, actual, delta);

        return new AndConstraint<NumericAssertions<double>>(assertions);
    }
}
