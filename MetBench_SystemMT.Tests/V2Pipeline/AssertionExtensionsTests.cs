using FluentAssertions;
using MetBench_BLL.SystemMT.Assertions;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pipeline;

/// <summary>
/// TDD 验证 P4.2 — 6 个 FluentAssertions 扩展方法 pass + fail 路径。
/// </summary>
public sealed class AssertionExtensionsTests
{
    // ====== BeLessThanWithNoiseFloor ======

    [Fact]
    public void BeLessThanWithNoiseFloor_passes_when_delta_exceeds_noise()
    {
        Action act = () => 0.5.Should().BeLessThanWithNoiseFloor(
            source: 1.0, sourceStd: 0.001, followupStd: 0.001,
            toleranceRel: 0.0, noiseMultiplier: 3.0);
        act.Should().NotThrow();
    }

    [Fact]
    public void BeLessThanWithNoiseFloor_fails_when_within_noise()
    {
        Action act = () => 0.999.Should().BeLessThanWithNoiseFloor(
            source: 1.0, sourceStd: 0.01, followupStd: 0.01,
            toleranceRel: 0.0, noiseMultiplier: 3.0);
        act.Should().Throw<Exception>().WithMessage("*less than*");
    }

    [Fact]
    public void BeLessThanWithNoiseFloor_uses_relative_tolerance_when_larger_than_3sigma()
    {
        // source=1.0, sigma=0; toleranceRel=0.1 → noise floor = 0.1
        // followup=0.85 < 1.0 - 0.1 = 0.9 → pass
        Action act = () => 0.85.Should().BeLessThanWithNoiseFloor(
            source: 1.0, sourceStd: 0.0, followupStd: 0.0,
            toleranceRel: 0.1);
        act.Should().NotThrow();
        // followup=0.95 < 1.0 - 0.1 = 0.9 → fail
        Action fail = () => 0.95.Should().BeLessThanWithNoiseFloor(
            source: 1.0, sourceStd: 0.0, followupStd: 0.0,
            toleranceRel: 0.1);
        fail.Should().Throw<Exception>();
    }

    // ====== BeGreaterThanWithNoiseFloor ======

    [Fact]
    public void BeGreaterThanWithNoiseFloor_passes_when_delta_exceeds_noise()
    {
        Action act = () => 1.5.Should().BeGreaterThanWithNoiseFloor(
            source: 1.0, sourceStd: 0.001, followupStd: 0.001);
        act.Should().NotThrow();
    }

    [Fact]
    public void BeGreaterThanWithNoiseFloor_fails_when_within_noise()
    {
        Action act = () => 1.001.Should().BeGreaterThanWithNoiseFloor(
            source: 1.0, sourceStd: 0.01, followupStd: 0.01);
        act.Should().Throw<Exception>();
    }

    // ====== BeApproximatelyEqualUnderTransform ======

    [Fact]
    public void BeApproximatelyEqualUnderTransform_passes_when_within_bound()
    {
        Action act = () => 1.0005.Should().BeApproximatelyEqualUnderTransform(
            source: 1.0, sourceStd: 0.0, followupStd: 0.0,
            toleranceRel: 0.001);
        act.Should().NotThrow();
    }

    [Fact]
    public void BeApproximatelyEqualUnderTransform_fails_when_out_of_bound()
    {
        Action act = () => 1.05.Should().BeApproximatelyEqualUnderTransform(
            source: 1.0, sourceStd: 0.0, followupStd: 0.0,
            toleranceRel: 0.001);
        act.Should().Throw<Exception>();
    }

    // ====== HaveVarianceRatio ======

    [Fact]
    public void HaveVarianceRatio_passes_at_1_over_sqrt_N()
    {
        // source σ = 0.002, refinement=4 → expected ratio = 0.5;
        // followup σ = 0.001 → actual ratio = 0.5 → 完美匹配
        Action act = () => 0.001.Should().HaveVarianceRatio(
            sourceStd: 0.002, refinementFactor: 4.0, tolerance: 0.05);
        act.Should().NotThrow();
    }

    [Fact]
    public void HaveVarianceRatio_fails_when_out_of_tolerance()
    {
        // source σ = 0.001, refinement=4 → expected ratio = 0.5;
        // followup σ = 0.0008 → actual = 0.8, off by 0.3, > tolerance 0.05
        Action act = () => 0.0008.Should().HaveVarianceRatio(
            sourceStd: 0.001, refinementFactor: 4.0, tolerance: 0.05);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void HaveVarianceRatio_rejects_zero_source_stdev()
    {
        Action act = () => 0.001.Should().HaveVarianceRatio(
            sourceStd: 0.0, refinementFactor: 4.0);
        act.Should().Throw<Exception>().WithMessage("*source stdev*");
    }

    // ====== BePointwiseApproximately ======

    [Fact]
    public void BePointwiseApproximately_passes_for_equal_arrays()
    {
        var source = new double[] { 1.0, 2.0, 3.0 };
        var actual = new double[] { 1.0, 2.0, 3.0 };
        Action act = () => actual.Should().BePointwiseApproximately(source, toleranceRel: 0.0);
        act.Should().NotThrow();
    }

    [Fact]
    public void BePointwiseApproximately_fails_at_first_mismatch()
    {
        var source = new double[] { 1.0, 2.0, 3.0 };
        var actual = new double[] { 1.0, 2.0, 3.5 };  // 17% mismatch at index 2
        Action act = () => actual.Should().BePointwiseApproximately(source, toleranceRel: 0.01);
        act.Should().Throw<Exception>().WithMessage("*index 2*");
    }

    [Fact]
    public void BePointwiseApproximately_fails_on_length_mismatch()
    {
        var source = new double[] { 1.0, 2.0 };
        var actual = new double[] { 1.0, 2.0, 3.0 };
        Action act = () => actual.Should().BePointwiseApproximately(source);
        act.Should().Throw<Exception>().WithMessage("*equal lengths*");
    }

    // ====== AgreeWithReference ======

    [Fact]
    public void AgreeWithReference_passes_within_tolerance()
    {
        Action act = () => 1.13.Should().AgreeWithReference(
            reference: 1.12, actualStd: 0.001, referenceStd: 0.001,
            toleranceRel: 0.05);
        act.Should().NotThrow();
    }

    [Fact]
    public void AgreeWithReference_fails_for_large_disagreement()
    {
        // 51% disagreement (Case 4)
        Action act = () => 0.4764.Should().AgreeWithReference(
            reference: 0.9683, actualStd: 0.0, referenceStd: 0.001,
            toleranceRel: 0.01);
        act.Should().Throw<Exception>();
    }
}
