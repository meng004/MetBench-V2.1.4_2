using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Derived;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class DerivedInvariantKernelTests
{
    [Fact]
    public void Mass_invariant_passes_when_sum_constant()
    {
        var result = Kernel().Evaluate(
            MassInvariant(),
            ContextWithFields(
                source: new[,]
                {
                    { 2.0, 3.0 }
                },
                followup: new[,]
                {
                    { 1.0, 4.0 }
                }));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void Mass_invariant_fails_when_sum_changes()
    {
        var result = Kernel().Evaluate(
            MassInvariant(),
            ContextWithFields(
                source: new[,]
                {
                    { 2.0, 3.0 }
                },
                followup: new[,]
                {
                    { 1.0, 3.0 }
                }));

        Assert.Equal(VerifyStatus.Failed, result.Status);
        Assert.Contains("mass_number_sum", result.Assertion!.FailureReason!);
    }

    [Fact]
    public void Derived_operators_compute_expected_values()
    {
        var field = new Field2DValue(new[,]
        {
            { 1.0, 2.0 },
            { 3.0, 4.0 }
        });

        Assert.Equal(10.0, MassNumberSum.Compute(field.Values));
        Assert.Equal(2.5, FieldRegionMean.Compute(field));
        Assert.Equal(5.0, ScalarSubtract.Compute(7.0, 2.0));
        Assert.Equal(5.477225575051661, L2Norm.Compute(field), 12);
        Assert.Equal(4.0, LinfNorm.Compute(field));
    }

    private static DerivedInvariantKernel Kernel() => new();

    private static DerivedInvariantPredicate MassInvariant() =>
        new("p-derived", "source", "followup", "isotopes", "isotopes", "mass_number_sum", new DeterministicToleranceSpec(1e-6, 1e-6));

    private static VerificationContext ContextWithFields(double[,] source, double[,] followup)
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-derived",
            "derived-invariant",
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
                ["isotopes"] = new Field2DProjectionSpec("/fields/isotopes", source.GetLength(0), source.GetLength(1))
            },
            new PredicateSpec[]
            {
                MassInvariant()
            },
            new DeterministicToleranceSpec(1e-6, 1e-6));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["source"] = new("source", new Dictionary<string, double>(), new Dictionary<string, Field2DValue>
                {
                    ["isotopes"] = new(source)
                }),
                ["followup"] = new("followup", new Dictionary<string, double>(), new Dictionary<string, Field2DValue>
                {
                    ["isotopes"] = new(followup)
                })
            });
    }
}
