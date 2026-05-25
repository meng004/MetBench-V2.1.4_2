using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Catalog.Typed.Migration;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

/// <summary>
/// Verification-semantics convergence PR-C contract: the System MT pipeline assertion
/// stage must produce a SystemMtAssertionResultV2 by routing through
/// PredicateDispatcher + VerificationContext on the typed semantic catalog,
/// not through the legacy AssertionEvaluator. These tests pin the typed runtime
/// surface independent of process orchestration so the runtime contract can
/// regress only on typed-catalog changes.
/// </summary>
public sealed class SystemMtPipelineTypedRuntimeContractTests
{
    [Fact]
    public void Typed_context_factory_builds_valid_context_from_scalar_outputs()
    {
        var spec = TypedSpecFactory.ForScalarBinaryComparison(
            mrCode: "MR-less",
            valueName: "k_eff",
            actualRole: "followup",
            expectedRole: "source",
            @operator: "Less",
            toleranceAbs: 0.0,
            toleranceRel: 0.0);

        var context = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["k_eff"] = 1.13 },
            followupScalars: new Dictionary<string, double> { ["k_eff"] = 0.51 },
            parameterValues: new Dictionary<string, string>());

        Assert.True(context.SpecValidation.IsValid,
            "Typed spec synthesized from PipelineContext must validate before runtime dispatch.");
        Assert.True(context.TryGetMetric("source", "k_eff", out var sourceMetric));
        Assert.Equal(1.13, sourceMetric);
        Assert.True(context.TryGetMetric("followup", "k_eff", out var followupMetric));
        Assert.Equal(0.51, followupMetric);
    }

    [Fact]
    public void Pipeline_assertion_stage_routes_less_through_typed_binary_comparison_passing()
    {
        var predicate = LegacyAssertionPredicateMapper.MapScalar(
            "less", actualRole: "followup", expectedRole: "source", metric: "k_eff");
        var spec = TypedSpecFactory.ForLegacyScalar(
            mrCode: "MR-less",
            valueName: "k_eff",
            predicate: predicate,
            toleranceAbs: 0.0,
            toleranceRel: 0.0);

        var context = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["k_eff"] = 1.13 },
            followupScalars: new Dictionary<string, double> { ["k_eff"] = 0.51 },
            parameterValues: new Dictionary<string, string>());

        var verification = new PredicateDispatcher().Dispatch(spec.Predicates.Single(), context);

        Assert.Equal(VerifyStatus.Passed, verification.Status);
        Assert.NotNull(verification.Assertion);
        Assert.True(verification.Assertion!.Passed);
        Assert.Equal(1.13, verification.Assertion.SourceValue);
        Assert.Equal(0.51, verification.Assertion.FollowupValue);
    }

    [Fact]
    public void Pipeline_assertion_stage_routes_greater_through_typed_binary_comparison_failing()
    {
        var predicate = LegacyAssertionPredicateMapper.MapScalar(
            "greater", actualRole: "followup", expectedRole: "source", metric: "k_eff");
        var spec = TypedSpecFactory.ForLegacyScalar(
            mrCode: "MR-greater",
            valueName: "k_eff",
            predicate: predicate,
            toleranceAbs: 0.0,
            toleranceRel: 0.0);

        var context = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["k_eff"] = 1.13 },
            followupScalars: new Dictionary<string, double> { ["k_eff"] = 0.51 },
            parameterValues: new Dictionary<string, string>());

        var verification = new PredicateDispatcher().Dispatch(spec.Predicates.Single(), context);

        Assert.Equal(VerifyStatus.Failed, verification.Status);
        Assert.NotNull(verification.Assertion);
        Assert.False(verification.Assertion!.Passed);
    }

    [Fact]
    public void Pipeline_assertion_stage_routes_approx_through_typed_deterministic_tolerance()
    {
        var predicate = LegacyAssertionPredicateMapper.MapScalar(
            "approx", actualRole: "followup", expectedRole: "source", metric: "energy");
        var spec = TypedSpecFactory.ForLegacyScalar(
            mrCode: "MR-approx",
            valueName: "energy",
            predicate: predicate,
            toleranceAbs: 0.0,
            toleranceRel: 0.01);

        var context = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["energy"] = 100.0 },
            followupScalars: new Dictionary<string, double> { ["energy"] = 100.5 },
            parameterValues: new Dictionary<string, string>());

        var verification = new PredicateDispatcher().Dispatch(spec.Predicates.Single(), context);

        Assert.Equal(VerifyStatus.Passed, verification.Status);
        Assert.True(verification.Assertion!.Passed);
    }

    [Fact]
    public void Pipeline_scaling_relation_routes_through_scaled_equality_predicate()
    {
        var predicate = LegacyAssertionPredicateMapper.MapScaling(
            actualRole: "followup",
            expectedRole: "source",
            metric: "phi_max",
            factor: new MrParameterRefExpression("factor"),
            exponent: 1.0);
        var parameterValues = new Dictionary<string, string> { ["factor"] = "2" };
        var spec = TypedSpecFactory.ForLegacyScaling(
            mrCode: "MR-scaling",
            valueName: "phi_max",
            predicate: (ScaledEqualityPredicate)predicate,
            factorParameterName: "factor",
            parameterValues: parameterValues,
            toleranceAbs: 0.0,
            toleranceRel: 0.0);

        var context = TypedVerificationContextFactory.FromScalarOutputs(
            spec,
            sourceScalars: new Dictionary<string, double> { ["phi_max"] = 3.0 },
            followupScalars: new Dictionary<string, double> { ["phi_max"] = 6.0 },
            parameterValues: parameterValues);

        var verification = new PredicateDispatcher().Dispatch(spec.Predicates.Single(), context);

        Assert.Equal(VerifyStatus.Passed, verification.Status);
        Assert.True(verification.Assertion!.Passed);
    }

    [Fact]
    public void Pipeline_typed_context_throws_on_invalid_spec_to_keep_runtime_fail_closed()
    {
        // No projections registered → spec validation must fail.
        var spec = new MrSpec(
            "MrSpec",
            "MR-invalid",
            "Invalid",
            null,
            null,
            null,
            new Dictionary<string, RunRoleSpec>
            {
                ["source"] = new("Baseline"),
                ["followup"] = new("Followup"),
            },
            Projections: null,
            Predicates: new PredicateSpec[]
            {
                new BinaryComparisonPredicate("p1", "followup", "source", "k_eff", "Less"),
            },
            DefaultTolerance: new DeterministicToleranceSpec(0.0, 0.0));

        Assert.Throws<System.InvalidOperationException>(() =>
            TypedVerificationContextFactory.FromScalarOutputs(
                spec,
                sourceScalars: new Dictionary<string, double> { ["k_eff"] = 1.0 },
                followupScalars: new Dictionary<string, double> { ["k_eff"] = 0.5 },
                parameterValues: new Dictionary<string, string>()));
    }
}
