using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class VerificationStatusFlowTests
{
    [Fact]
    public void Missing_projection_returns_skipped_missing_observable()
    {
        var result = new PredicateDispatcher().Dispatch(
            new BinaryComparisonPredicate("p1", "followup", "source", "k_eff", "Greater"),
            BuildContext(
                BuildValidSpec(),
                new Dictionary<string, RoleOutput>
                {
                    ["source"] = new("source", new Dictionary<string, double>()),
                    ["followup"] = new("followup", new Dictionary<string, double> { ["k_eff"] = 2.0 })
                }));

        Assert.Equal(VerifyStatus.SkippedMissingObservable, result.Status);
        Assert.Null(result.Assertion);
    }

    [Fact]
    public void Invalid_spec_returns_invalid_spec_status()
    {
        var invalidSpec = new MrSpec(
            "MrSpec",
            "mr-invalid",
            "invalid",
            null,
            new Dictionary<string, string>(),
            new Dictionary<string, ParameterExpression>(),
            new Dictionary<string, RunRoleSpec>(),
            new Dictionary<string, ProjectionSpec>(),
            new PredicateSpec[] { new BinaryComparisonPredicate("p1", "followup", "source", "k_eff", "Greater") },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        var result = new PredicateDispatcher().Dispatch(
            new BinaryComparisonPredicate("p1", "followup", "source", "k_eff", "Greater"),
            BuildContext(invalidSpec, new Dictionary<string, RoleOutput>()));

        Assert.Equal(VerifyStatus.InvalidSpec, result.Status);
    }

    [Fact]
    public void False_applicability_returns_skipped_not_applicable()
    {
        var spec = BuildValidSpec() with
        {
            Applicability = new ApplicabilitySpec(
                new ConditionExpr[]
                {
                    new ComparisonConditionExpr(
                        new InputRefExpr("boron_ppm"),
                        "LessOrEqual",
                        new ConstantRefExpr(1000.0))
                })
        };

        var result = new PredicateDispatcher().Dispatch(
            new BinaryComparisonPredicate("p1", "followup", "source", "k_eff", "Greater"),
            BuildContext(
                spec,
                new Dictionary<string, RoleOutput>
                {
                    ["source"] = new("source", new Dictionary<string, double> { ["k_eff"] = 1.0 }),
                    ["followup"] = new("followup", new Dictionary<string, double> { ["k_eff"] = 2.0 })
                },
                new Dictionary<string, double> { ["boron_ppm"] = 1600.0 }));

        Assert.Equal(VerifyStatus.SkippedNotApplicable, result.Status);
    }

    private static VerificationContext BuildContext(
        MrSpec spec,
        IReadOnlyDictionary<string, RoleOutput> outputs,
        IReadOnlyDictionary<string, double>? inputs = null) =>
        new(spec, outputs, inputs);

    private static MrSpec BuildValidSpec() =>
        new(
            "MrSpec",
            "mr-flow",
            "status flow",
            null,
            new Dictionary<string, string>(),
            new Dictionary<string, ParameterExpression>(),
            new Dictionary<string, RunRoleSpec>
            {
                ["source"] = new("Baseline"),
                ["followup"] = new("Followup")
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["k_eff"] = new ScalarProjectionSpec("/results/k_eff")
            },
            new PredicateSpec[] { new BinaryComparisonPredicate("p1", "followup", "source", "k_eff", "Greater") },
            new DeterministicToleranceSpec(1e-6, 1e-4));
}
