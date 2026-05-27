using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// PR-Bol-2A pin: <see cref="TypedVerificationContextFactory.FromPhaseOutputs"/> projects
/// a per-phase scalar dictionary into one <see cref="RoleOutput"/> per phase role and
/// produces a context the <see cref="ErrorMonotonicKernel"/> consumes via
/// <c>GetMetric(role, metric)</c>. Independent of PR-VR's 2-side
/// <see cref="TypedVerificationContextFactory.FromScalarOutputs"/>.
/// </summary>
public sealed class TypedVerificationContextFactoryErrorMonotonicTests
{
    private static MrSpec BuildSpec() => TypedSpecFactory.ForErrorMonotonic(
        mrCode: "mr-em",
        metric: "k_eff",
        orderedRoles: new[] { "coarse", "medium" },
        referenceRole: "reference");

    [Fact]
    public void FromPhaseOutputs_emits_one_role_output_per_phase()
    {
        var spec = BuildSpec();
        var ctx = TypedVerificationContextFactory.FromPhaseOutputs(
            spec,
            phaseScalars: new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["coarse"] = new Dictionary<string, double> { ["k_eff"] = 1.40 },
                ["medium"] = new Dictionary<string, double> { ["k_eff"] = 1.43 },
                ["reference"] = new Dictionary<string, double> { ["k_eff"] = 1.44 },
            },
            parameterValues: new Dictionary<string, string>());

        Assert.Equal(3, ctx.RoleOutputs.Count);
        Assert.Equal(1.40, ctx.GetMetric("coarse", "k_eff"), precision: 12);
        Assert.Equal(1.43, ctx.GetMetric("medium", "k_eff"), precision: 12);
        Assert.Equal(1.44, ctx.GetMetric("reference", "k_eff"), precision: 12);
    }

    [Fact]
    public void FromPhaseOutputs_role_output_metric_dict_is_independent_copy()
    {
        // Mutating the input scalar dict after FromPhaseOutputs builds the context must
        // not leak into RoleOutput.Metrics; we copy on entry.
        var spec = BuildSpec();
        var coarseDict = new Dictionary<string, double> { ["k_eff"] = 1.40 };
        var ctx = TypedVerificationContextFactory.FromPhaseOutputs(
            spec,
            phaseScalars: new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["coarse"] = coarseDict,
                ["medium"] = new Dictionary<string, double> { ["k_eff"] = 1.43 },
                ["reference"] = new Dictionary<string, double> { ["k_eff"] = 1.44 },
            },
            parameterValues: new Dictionary<string, string>());

        coarseDict["k_eff"] = 99.9;
        Assert.Equal(1.40, ctx.GetMetric("coarse", "k_eff"), precision: 12);
    }

    [Fact]
    public void FromPhaseOutputs_throws_when_spec_validation_fails()
    {
        // Empty Predicates list yields a spec that fails MrSpecValidator.
        var invalid = new MrSpec(
            Kind: "MrSpec", MrId: "mr-em", Name: "mr-em",
            Description: null, Tags: null, Parameters: null,
            Roles: new Dictionary<string, RunRoleSpec>(),
            Projections: null,
            Predicates: Array.Empty<PredicateSpec>(),
            DefaultTolerance: new DeterministicToleranceSpec(0, 0));

        Assert.Throws<InvalidOperationException>(() =>
            TypedVerificationContextFactory.FromPhaseOutputs(
                invalid,
                phaseScalars: new Dictionary<string, IReadOnlyDictionary<string, double>>(),
                parameterValues: new Dictionary<string, string>()));
    }

    [Fact]
    public void FromPhaseOutputs_throws_when_a_phase_scalar_dict_is_null()
    {
        var spec = BuildSpec();
        Assert.Throws<ArgumentException>(() =>
            TypedVerificationContextFactory.FromPhaseOutputs(
                spec,
                phaseScalars: new Dictionary<string, IReadOnlyDictionary<string, double>>
                {
                    ["coarse"] = null!,
                    ["medium"] = new Dictionary<string, double> { ["k_eff"] = 1.43 },
                    ["reference"] = new Dictionary<string, double> { ["k_eff"] = 1.44 },
                },
                parameterValues: new Dictionary<string, string>()));
    }

    [Fact]
    public void FromPhaseOutputs_parses_parameter_values_with_invariant_culture()
    {
        var spec = BuildSpec();
        var ctx = TypedVerificationContextFactory.FromPhaseOutputs(
            spec,
            phaseScalars: new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["coarse"] = new Dictionary<string, double> { ["k_eff"] = 1.40 },
                ["medium"] = new Dictionary<string, double> { ["k_eff"] = 1.43 },
                ["reference"] = new Dictionary<string, double> { ["k_eff"] = 1.44 },
            },
            parameterValues: new Dictionary<string, string> { ["factor"] = "2.0" });

        Assert.Equal(2.0, ctx.Inputs["factor"], precision: 12);
    }

    [Fact]
    public void Kernel_dispatch_through_factory_passes_on_monotonic_error_decrease()
    {
        // Reference = 1.44. Errors: |1.40-1.44|=0.04 ≥ |1.43-1.44|=0.01 → Pass.
        var spec = BuildSpec();
        var ctx = TypedVerificationContextFactory.FromPhaseOutputs(
            spec,
            phaseScalars: new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["coarse"] = new Dictionary<string, double> { ["k_eff"] = 1.40 },
                ["medium"] = new Dictionary<string, double> { ["k_eff"] = 1.43 },
                ["reference"] = new Dictionary<string, double> { ["k_eff"] = 1.44 },
            },
            parameterValues: new Dictionary<string, string>());

        var dispatcher = new PredicateDispatcher();
        var result = dispatcher.Dispatch((ErrorMonotonicPredicate)spec.Predicates![0], ctx);
        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void Kernel_dispatch_through_factory_fails_on_non_monotonic_error()
    {
        // Reference = 1.44. Errors: |1.45-1.44|=0.01 < |1.43-1.44|=0.01 (tie) but medium
        // moves AWAY from reference: 1.43 vs reference 1.44 → 0.01; then 1.45 → 0.01.
        // Make it clearly non-monotonic: coarse=1.43 (err 0.01), medium=1.40 (err 0.04).
        var spec = BuildSpec();
        var ctx = TypedVerificationContextFactory.FromPhaseOutputs(
            spec,
            phaseScalars: new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["coarse"] = new Dictionary<string, double> { ["k_eff"] = 1.43 },
                ["medium"] = new Dictionary<string, double> { ["k_eff"] = 1.40 },
                ["reference"] = new Dictionary<string, double> { ["k_eff"] = 1.44 },
            },
            parameterValues: new Dictionary<string, string>());

        var dispatcher = new PredicateDispatcher();
        var result = dispatcher.Dispatch((ErrorMonotonicPredicate)spec.Predicates![0], ctx);
        Assert.Equal(VerifyStatus.Failed, result.Status);
    }
}
