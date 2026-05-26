using System;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// PR-N1 noise-aware typed scalar predicate — pure-function contract tests for
/// <see cref="NoiseAwareScalarToleranceEvaluator"/>. The formula is
/// <c>NoiseMultiplier · √(sourceStd² + followupStd²)</c>; the input checks reject NaN /
/// ±∞ / negative σ and non-positive multipliers.
/// </summary>
public sealed class NoiseAwareScalarToleranceEvaluatorTests
{
    private static NoiseAwareScalarToleranceEvaluator Eval() => new();

    [Fact]
    public void ComputeTolerance_returns_NoiseMultiplier_times_combined_sigma()
    {
        var tolerance = Eval().ComputeTolerance(sourceStd: 1.0, followupStd: 1.0, noiseMultiplier: 3.0);

        // 3 · √(1 + 1) = 3√2 ≈ 4.2426406871...
        Assert.Equal(3.0 * Math.Sqrt(2.0), tolerance, precision: 12);
    }

    [Fact]
    public void ComputeTolerance_both_sigma_zero_yields_zero()
    {
        Assert.Equal(0.0, Eval().ComputeTolerance(sourceStd: 0.0, followupStd: 0.0, noiseMultiplier: 3.0));
    }

    [Fact]
    public void ComputeTolerance_asymmetric_sigma_uses_root_sum_squares()
    {
        var tolerance = Eval().ComputeTolerance(sourceStd: 2.0, followupStd: 1.0, noiseMultiplier: 1.0);

        // 1 · √(4 + 1) = √5 ≈ 2.2360679...
        Assert.Equal(Math.Sqrt(5.0), tolerance, precision: 12);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ComputeTolerance_rejects_invalid_sourceStd(double bad)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Eval().ComputeTolerance(sourceStd: bad, followupStd: 1.0, noiseMultiplier: 3.0));
        Assert.Equal("sourceStd", ex.ParamName);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ComputeTolerance_rejects_invalid_followupStd(double bad)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Eval().ComputeTolerance(sourceStd: 1.0, followupStd: bad, noiseMultiplier: 3.0));
        Assert.Equal("followupStd", ex.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ComputeTolerance_rejects_invalid_noiseMultiplier(double bad)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Eval().ComputeTolerance(sourceStd: 1.0, followupStd: 1.0, noiseMultiplier: bad));
        Assert.Equal("noiseMultiplier", ex.ParamName);
    }
}
