using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// PR-VR pin: <see cref="TypedVerificationContextFactory.FromScalarOutputs"/>
/// must promote the statistical metric named by a <see cref="VarianceRatioPredicate"/>
/// from each side's flat scalar map into <see cref="RoleOutput.Statistics"/> so
/// the <see cref="VarianceRatioKernel"/> can read it via
/// <c>context.GetStatistical(role, metric)</c>. Convention: <c>scalars[metric]</c>
/// carries the stderr; the Mean component is set to 0 (the kernel does not use it).
/// Non-variance-ratio specs are unaffected (additive).
/// </summary>
public sealed class TypedVerificationContextFactoryVarianceRatioTests
{
    [Fact]
    public void Promotes_statistical_metric_from_source_scalars_into_low_sample_role_statistics()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr", statisticalMetric: "k_eff_std", factor: 4.0, toleranceRel: 0.30);

        var ctx = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.020 },
            followupScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.010 },
            parameterValues: new Dictionary<string, string> { ["factor"] = "4" });

        Assert.True(ctx.TryGetStatistical("source", "k_eff_std", out var sourceStat));
        Assert.Equal(0.020, sourceStat.StdError, precision: 12);
    }

    [Fact]
    public void Promotes_statistical_metric_from_followup_scalars_into_high_sample_role_statistics()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr", statisticalMetric: "k_eff_std", factor: 4.0, toleranceRel: 0.30);

        var ctx = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.020 },
            followupScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.010 },
            parameterValues: new Dictionary<string, string> { ["factor"] = "4" });

        Assert.True(ctx.TryGetStatistical("followup", "k_eff_std", out var followupStat));
        Assert.Equal(0.010, followupStat.StdError, precision: 12);
    }

    [Fact]
    public void Promotion_uses_zero_mean_because_kernel_does_not_consume_it()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr", statisticalMetric: "k_eff_std", factor: 4.0, toleranceRel: 0.30);

        var ctx = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.020 },
            followupScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.010 },
            parameterValues: new Dictionary<string, string> { ["factor"] = "4" });

        Assert.Equal(0.0, ctx.GetStatistical("source", "k_eff_std").Mean);
        Assert.Equal(0.0, ctx.GetStatistical("followup", "k_eff_std").Mean);
    }

    [Fact]
    public void Source_scalars_keyed_under_low_sample_role_followup_keyed_under_high_sample_role()
    {
        // PR-VR convention: source → expected role → LowSampleRole;
        // followup → actual role → HighSampleRole. The kernel reads
        // low.StdError / sqrt(sampleRatio) and compares to high.StdError.
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr", statisticalMetric: "k_eff_std", factor: 4.0, toleranceRel: 0.30);
        var predicate = (VarianceRatioPredicate)spec.Predicates![0];

        var ctx = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.020 },
            followupScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.010 },
            parameterValues: new Dictionary<string, string> { ["factor"] = "4" });

        var low = ctx.GetStatistical(predicate.LowSampleRole, predicate.StatisticalMetric);
        var high = ctx.GetStatistical(predicate.HighSampleRole, predicate.StatisticalMetric);
        Assert.Equal(0.020, low.StdError, precision: 12);
        Assert.Equal(0.010, high.StdError, precision: 12);
    }

    [Fact]
    public void Missing_metric_on_source_yields_role_without_statistics_for_that_metric()
    {
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr", statisticalMetric: "k_eff_std", factor: 4.0, toleranceRel: 0.30);

        var ctx = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double>(),
            followupScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.010 },
            parameterValues: new Dictionary<string, string> { ["factor"] = "4" });

        Assert.False(ctx.TryGetStatistical("source", "k_eff_std", out _));
        Assert.True(ctx.TryGetStatistical("followup", "k_eff_std", out _));
    }

    [Fact]
    public void Non_variance_ratio_spec_does_not_promote_any_statistical_companion()
    {
        // Sanity: a BinaryComparison spec with both sides carrying a scalar must not
        // suddenly grow a Statistics dict — _std promotion is gated on the predicate
        // being VarianceRatioPredicate.
        var binarySpec = TypedSpecFactory.ForScalarBinaryComparison(
            mrCode: "mr-bin",
            valueName: "k_eff",
            actualRole: "followup",
            expectedRole: "source",
            @operator: "Greater",
            toleranceAbs: 0.0,
            toleranceRel: 0.0);

        var ctx = TypedVerificationContextFactory.FromScalarOutputs(
            binarySpec,
            sourceScalars: new Dictionary<string, double> { ["k_eff"] = 1.0 },
            followupScalars: new Dictionary<string, double> { ["k_eff"] = 2.0 },
            parameterValues: new Dictionary<string, string>());

        Assert.False(ctx.TryGetStatistical("source", "k_eff", out _));
        Assert.False(ctx.TryGetStatistical("followup", "k_eff", out _));
        // Scalar metrics are still in the flat metric map (regression guard).
        Assert.True(ctx.TryGetMetric("source", "k_eff", out _));
        Assert.True(ctx.TryGetMetric("followup", "k_eff", out _));
    }

    [Fact]
    public void Kernel_dispatch_through_factory_built_context_passes_when_high_stderr_meets_threshold()
    {
        // End-to-end smoke from factory output to VarianceRatioKernel via PredicateDispatcher.
        // low=0.020, factor=4 → expected = 0.010; SigmaMultiplier=1.30 → threshold=0.013;
        // high=0.012 ≤ 0.013 → Pass.
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr", statisticalMetric: "k_eff_std", factor: 4.0, toleranceRel: 0.30);

        var ctx = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.020 },
            followupScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.012 },
            parameterValues: new Dictionary<string, string> { ["factor"] = "4" });

        var dispatcher = new PredicateDispatcher();
        var result = dispatcher.Dispatch((VarianceRatioPredicate)spec.Predicates![0], ctx);

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void Kernel_dispatch_through_factory_built_context_fails_when_high_stderr_exceeds_threshold()
    {
        // low=0.020, factor=4 → expected = 0.010; SigmaMultiplier=1.30 → threshold=0.013;
        // high=0.018 > 0.013 → Fail.
        var spec = TypedSpecFactory.ForVarianceRatio(
            mrCode: "mr-vr", statisticalMetric: "k_eff_std", factor: 4.0, toleranceRel: 0.30);

        var ctx = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.020 },
            followupScalars: new Dictionary<string, double> { ["k_eff_std"] = 0.018 },
            parameterValues: new Dictionary<string, string> { ["factor"] = "4" });

        var dispatcher = new PredicateDispatcher();
        var result = dispatcher.Dispatch((VarianceRatioPredicate)spec.Predicates![0], ctx);

        Assert.Equal(VerifyStatus.Failed, result.Status);
    }
}
