using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// PR-N1 noise-aware typed scalar predicate — validator contract tests.
/// Mirrors <c>BinaryComparisonPredicateValidator</c>'s role + metric existence
/// checks and adds existence checks for <c>SourceStdMetric</c> / <c>FollowupStdMetric</c>
/// plus operator and noise-multiplier validity.
/// </summary>
public sealed class NoiseAwareBinaryComparisonPredicateValidatorTests
{
    private static NoiseAwareBinaryComparisonPredicateValidator Validator() =>
        new(new SharedReferenceResolver());

    [Fact]
    public void Validate_happy_path_returns_no_errors()
    {
        var result = Validator().Validate(GoodPredicate(), BuildSpec());

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.Path + ": " + e.Message)));
    }

    [Fact]
    public void Validate_rejects_unknown_left_role()
    {
        var pred = GoodPredicate() with { LeftRole = "ghost" };
        var result = Validator().Validate(pred, BuildSpec());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.EndsWith("left_role"));
    }

    [Fact]
    public void Validate_rejects_unknown_right_role()
    {
        var pred = GoodPredicate() with { RightRole = "ghost" };
        var result = Validator().Validate(pred, BuildSpec());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.EndsWith("right_role"));
    }

    [Fact]
    public void Validate_rejects_unknown_metric()
    {
        var pred = GoodPredicate() with { Metric = "phi_max" };
        var result = Validator().Validate(pred, BuildSpec());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.EndsWith(".metric"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_rejects_blank_source_std_metric(string blank)
    {
        var pred = GoodPredicate() with { SourceStdMetric = blank };
        var result = Validator().Validate(pred, BuildSpec());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.EndsWith("source_std_metric"));
    }

    [Fact]
    public void Validate_rejects_unknown_source_std_metric()
    {
        var pred = GoodPredicate() with { SourceStdMetric = "ghost_sigma" };
        var result = Validator().Validate(pred, BuildSpec());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.EndsWith("source_std_metric"));
    }

    [Fact]
    public void Validate_rejects_blank_followup_std_metric()
    {
        var pred = GoodPredicate() with { FollowupStdMetric = "" };
        var result = Validator().Validate(pred, BuildSpec());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.EndsWith("followup_std_metric"));
    }

    [Fact]
    public void Validate_rejects_unknown_followup_std_metric()
    {
        var pred = GoodPredicate() with { FollowupStdMetric = "ghost_sigma" };
        var result = Validator().Validate(pred, BuildSpec());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.EndsWith("followup_std_metric"));
    }

    [Theory]
    [InlineData("Equal")]
    [InlineData("NotEqual")]
    [InlineData("")]
    [InlineData("greater")]
    public void Validate_rejects_operator_outside_Greater_or_Less(string badOperator)
    {
        var pred = GoodPredicate() with { Operator = badOperator };
        var result = Validator().Validate(pred, BuildSpec());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.EndsWith(".operator"));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_rejects_invalid_noise_multiplier(double bad)
    {
        var pred = GoodPredicate() with { NoiseMultiplier = bad };
        var result = Validator().Validate(pred, BuildSpec());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path.EndsWith("noise_multiplier"));
    }

    private static NoiseAwareBinaryComparisonPredicate GoodPredicate() =>
        new(
            PredicateId: "p-noise",
            LeftRole: "source",
            RightRole: "followup",
            Metric: "k_eff",
            Operator: "Greater",
            SourceStdMetric: "k_eff_sigma_source",
            FollowupStdMetric: "k_eff_sigma_followup",
            NoiseMultiplier: 3.0);

    private static MrSpec BuildSpec() =>
        new(
            "MrSpec",
            "mr-noise",
            "noise-aware-test",
            null,
            new Dictionary<string, string>(),
            new Dictionary<string, ParameterExpression>(),
            new Dictionary<string, RunRoleSpec>
            {
                ["source"] = new("Baseline"),
                ["followup"] = new("Followup"),
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["k_eff"] = new ScalarProjectionSpec("/results/k_eff"),
                ["k_eff_sigma_source"] = new ScalarProjectionSpec("/results/k_eff_sigma_source"),
                ["k_eff_sigma_followup"] = new ScalarProjectionSpec("/results/k_eff_sigma_followup"),
            },
            new PredicateSpec[] { GoodPredicate() },
            new DeterministicToleranceSpec(0.0, 0.0));
}
