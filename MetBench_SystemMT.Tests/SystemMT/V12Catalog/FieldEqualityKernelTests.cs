using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using MetBench_BLL.SystemMT.V12Catalog.Validation;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class FieldEqualityKernelTests
{
    [Fact]
    public void Validator_rejects_dimension_mismatch()
    {
        var spec = SpecWithFields(
            new Field2DProjectionSpec("/fields/source", 2, 2),
            new Field2DProjectionSpec("/fields/followup", 2, 3));

        var result = Validator().Validate(FieldEquality(), spec);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Path.Contains("metric", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Kernel_reports_worst_offender_location()
    {
        var result = Kernel().Evaluate(
            FieldEquality(),
            ContextWithFields(
                left: new[,]
                {
                    { 1.0, 1.0 },
                    { 1.0, 5.0 }
                },
                right: new[,]
                {
                    { 1.0, 1.0 },
                    { 1.0, 1.5 }
                }));

        Assert.Equal(VerifyStatus.Failed, result.Status);
        Assert.Contains("1", result.Assertion!.FailureReason!);
        Assert.Contains("1", result.Assertion.FailureReason!);
    }

    [Fact]
    public void Kernel_passes_when_fields_match_within_tolerance()
    {
        var result = Kernel().Evaluate(
            FieldEquality(),
            ContextWithFields(
                left: new[,]
                {
                    { 1.0, 1.0 },
                    { 1.0, 1.0005 }
                },
                right: new[,]
                {
                    { 1.0, 1.0 },
                    { 1.0, 1.0 }
                }));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    private static FieldEqualityPredicateValidator Validator() => new();

    private static FieldEqualityKernel Kernel() => new();

    private static FieldEqualityPredicate FieldEquality() =>
        new("p-field", "source", "followup", "source_flux", "followup_flux", new IdentityFieldPairing(), new FieldNormToleranceSpec("L2", 1e-6, 1e-3));

    private static MrSpec SpecWithFields(Field2DProjectionSpec left, Field2DProjectionSpec right) =>
        new(
            "MrSpec",
            "mr-field",
            "field-equality",
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
                ["source_flux"] = left,
                ["followup_flux"] = right
            },
            new PredicateSpec[]
            {
                FieldEquality()
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

    private static VerificationContext ContextWithFields(double[,] left, double[,] right)
    {
        var spec = new MrSpec(
            "MrSpec",
            "mr-field",
            "field-equality",
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
                ["source_flux"] = new Field2DProjectionSpec("/fields/source_flux", left.GetLength(0), left.GetLength(1)),
                ["followup_flux"] = new Field2DProjectionSpec("/fields/followup_flux", right.GetLength(0), right.GetLength(1))
            },
            new PredicateSpec[]
            {
                FieldEquality()
            },
            new DeterministicToleranceSpec(1e-6, 1e-4));

        return new VerificationContext(
            spec,
            new Dictionary<string, RoleOutput>
            {
                ["source"] = new("source", new Dictionary<string, double>(), new Dictionary<string, Field2DValue>
                {
                    ["source_flux"] = new(left)
                }),
                ["followup"] = new("followup", new Dictionary<string, double>(), new Dictionary<string, Field2DValue>
                {
                    ["followup_flux"] = new(right)
                })
            });
    }
}
