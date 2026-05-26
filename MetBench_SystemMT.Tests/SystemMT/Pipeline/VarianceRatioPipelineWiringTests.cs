using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Pipeline;

/// <summary>
/// PR-VR pin: <see cref="SystemMtPipeline.BuildVarianceRatioSpec"/> turns a
/// <c>variance-ratio</c> <see cref="PipelineContext"/> into the typed
/// <see cref="MrSpec"/> the runtime dispatcher expects. Validates input
/// parsing (factor from <c>Parameters["factor"]</c>), tolerance translation
/// (ToleranceRel → SigmaMultiplier = 1 + ToleranceRel), and fail-closed
/// errors for missing / malformed inputs.
/// </summary>
public sealed class VarianceRatioPipelineWiringTests
{
    private static PipelineContext MakeContext(
        string valueName = "k_eff_std",
        string factor = "4",
        double toleranceRel = 0.30,
        string mrCode = "mr-vr")
    {
        var parameters = new Dictionary<string, string>();
        if (factor is not null)
        {
            parameters["factor"] = factor;
        }

        return new PipelineContext(
            MrCode: mrCode,
            TransformationName: "ScaleField",
            AssertionTypeCode: AssertionTypeCodes.VarianceRatio,
            ValueName: valueName,
            TargetFieldPath: "/solver/particles",
            PathSyntax: "json-pointer",
            Parameters: parameters,
            Tolerance: new AssertionTolerance(NoiseAware: true, ToleranceRel: toleranceRel, NoiseMultiplier: 1.0),
            ExtraAssertionValues: null,
            SutName: "test-sut",
            SourceCasePath: "fake.in.json",
            WorkingDirectory: "/tmp/fake",
            InputParserCommand: "fake",
            OutputParserCommand: "fake",
            RunnerCommand: "fake",
            TimeoutSeconds: 30,
            CatalogVersionSha: "test",
            SutVersionSnapshot: "test",
            MetbenchVersion: "v2",
            TriggeredBy: "test");
    }

    [Fact]
    public void BuildVarianceRatioSpec_happy_path_emits_typed_spec_with_variance_ratio_predicate()
    {
        var spec = SystemMtPipeline.BuildVarianceRatioSpec(MakeContext());

        Assert.NotNull(spec.Predicates);
        var predicate = Assert.IsType<VarianceRatioPredicate>(spec.Predicates![0]);
        Assert.Equal("k_eff_std", predicate.StatisticalMetric);
        Assert.Equal("source", predicate.LowSampleRole);
        Assert.Equal("followup", predicate.HighSampleRole);
    }

    [Fact]
    public void BuildVarianceRatioSpec_pipes_factor_parameter_into_sample_ratio()
    {
        var spec = SystemMtPipeline.BuildVarianceRatioSpec(MakeContext(factor: "4"));

        var predicate = (VarianceRatioPredicate)spec.Predicates![0];
        var sampleRatio = Assert.IsType<ConstantParameterExpression>(predicate.SampleRatio);
        Assert.Equal(4.0, sampleRatio.Value);
    }

    [Fact]
    public void BuildVarianceRatioSpec_pipes_tolerance_rel_into_sigma_multiplier_as_one_plus_rel()
    {
        var spec = SystemMtPipeline.BuildVarianceRatioSpec(
            MakeContext(toleranceRel: 0.25));

        var predicate = (VarianceRatioPredicate)spec.Predicates![0];
        Assert.Equal(1.25, predicate.Tolerance.SigmaMultiplier, precision: 12);
    }

    [Fact]
    public void BuildVarianceRatioSpec_resulting_spec_passes_typed_validation()
    {
        var spec = SystemMtPipeline.BuildVarianceRatioSpec(MakeContext());

        var validation = spec.Validate();
        Assert.True(validation.IsValid,
            "Pipeline-built variance-ratio spec must pass typed validation. Errors: "
            + string.Join("; ", validation.Errors));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void BuildVarianceRatioSpec_rejects_blank_value_name(string blank)
    {
        Assert.Throws<ArgumentException>(() =>
            SystemMtPipeline.BuildVarianceRatioSpec(MakeContext(valueName: blank)));
    }

    [Fact]
    public void BuildVarianceRatioSpec_rejects_missing_factor_parameter()
    {
        var ctx = MakeContext(factor: null!);
        var ex = Assert.Throws<ArgumentException>(() => SystemMtPipeline.BuildVarianceRatioSpec(ctx));
        Assert.Contains("factor", ex.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildVarianceRatioSpec_rejects_non_numeric_factor(string raw)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SystemMtPipeline.BuildVarianceRatioSpec(MakeContext(factor: raw)));
        Assert.Contains("factor", ex.Message);
    }

    [Theory]
    [InlineData("1")]      // no refinement
    [InlineData("0.5")]    // factor < 1
    [InlineData("0")]
    [InlineData("-2")]
    public void BuildVarianceRatioSpec_rejects_factor_not_strictly_greater_than_one(string raw)
    {
        Assert.Throws<ArgumentException>(() =>
            SystemMtPipeline.BuildVarianceRatioSpec(MakeContext(factor: raw)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.10)]
    public void BuildVarianceRatioSpec_rejects_non_positive_tolerance_rel(double toleranceRel)
    {
        Assert.Throws<ArgumentException>(() =>
            SystemMtPipeline.BuildVarianceRatioSpec(MakeContext(toleranceRel: toleranceRel)));
    }

    [Fact]
    public void BuildVarianceRatioSpec_parses_factor_with_invariant_culture_decimal_separator()
    {
        // Defence-in-depth: even on a host with `,` as decimal separator the
        // pipeline must parse blueprint default parameters with invariant culture.
        var spec = SystemMtPipeline.BuildVarianceRatioSpec(MakeContext(factor: "4.0"));

        var predicate = (VarianceRatioPredicate)spec.Predicates![0];
        var sampleRatio = (ConstantParameterExpression)predicate.SampleRatio;
        Assert.Equal(4.0, sampleRatio.Value);
    }
}
