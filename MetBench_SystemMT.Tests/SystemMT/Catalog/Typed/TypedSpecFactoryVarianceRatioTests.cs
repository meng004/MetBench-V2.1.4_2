using System;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// PR-VR pin: <see cref="TypedSpecFactory.ForVarianceRatio"/> builds a typed
/// <see cref="MrSpec"/> wrapping a <see cref="VarianceRatioPredicate"/> with
/// the correct role-mapping convention (source → low, followup → high) and a
/// SigmaMultiplier of <c>1.0 + toleranceRel</c>. Validates fail-closed on bad
/// inputs.
/// </summary>
public sealed class TypedSpecFactoryVarianceRatioTests
{
    [Fact]
    public void ForVarianceRatio_emits_predicate_with_low_source_high_followup_roles()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr",
            statisticalMetric: "k_eff_std",
            factor: 4.0,
            toleranceRel: 0.30);

        var predicate = Assert.IsType<VarianceRatioPredicate>(spec.Predicates![0]);
        Assert.Equal("source", predicate.LowSampleRole);
        Assert.Equal("followup", predicate.HighSampleRole);
        Assert.Equal("k_eff_std", predicate.StatisticalMetric);
        Assert.Equal("k_eff_std-variance-ratio", predicate.PredicateId);
    }

    [Fact]
    public void ForVarianceRatio_wraps_factor_as_constant_sample_ratio()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr",
            statisticalMetric: "k_eff_std",
            factor: 4.0,
            toleranceRel: 0.30);

        var predicate = (VarianceRatioPredicate)spec.Predicates![0];
        var sampleRatio = Assert.IsType<ConstantParameterExpression>(predicate.SampleRatio);
        Assert.Equal(4.0, sampleRatio.Value);
    }

    [Fact]
    public void ForVarianceRatio_sets_sigma_multiplier_from_one_plus_tolerance_rel()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr",
            statisticalMetric: "k_eff_std",
            factor: 4.0,
            toleranceRel: 0.30);

        var predicate = (VarianceRatioPredicate)spec.Predicates![0];
        // 30% rel tolerance → SigmaMultiplier = 1.30 (threshold = expectedStdError × 1.30).
        Assert.Equal(1.30, predicate.Tolerance.SigmaMultiplier, precision: 12);
    }

    [Fact]
    public void ForVarianceRatio_declares_statistical_projection_for_the_metric()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr",
            statisticalMetric: "k_eff_std",
            factor: 4.0,
            toleranceRel: 0.30);

        Assert.NotNull(spec.Projections);
        Assert.True(spec.Projections!.ContainsKey("k_eff_std"));
        var projection = Assert.IsType<StatisticalProjectionSpec>(spec.Projections["k_eff_std"]);
        Assert.Equal("/values/k_eff_std", projection.StdErrorPath);
    }

    [Fact]
    public void ForVarianceRatio_emits_both_roles_with_baseline_followup_categories()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr",
            statisticalMetric: "k_eff_std",
            factor: 4.0,
            toleranceRel: 0.30);

        Assert.NotNull(spec.Roles);
        Assert.True(spec.Roles!.ContainsKey("source"));
        Assert.True(spec.Roles.ContainsKey("followup"));
        Assert.Equal("Baseline", spec.Roles["source"].Kind);
        Assert.Equal("Followup", spec.Roles["followup"].Kind);
    }

    [Fact]
    public void ForVarianceRatio_resulting_spec_passes_typed_validation()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr",
            statisticalMetric: "k_eff_std",
            factor: 4.0,
            toleranceRel: 0.30);

        var validation = spec.Validate();
        Assert.True(validation.IsValid,
            "ForVarianceRatio output must validate via the typed validator. Errors: "
            + string.Join("; ", validation.Errors));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ForVarianceRatio_rejects_blank_statistical_metric(string? blank)
    {
        Assert.Throws<ArgumentException>(() =>
            TypedSpecFactory.ForVarianceRatio(
                mrCode: "mr-vr",
                statisticalMetric: blank!,
                factor: 4.0,
                toleranceRel: 0.30));
    }

    [Theory]
    [InlineData(1.0)]      // no refinement
    [InlineData(0.5)]      // factor < 1
    [InlineData(0.0)]
    [InlineData(-2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ForVarianceRatio_rejects_factor_outside_strictly_greater_than_one(double factor)
    {
        Assert.Throws<ArgumentException>(() =>
            TypedSpecFactory.ForVarianceRatio(
                mrCode: "mr-vr",
                statisticalMetric: "k_eff_std",
                factor: factor,
                toleranceRel: 0.30));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.10)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ForVarianceRatio_rejects_non_positive_or_non_finite_tolerance_rel(double toleranceRel)
    {
        Assert.Throws<ArgumentException>(() =>
            TypedSpecFactory.ForVarianceRatio(
                mrCode: "mr-vr",
                statisticalMetric: "k_eff_std",
                factor: 4.0,
                toleranceRel: toleranceRel));
    }
}
