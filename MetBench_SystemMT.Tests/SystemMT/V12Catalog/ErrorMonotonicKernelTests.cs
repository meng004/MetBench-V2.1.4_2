using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class ErrorMonotonicKernelTests
{
    [Fact]
    public void Non_monotonic_error_reports_offending_pair()
    {
        var result = new ErrorMonotonicKernel().Evaluate(
            new ErrorMonotonicPredicate("p1", new[] { "coarse", "fine" }, "reference", "k_eff", NormKind.Absolute),
            BuildContext(reference: 1.00, coarse: 1.10, fine: 1.20));

        Assert.Equal(VerifyStatus.Failed, result.Status);
        Assert.Contains("coarse->fine", result.Assertion!.FailureReason!);
    }

    [Fact]
    public void Monotonic_error_passes()
    {
        var result = new ErrorMonotonicKernel().Evaluate(
            new ErrorMonotonicPredicate("p1", new[] { "coarse", "fine" }, "reference", "k_eff", NormKind.Absolute),
            BuildContext(reference: 1.00, coarse: 1.20, fine: 1.10));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    private static VerificationContext BuildContext(double reference, double coarse, double fine)
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-conv",
            "conv",
            null,
            new Dictionary<string, string>(),
            new Dictionary<string, ParameterExpression>(),
            new Dictionary<string, RunRoleSpec>
            {
                ["coarse"] = new("Baseline"),
                ["fine"] = new("Followup"),
                ["reference"] = new("Reference")
            },
            new Dictionary<string, ProjectionSpec>
            {
                ["k_eff"] = new ScalarProjectionSpec("/results/k_eff")
            },
            new PredicateSpec[]
            {
                new ErrorMonotonicPredicate("p1", new[] { "coarse", "fine" }, "reference", "k_eff", NormKind.Absolute)
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["coarse"] = new("coarse", new Dictionary<string, double> { ["k_eff"] = coarse }),
                ["fine"] = new("fine", new Dictionary<string, double> { ["k_eff"] = fine }),
                ["reference"] = new("reference", new Dictionary<string, double> { ["k_eff"] = reference })
            });
    }
}
