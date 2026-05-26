using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Pipeline;

/// <summary>
/// PR-VR pin: <see cref="TypedSpecFactory.ForLegacyAssertion"/> is the single
/// migration entry point that the pipeline calls. For
/// <c>AssertionTypeCode = "variance-ratio"</c> it delegates to
/// <see cref="TypedSpecFactory.ForVarianceRatio"/> with <c>factor</c> read
/// from the pipeline parameters dictionary. Validates input parsing,
/// tolerance translation (ToleranceRel → SigmaMultiplier = 1 + ToleranceRel),
/// and fail-closed errors for missing / malformed inputs.
/// <para>
/// String dispatch on <c>AssertionTypeCodes</c> is intentionally confined
/// to <c>Catalog/Typed/Migration/</c> per the SemanticCatalogBoundaryTests
/// architecture guard — these tests live in the test project's
/// <c>SystemMT.Pipeline</c> folder because they pin the *pipeline-facing*
/// contract of the helper, but they call <see cref="TypedSpecFactory"/>
/// directly to avoid re-creating a Pipeline ↔ string-dispatch coupling.
/// </para>
/// </summary>
public sealed class VarianceRatioPipelineWiringTests
{
    private static MrSpec Invoke(
        string valueName = "k_eff_std",
        string? factor = "4",
        double toleranceRel = 0.30,
        string mrCode = "mr-vr")
    {
        var parameters = new Dictionary<string, string>();
        if (factor is not null)
        {
            parameters["factor"] = factor;
        }

        return TypedSpecFactory.ForLegacyAssertion(
            mrCode: mrCode,
            assertionTypeCode: AssertionTypeCodes.VarianceRatio,
            valueName: valueName,
            parameters: parameters,
            toleranceAbs: 0.0,
            toleranceRel: toleranceRel);
    }

    [Fact]
    public void ForLegacyAssertion_variance_ratio_happy_path_emits_typed_spec_with_variance_ratio_predicate()
    {
        var spec = Invoke();

        Assert.NotNull(spec.Predicates);
        var predicate = Assert.IsType<VarianceRatioPredicate>(spec.Predicates![0]);
        Assert.Equal("k_eff_std", predicate.StatisticalMetric);
        Assert.Equal("source", predicate.LowSampleRole);
        Assert.Equal("followup", predicate.HighSampleRole);
    }

    [Fact]
    public void ForLegacyAssertion_variance_ratio_pipes_factor_parameter_into_sample_ratio()
    {
        var spec = Invoke(factor: "4");

        var predicate = (VarianceRatioPredicate)spec.Predicates![0];
        var sampleRatio = Assert.IsType<ConstantParameterExpression>(predicate.SampleRatio);
        Assert.Equal(4.0, sampleRatio.Value);
    }

    [Fact]
    public void ForLegacyAssertion_variance_ratio_pipes_tolerance_rel_into_sigma_multiplier_as_one_plus_rel()
    {
        var spec = Invoke(toleranceRel: 0.25);

        var predicate = (VarianceRatioPredicate)spec.Predicates![0];
        Assert.Equal(1.25, predicate.Tolerance.SigmaMultiplier, precision: 12);
    }

    [Fact]
    public void ForLegacyAssertion_variance_ratio_resulting_spec_passes_typed_validation()
    {
        var spec = Invoke();

        var validation = spec.Validate();
        Assert.True(validation.IsValid,
            "Migration-built variance-ratio spec must pass typed validation. Errors: "
            + string.Join("; ", validation.Errors));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ForLegacyAssertion_variance_ratio_rejects_blank_value_name(string blank)
    {
        Assert.Throws<ArgumentException>(() => Invoke(valueName: blank));
    }

    [Fact]
    public void ForLegacyAssertion_variance_ratio_rejects_missing_factor_parameter()
    {
        var ex = Assert.Throws<ArgumentException>(() => Invoke(factor: null));
        Assert.Contains("factor", ex.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    public void ForLegacyAssertion_variance_ratio_rejects_non_numeric_factor(string raw)
    {
        var ex = Assert.Throws<ArgumentException>(() => Invoke(factor: raw));
        Assert.Contains("factor", ex.Message);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0.5")]
    [InlineData("0")]
    [InlineData("-2")]
    public void ForLegacyAssertion_variance_ratio_rejects_factor_not_strictly_greater_than_one(string raw)
    {
        Assert.Throws<ArgumentException>(() => Invoke(factor: raw));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.10)]
    public void ForLegacyAssertion_variance_ratio_rejects_non_positive_tolerance_rel(double toleranceRel)
    {
        Assert.Throws<ArgumentException>(() => Invoke(toleranceRel: toleranceRel));
    }

    [Fact]
    public void ForLegacyAssertion_variance_ratio_parses_factor_with_invariant_culture_decimal_separator()
    {
        // Defence-in-depth: pipeline parameter `factor` must parse with invariant
        // culture even on hosts where `,` is the decimal separator.
        var spec = Invoke(factor: "4.0");

        var predicate = (VarianceRatioPredicate)spec.Predicates![0];
        var sampleRatio = (ConstantParameterExpression)predicate.SampleRatio;
        Assert.Equal(4.0, sampleRatio.Value);
    }

    [Fact]
    public void ForLegacyAssertion_non_variance_ratio_code_still_routes_through_map_scalar_path()
    {
        // Sanity: the new dispatch must not regress the legacy scalar
        // less/greater/approx/equal path — those continue to flow through
        // LegacyAssertionPredicateMapper.MapScalar inside the same helper.
        var spec = TypedSpecFactory.ForLegacyAssertion(
            mrCode: "mr-bin",
            assertionTypeCode: "less",
            valueName: "k_eff",
            parameters: new Dictionary<string, string>(),
            toleranceAbs: 1e-6,
            toleranceRel: 1e-4);

        Assert.NotNull(spec.Predicates);
        Assert.IsType<BinaryComparisonPredicate>(spec.Predicates![0]);
    }

    [Fact]
    public void ForLegacyAssertion_unknown_code_throws_argument_exception_from_map_scalar()
    {
        // Unknown legacy codes still fail closed via LegacyAssertionPredicateMapper.
        Assert.Throws<ArgumentException>(() =>
            TypedSpecFactory.ForLegacyAssertion(
                mrCode: "mr-?",
                assertionTypeCode: "totally-bogus-code",
                valueName: "k_eff",
                parameters: new Dictionary<string, string>(),
                toleranceAbs: 0.0,
                toleranceRel: 1e-4));
    }
}
