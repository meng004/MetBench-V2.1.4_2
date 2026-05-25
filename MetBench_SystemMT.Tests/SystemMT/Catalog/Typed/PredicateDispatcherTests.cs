using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class PredicateDispatcherTests
{
    [Fact]
    public void Dispatcher_routes_binary_and_scaled_predicates()
    {
        var context = BuildContext();
        var dispatcher = new PredicateDispatcher();

        var binary = dispatcher.Dispatch(
            new BinaryComparisonPredicate("p1", "followup", "source", "k_eff", "Greater"),
            context);
        var scaled = dispatcher.Dispatch(
            new ScaledEqualityPredicate("p2", "followup", "source", "phi_forward", new ConstantParameterExpression(2.0), 1.0),
            context);

        Assert.True(binary.Assertion.Passed);
        Assert.True(scaled.Assertion.Passed);
    }

    private static VerificationContext BuildContext()
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-3",
            "dispatch",
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
                ["k_eff"] = new ScalarProjectionSpec("/results/k_eff"),
                ["phi_forward"] = new ScalarProjectionSpec("/results/phi_forward")
            },
            new PredicateSpec[]
            {
                new BinaryComparisonPredicate("p1", "followup", "source", "k_eff", "Greater")
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["source"] = new("source", new Dictionary<string, double> { ["k_eff"] = 1.0, ["phi_forward"] = 10.0 }),
                ["followup"] = new("followup", new Dictionary<string, double> { ["k_eff"] = 2.0, ["phi_forward"] = 20.0 })
            });
    }
}
