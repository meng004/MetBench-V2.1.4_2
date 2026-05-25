using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class CrossMethodComparisonTests
{
    [Fact]
    public void Validator_rejects_missing_method_binding()
    {
        var result = Validator().Validate(CrossMethod(), SpecWithoutMethodBinding());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Path.Contains("method", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Kernel_compares_metric_across_methods()
    {
        var result = Kernel().Evaluate(
            CrossMethod(),
            ContextWithMethods(1.00, 1.0005));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    private static CrossMethodPredicateValidator Validator() => new(new SharedReferenceResolver());

    private static CrossMethodComparisonKernel Kernel() => new();

    private static CrossMethodComparisonPredicate CrossMethod() =>
        new("p-cross", "p0", "p1", "k_eff", "Equal", new DeterministicToleranceSpec(1e-6, 1e-3));

    private static MrSpec SpecWithoutMethodBinding() =>
        new(
            "MrSpec",
            "mr-cross",
            "cross-method",
            null,
            new Dictionary<string, string>(),
            new Dictionary<string, ParameterExpression>(),
            new Dictionary<string, RunRoleSpec>
            {
                ["p0"] = new("Baseline"),
                ["p1"] = new("Followup")
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["k_eff"] = new ScalarProjectionSpec("/results/k_eff")
            },
            new PredicateSpec[]
            {
                CrossMethod()
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

    private static VerificationContext ContextWithMethods(double p0, double p1)
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-cross",
            "cross-method",
            null,
            new Dictionary<string, string>(),
            new Dictionary<string, ParameterExpression>(),
            new Dictionary<string, RunRoleSpec>
            {
                ["p0"] = new("Baseline") { MethodBinding = new MethodBinding("OpenMOC") },
                ["p1"] = new("Followup") { MethodBinding = new MethodBinding("OpenMC") }
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["k_eff"] = new ScalarProjectionSpec("/results/k_eff")
            },
            new PredicateSpec[]
            {
                CrossMethod()
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["p0"] = new("p0", new Dictionary<string, double> { ["k_eff"] = p0 }),
                ["p1"] = new("p1", new Dictionary<string, double> { ["k_eff"] = p1 })
            });
    }
}
