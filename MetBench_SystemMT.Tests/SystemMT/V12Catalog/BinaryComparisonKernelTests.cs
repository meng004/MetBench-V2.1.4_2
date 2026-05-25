using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class BinaryComparisonKernelTests
{
    [Fact]
    public void Equal_operator_honors_tolerance()
    {
        var result = Kernel().Evaluate(Predicate("Equal"), Context(source: 100.0, followup: 100.001));

        Assert.True(result.Assertion.Passed);
        Assert.Equal(100.0, result.Diagnostic.Expected);
        Assert.Equal(100.001, result.Diagnostic.Actual);
    }

    [Fact]
    public void Greater_operator_fails_when_followup_is_not_greater()
    {
        var result = Kernel().Evaluate(Predicate("Greater"), Context(source: 100.0, followup: 99.0));

        Assert.False(result.Assertion.Passed);
        Assert.Contains("Greater", result.Assertion.FailureReason);
    }

    private static BinaryComparisonKernel Kernel() => new();

    private static BinaryComparisonPredicate Predicate(string @operator) =>
        new("p1", "followup", "source", "k_eff", @operator);

    private static VerificationContext Context(double source, double followup)
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-1",
            "test",
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
            new PredicateSpec[] { Predicate("Equal") },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["source"] = new("source", new Dictionary<string, double> { ["k_eff"] = source }),
                ["followup"] = new("followup", new Dictionary<string, double> { ["k_eff"] = followup })
            });
    }
}
