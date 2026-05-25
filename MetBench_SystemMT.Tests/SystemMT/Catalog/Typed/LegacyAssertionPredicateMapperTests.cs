using System;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// Migration-input contract for the legacy-string-code to typed-predicate mapper.
/// Scope (verification-semantics convergence PR-C + PR-124 dormant-code extension):
///   less              -> BinaryComparisonPredicate(Operator=Less)
///   greater           -> BinaryComparisonPredicate(Operator=Greater)
///   approx            -> BinaryComparisonPredicate(Operator=Equal)
///   approx-invariant  -> BinaryComparisonPredicate(Operator=Equal) (scalar form; deterministic tolerance)
///   scaling flw = k * src -> ScaledEqualityPredicate
///   variance-ratio        -> VarianceRatioPredicate
///   flux-pointwise-approx -> FieldEqualityPredicate (Identity pairing)
///   cross-program-agree   -> CrossMethodComparisonPredicate(Operator=Equal)
///
/// Intentional fail-closed (typed model cannot yet represent std/noise scalar semantics):
///   less-noise-aware, greater-noise-aware
///
/// Any other legacy code is rejected fail-closed; the typed runtime
/// has no production fallback to AssertionEvaluator after PR-D.
/// </summary>
public sealed class LegacyAssertionPredicateMapperTests
{
    [Theory]
    [InlineData("less", "Less")]
    [InlineData("greater", "Greater")]
    [InlineData("approx", "Equal")]
    [InlineData("approx-invariant", "Equal")]
    public void Scalar_assertion_codes_map_to_binary_comparison(string code, string expectedOperator)
    {
        var predicate = LegacyAssertionPredicateMapper.MapScalar(
            assertionTypeCode: code,
            actualRole: "followup",
            expectedRole: "source",
            metric: "k_eff");

        var binary = Assert.IsType<BinaryComparisonPredicate>(predicate);
        Assert.Equal(expectedOperator, binary.Operator);
        Assert.Equal("followup", binary.LeftRole);
        Assert.Equal("source", binary.RightRole);
        Assert.Equal("k_eff", binary.Metric);
        Assert.False(string.IsNullOrEmpty(binary.PredicateId));
    }

    [Fact]
    public void Scaling_relation_maps_to_scaled_equality_with_constant_factor()
    {
        var predicate = LegacyAssertionPredicateMapper.MapScaling(
            actualRole: "followup",
            expectedRole: "source",
            metric: "delta_T",
            factor: new ConstantParameterExpression(2.0),
            exponent: 1.0);

        var scaled = Assert.IsType<ScaledEqualityPredicate>(predicate);
        Assert.Equal("followup", scaled.ActualRole);
        Assert.Equal("source", scaled.ReferenceRole);
        Assert.Equal("delta_T", scaled.Metric);
        Assert.Equal(1.0, scaled.Exponent);
        var constantFactor = Assert.IsType<ConstantParameterExpression>(scaled.Factor);
        Assert.Equal(2.0, constantFactor.Value);
    }

    [Fact]
    public void Scaling_relation_maps_to_scaled_equality_with_parameter_factor()
    {
        var predicate = LegacyAssertionPredicateMapper.MapScaling(
            actualRole: "followup",
            expectedRole: "source",
            metric: "phi_max",
            factor: new MrParameterRefExpression("factor"),
            exponent: 1.0);

        var scaled = Assert.IsType<ScaledEqualityPredicate>(predicate);
        var factorRef = Assert.IsType<MrParameterRefExpression>(scaled.Factor);
        Assert.Equal("factor", factorRef.Name);
    }

    [Theory]
    [InlineData("less-noise-aware")]
    [InlineData("greater-noise-aware")]
    public void Noise_aware_scalar_codes_fail_closed_with_documented_reason(string code)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapScalar(code, "followup", "source", "k_eff"));

        Assert.Contains("noise-aware", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not yet representable", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("string-switch-new-code")]
    [InlineData("")]
    public void Unknown_legacy_code_is_rejected_fail_closed(string code)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapScalar(code, "followup", "source", "k_eff"));

        Assert.Contains("Unsupported legacy assertion code", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Variance_ratio_maps_to_variance_ratio_predicate()
    {
        var predicate = LegacyAssertionPredicateMapper.MapVarianceRatio(
            lowSampleRole: "coarse",
            highSampleRole: "refined",
            statisticalMetric: "k_eff",
            sampleRatio: new ConstantParameterExpression(4.0),
            sigmaMultiplier: 3.0);

        var variance = Assert.IsType<VarianceRatioPredicate>(predicate);
        Assert.Equal("coarse", variance.LowSampleRole);
        Assert.Equal("refined", variance.HighSampleRole);
        Assert.Equal("k_eff", variance.StatisticalMetric);
        var ratio = Assert.IsType<ConstantParameterExpression>(variance.SampleRatio);
        Assert.Equal(4.0, ratio.Value);
        Assert.Equal(3.0, variance.Tolerance.SigmaMultiplier);
        Assert.False(string.IsNullOrEmpty(variance.PredicateId));
    }

    [Fact]
    public void Variance_ratio_rejects_null_or_invalid_inputs()
    {
        Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapVarianceRatio(
                lowSampleRole: "",
                highSampleRole: "refined",
                statisticalMetric: "k_eff",
                sampleRatio: new ConstantParameterExpression(4.0),
                sigmaMultiplier: 3.0));
        Assert.Throws<ArgumentNullException>(() =>
            LegacyAssertionPredicateMapper.MapVarianceRatio(
                lowSampleRole: "coarse",
                highSampleRole: "refined",
                statisticalMetric: "k_eff",
                sampleRatio: null!,
                sigmaMultiplier: 3.0));
    }

    [Fact]
    public void Flux_pointwise_approx_maps_to_field_equality_with_identity_pairing()
    {
        var predicate = LegacyAssertionPredicateMapper.MapFluxPointwise(
            leftRole: "followup",
            rightRole: "source",
            leftMetric: "flux",
            rightMetric: "flux",
            norm: "L2",
            atol: 1e-4,
            rtol: 1e-3);

        var field = Assert.IsType<FieldEqualityPredicate>(predicate);
        Assert.Equal("followup", field.LeftRole);
        Assert.Equal("source", field.RightRole);
        Assert.Equal("flux", field.LeftMetric);
        Assert.Equal("flux", field.RightMetric);
        Assert.IsType<IdentityFieldPairing>(field.Pairing);
        Assert.Equal("L2", field.Tolerance.Norm);
        Assert.Equal(1e-4, field.Tolerance.Atol);
        Assert.Equal(1e-3, field.Tolerance.Rtol);
    }

    [Fact]
    public void Flux_pointwise_approx_rejects_invalid_inputs()
    {
        Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapFluxPointwise(
                leftRole: "",
                rightRole: "source",
                leftMetric: "flux",
                rightMetric: "flux",
                norm: "L2",
                atol: 0.0,
                rtol: 0.0));
        Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapFluxPointwise(
                leftRole: "followup",
                rightRole: "source",
                leftMetric: "flux",
                rightMetric: "flux",
                norm: "",
                atol: 0.0,
                rtol: 0.0));
    }

    [Fact]
    public void Cross_program_agree_maps_to_cross_method_comparison_predicate()
    {
        var predicate = LegacyAssertionPredicateMapper.MapCrossProgramAgree(
            leftRole: "openmoc",
            rightRole: "openmc",
            metric: "k_eff",
            atol: 0.0,
            rtol: 5e-3);

        var cross = Assert.IsType<CrossMethodComparisonPredicate>(predicate);
        Assert.Equal("openmoc", cross.LeftRole);
        Assert.Equal("openmc", cross.RightRole);
        Assert.Equal("k_eff", cross.Metric);
        Assert.Equal("Equal", cross.Operator);
        Assert.Equal(0.0, cross.Tolerance.Atol);
        Assert.Equal(5e-3, cross.Tolerance.Rtol);
        Assert.False(string.IsNullOrEmpty(cross.PredicateId));
    }

    [Fact]
    public void Cross_program_agree_rejects_invalid_inputs()
    {
        Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapCrossProgramAgree(
                leftRole: "",
                rightRole: "openmc",
                metric: "k_eff",
                atol: 0.0,
                rtol: 5e-3));
        Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapCrossProgramAgree(
                leftRole: "openmoc",
                rightRole: "openmc",
                metric: "",
                atol: 0.0,
                rtol: 5e-3));
    }

    [Fact]
    public void MapScalar_rejects_null_or_empty_role_or_metric()
    {
        Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapScalar("less", "", "source", "k_eff"));
        Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapScalar("less", "followup", "", "k_eff"));
        Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapScalar("less", "followup", "source", ""));
    }

    [Fact]
    public void MapScaling_rejects_null_factor()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LegacyAssertionPredicateMapper.MapScaling(
                actualRole: "followup",
                expectedRole: "source",
                metric: "phi",
                factor: null!,
                exponent: 1.0));
    }
}
