using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// PR-N1 noise-aware typed scalar predicate — kernel contract tests for
/// <see cref="NoiseAwareBinaryComparisonKernel"/>. Mirrors the
/// <c>BinaryComparisonKernelTests</c> shape (helper-built <see cref="VerificationContext"/>
/// with two roles) and adds the σ metric values on each side so the kernel can compute
/// <c>NoiseMultiplier · √(σ_source² + σ_followup²)</c>.
/// </summary>
public sealed class NoiseAwareBinaryComparisonKernelTests
{
    private const string MetricName = "k_eff";
    private const string SourceStdMetricName = "k_eff_sigma_source";
    private const string FollowupStdMetricName = "k_eff_sigma_followup";

    [Fact]
    public void Greater_passes_when_followup_strictly_above_source_minus_noise_band()
    {
        // left=1.0, right=2.0, σ_s=0.1, σ_f=0.1, k=3 → noise tol = 3·√0.02 ≈ 0.4243.
        // Greater: 2.0 > 1.0 - 0.4243 = 0.5757 → Pass.
        var result = Kernel().Evaluate(
            Predicate("Greater", noiseMultiplier: 3.0),
            Context(left: 1.0, right: 2.0, sigmaSource: 0.1, sigmaFollowup: 0.1));

        Assert.Equal(VerifyStatus.Passed, result.Status);
        Assert.True(result.Assertion!.Passed);
    }

    [Fact]
    public void Less_passes_when_followup_strictly_below_source_plus_noise_band()
    {
        var result = Kernel().Evaluate(
            Predicate("Less", noiseMultiplier: 3.0),
            Context(left: 2.0, right: 1.0, sigmaSource: 0.1, sigmaFollowup: 0.1));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void Greater_passes_for_tiny_reverse_move_inside_noise_band()
    {
        // left=1.0, right=0.95 (-0.05 move), σ_s=0.1, σ_f=0.1, k=3 → tol ≈ 0.4243.
        // Greater: 0.95 > 1.0 - 0.4243 = 0.5757 → Pass (move is inside noise tolerance).
        var result = Kernel().Evaluate(
            Predicate("Greater", noiseMultiplier: 3.0),
            Context(left: 1.0, right: 0.95, sigmaSource: 0.1, sigmaFollowup: 0.1));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void Greater_fails_when_followup_drops_beyond_noise_band()
    {
        // left=1.0, right=0.5, σ_s=0.1, σ_f=0.1, k=3 → tol ≈ 0.4243.
        // Greater: 0.5 > 1.0 - 0.4243 = 0.5757 → false → Fail.
        var result = Kernel().Evaluate(
            Predicate("Greater", noiseMultiplier: 3.0),
            Context(left: 1.0, right: 0.5, sigmaSource: 0.1, sigmaFollowup: 0.1));

        Assert.Equal(VerifyStatus.Failed, result.Status);
        Assert.False(result.Assertion!.Passed);
        Assert.Contains("Greater", result.Assertion.FailureReason ?? "");
    }

    [Fact]
    public void Less_fails_when_followup_rises_beyond_noise_band()
    {
        var result = Kernel().Evaluate(
            Predicate("Less", noiseMultiplier: 3.0),
            Context(left: 1.0, right: 1.5, sigmaSource: 0.1, sigmaFollowup: 0.1));

        Assert.Equal(VerifyStatus.Failed, result.Status);
    }

    [Fact]
    public void Operator_Equal_is_InvalidSpec_with_explicit_diagnostic()
    {
        var result = Kernel().Evaluate(
            Predicate("Equal", noiseMultiplier: 3.0),
            Context(left: 1.0, right: 1.0, sigmaSource: 0.1, sigmaFollowup: 0.1));

        Assert.Equal(VerifyStatus.InvalidSpec, result.Status);
        Assert.NotNull(result.Context);
        Assert.Contains("Equal", result.Context!.Reason);
    }

    [Theory]
    [InlineData("NotEqual")]
    [InlineData("")]
    [InlineData("greater")]   // case-sensitive: lower-case rejected
    public void Operator_outside_Greater_or_Less_is_InvalidSpec(string badOperator)
    {
        var result = Kernel().Evaluate(
            Predicate(badOperator, noiseMultiplier: 3.0),
            Context(left: 1.0, right: 1.0, sigmaSource: 0.1, sigmaFollowup: 0.1));

        Assert.Equal(VerifyStatus.InvalidSpec, result.Status);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NoiseMultiplier_invalid_is_InvalidSpec(double bad)
    {
        var result = Kernel().Evaluate(
            Predicate("Greater", noiseMultiplier: bad),
            Context(left: 1.0, right: 2.0, sigmaSource: 0.1, sigmaFollowup: 0.1));

        Assert.Equal(VerifyStatus.InvalidSpec, result.Status);
    }

    [Theory]
    [InlineData(double.NaN, 1.0, 0.1, 0.1)]
    [InlineData(1.0, double.NaN, 0.1, 0.1)]
    [InlineData(1.0, 1.0, double.NaN, 0.1)]
    [InlineData(1.0, 1.0, 0.1, double.NaN)]
    [InlineData(1.0, double.PositiveInfinity, 0.1, 0.1)]
    [InlineData(1.0, 1.0, double.NegativeInfinity, 0.1)]
    public void NonFinite_metric_value_is_SkippedMissingObservable(
        double left, double right, double sigmaSource, double sigmaFollowup)
    {
        var result = Kernel().Evaluate(
            Predicate("Greater", noiseMultiplier: 3.0),
            Context(left: left, right: right, sigmaSource: sigmaSource, sigmaFollowup: sigmaFollowup));

        Assert.Equal(VerifyStatus.SkippedMissingObservable, result.Status);
    }

    [Fact]
    public void Negative_sigma_value_is_SkippedMissingObservable()
    {
        var result = Kernel().Evaluate(
            Predicate("Greater", noiseMultiplier: 3.0),
            Context(left: 1.0, right: 2.0, sigmaSource: -0.1, sigmaFollowup: 0.1));

        Assert.Equal(VerifyStatus.SkippedMissingObservable, result.Status);
        Assert.Contains("sourceStd", result.Context!.Reason);
    }

    [Fact]
    public void Diagnostic_carries_left_right_residual_and_noise_tolerance()
    {
        var result = Kernel().Evaluate(
            Predicate("Greater", noiseMultiplier: 3.0),
            Context(left: 1.0, right: 2.0, sigmaSource: 0.1, sigmaFollowup: 0.1));

        Assert.NotNull(result.Diagnostic);
        Assert.Equal(1.0, result.Diagnostic!.Expected);
        Assert.Equal(2.0, result.Diagnostic.Actual);
        Assert.Equal(1.0, result.Diagnostic.Residual);
        Assert.True(result.Diagnostic.Tolerance > 0.4 && result.Diagnostic.Tolerance < 0.45,
            $"expected noise tolerance ≈ 0.4243, got {result.Diagnostic.Tolerance}");
    }

    private static NoiseAwareBinaryComparisonKernel Kernel() => new();

    private static NoiseAwareBinaryComparisonPredicate Predicate(string @operator, double noiseMultiplier) =>
        new(
            PredicateId: "p1",
            LeftRole: "source",
            RightRole: "followup",
            Metric: MetricName,
            Operator: @operator,
            SourceStdMetric: SourceStdMetricName,
            FollowupStdMetric: FollowupStdMetricName,
            NoiseMultiplier: noiseMultiplier);

    private static VerificationContext Context(double left, double right, double sigmaSource, double sigmaFollowup)
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-noise",
            "test",
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
                [MetricName] = new ScalarProjectionSpec("/results/k_eff"),
                [SourceStdMetricName] = new ScalarProjectionSpec("/results/k_eff_sigma_source"),
                [FollowupStdMetricName] = new ScalarProjectionSpec("/results/k_eff_sigma_followup"),
            },
            new PredicateSpec[] { Predicate("Greater", 3.0) },
            new DeterministicToleranceSpec(0.0, 0.0));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["source"] = new(
                    "source",
                    new Dictionary<string, double>
                    {
                        [MetricName] = left,
                        [SourceStdMetricName] = sigmaSource,
                    }),
                ["followup"] = new(
                    "followup",
                    new Dictionary<string, double>
                    {
                        [MetricName] = right,
                        [FollowupStdMetricName] = sigmaFollowup,
                    }),
            });
    }
}
