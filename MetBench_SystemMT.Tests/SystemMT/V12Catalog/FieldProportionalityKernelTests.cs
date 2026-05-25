using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class FieldProportionalityKernelTests
{
    [Fact]
    public void FieldProportionality_estimates_constant_ratio()
    {
        var result = Kernel().Evaluate(
            FieldProportionality(),
            Context(
                source: new[,] { { 1.0, 2.0 } },
                followup: new[,] { { 2.0, 4.0 } }));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void FieldProportionality_fails_when_ratio_is_not_constant()
    {
        var result = Kernel().Evaluate(
            FieldProportionality(),
            Context(
                source: new[,] { { 1.0, 2.0 } },
                followup: new[,] { { 2.0, 5.0 } }));

        Assert.Equal(VerifyStatus.Failed, result.Status);
        Assert.Contains("ratio", result.Assertion!.FailureReason!);
    }

    private static FieldProportionalityKernel Kernel() => new();

    private static FieldProportionalityPredicate FieldProportionality() =>
        new("p-prop", "source", "followup", "source_flux", "followup_flux", ConstantEstimator.LeastSquaresThroughOrigin, new FieldNormToleranceSpec("L2", 1e-6, 1e-3));

    private static VerificationContext Context(double[,] source, double[,] followup)
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-prop",
            "field-proportionality",
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
                ["source_flux"] = new Field2DProjectionSpec("/fields/source_flux", source.GetLength(0), source.GetLength(1)),
                ["followup_flux"] = new Field2DProjectionSpec("/fields/followup_flux", followup.GetLength(0), followup.GetLength(1))
            },
            new PredicateSpec[]
            {
                FieldProportionality()
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["source"] = new("source", new Dictionary<string, double>(), new Dictionary<string, Field2DValue>
                {
                    ["source_flux"] = new(source)
                }),
                ["followup"] = new("followup", new Dictionary<string, double>(), new Dictionary<string, Field2DValue>
                {
                    ["followup_flux"] = new(followup)
                })
            });
    }
}
