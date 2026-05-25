using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class VarianceRatioKernelTests
{
    [Fact]
    public void VarianceRatio_passes_when_high_sample_has_lower_variance()
    {
        var result = Kernel().Evaluate(
            VarianceRatio(),
            Context(lowStdError: 0.20, highStdError: 0.10));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void VarianceRatio_fails_when_variance_does_not_reduce_enough()
    {
        var result = Kernel().Evaluate(
            VarianceRatio(),
            Context(lowStdError: 0.20, highStdError: 0.19));

        Assert.Equal(VerifyStatus.Failed, result.Status);
        Assert.Contains("ratio", result.Assertion!.FailureReason!);
    }

    private static VarianceRatioKernel Kernel() => new();

    private static VarianceRatioPredicate VarianceRatio() =>
        new("p-var", "low", "high", "k_eff_stats", new ConstantParameterExpression(2.0), new StatisticalToleranceSpec(1.0));

    private static VerificationContext Context(double lowStdError, double highStdError)
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-var",
            "variance-ratio",
            null,
            new Dictionary<string, string>(),
            new Dictionary<string, ParameterExpression>(),
            new Dictionary<string, RunRoleSpec>
            {
                ["low"] = new("Baseline"),
                ["high"] = new("Followup")
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["k_eff_stats"] = new StatisticalProjectionSpec("/k_eff/mean", "/k_eff/stderr")
            },
            new PredicateSpec[]
            {
                VarianceRatio()
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["low"] = new(
                    "low",
                    new Dictionary<string, double>(),
                    null,
                    new Dictionary<string, StatisticalValue>
                    {
                        ["k_eff_stats"] = new(1.0, lowStdError)
                    }),
                ["high"] = new(
                    "high",
                    new Dictionary<string, double>(),
                    null,
                    new Dictionary<string, StatisticalValue>
                    {
                        ["k_eff_stats"] = new(1.0, highStdError)
                    })
            });
    }
}
