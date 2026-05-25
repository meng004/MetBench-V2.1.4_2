using System;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// Migration-input contract for the legacy-string-code to typed-predicate mapper.
/// Scope (verification-semantics convergence PR-C):
///   less    -> BinaryComparisonPredicate(Operator=Less)
///   greater -> BinaryComparisonPredicate(Operator=Greater)
///   approx  -> BinaryComparisonPredicate(Operator=Equal)
///   scaling flw = k * src -> ScaledEqualityPredicate
/// Any other legacy code is rejected fail-closed; the typed runtime
/// has no production fallback to AssertionEvaluator after this PR.
/// </summary>
public sealed class LegacyAssertionPredicateMapperTests
{
    [Theory]
    [InlineData("less", "Less")]
    [InlineData("greater", "Greater")]
    [InlineData("approx", "Equal")]
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
    [InlineData("approx-invariant")]
    [InlineData("variance-ratio")]
    [InlineData("flux-pointwise-approx")]
    [InlineData("cross-program-agree")]
    [InlineData("string-switch-new-code")]
    [InlineData("")]
    public void Unknown_or_out_of_scope_legacy_code_is_rejected_fail_closed(string code)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            LegacyAssertionPredicateMapper.MapScalar(code, "followup", "source", "k_eff"));

        Assert.Contains("Unsupported legacy assertion code", ex.Message, StringComparison.Ordinal);
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
