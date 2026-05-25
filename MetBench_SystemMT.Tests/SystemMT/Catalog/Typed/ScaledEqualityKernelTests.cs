using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class ScaledEqualityKernelTests
{
    [Fact]
    public void ScaledEquality_uses_factor_power_reference()
    {
        var result = Kernel().Evaluate(
            new ScaledEqualityPredicate("p1", "followup", "source", "phi_forward", new ConstantParameterExpression(2.0), 1.0),
            Context(reference: 10.0, actual: 20.0));

        Assert.True(result.Assertion.Passed);
        Assert.Equal(20.0, result.Diagnostic.Expected);
    }

    [Fact]
    public void ScaledEquality_reports_residual_on_failure()
    {
        var result = Kernel().Evaluate(
            new ScaledEqualityPredicate("p1", "followup", "source", "phi_forward", new ConstantParameterExpression(2.0), 1.0),
            Context(reference: 10.0, actual: 18.0));

        Assert.False(result.Assertion.Passed);
        Assert.Contains("residual", result.Assertion.FailureReason!, System.StringComparison.OrdinalIgnoreCase);
    }

    private static ScaledEqualityKernel Kernel() => new();

    private static VerificationContext Context(double reference, double actual)
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-2",
            "scaled",
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
                ["phi_forward"] = new ScalarProjectionSpec("/results/phi_forward")
            },
            new PredicateSpec[]
            {
                new ScaledEqualityPredicate("p1", "followup", "source", "phi_forward", new ConstantParameterExpression(2.0), 1.0)
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["source"] = new("source", new Dictionary<string, double> { ["phi_forward"] = reference }),
                ["followup"] = new("followup", new Dictionary<string, double> { ["phi_forward"] = actual })
            });
    }
}
