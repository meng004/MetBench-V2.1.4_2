using System;
using MetBench_BLL.SystemMT.Catalog.Typed.Property;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class ExponentialGrowthEvaluatorTests
{
    [Fact]
    public void Perfect_exponential_passes()
    {
        var result = Checker().Check(
            Property(),
            new PropertyVerificationContext(
                Property(),
                sequences: new System.Collections.Generic.Dictionary<string, SequenceValue>
                {
                    ["power_history"] = Sequence((0.0, 1.0), (1.0, 2.0), (2.0, 4.0))
                }));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Non_positive_sequence_fails_with_clear_diagnostic()
    {
        var result = Checker().Check(
            Property(),
            new PropertyVerificationContext(
                Property(),
                sequences: new System.Collections.Generic.Dictionary<string, SequenceValue>
                {
                    ["power_history"] = Sequence((0.0, 1.0), (1.0, 0.0), (2.0, 4.0))
                }));

        Assert.False(result.Passed);
        Assert.Contains("positive", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    private static PropertyChecker Checker() => new();

    private static PropertySpec Property() =>
        new(
            "PropertySpec",
            "kin-phy-02",
            "exp-growth",
            null,
            new System.Collections.Generic.Dictionary<string, string>(),
            new PropertyCaseSpec("Base"),
            new System.Collections.Generic.Dictionary<string, ProjectionSpec>
            {
                ["power_history"] = new SequenceProjectionSpec("/results/power_history")
            },
            new PropertyPredicateSpec[]
            {
                new ShapePropertyPredicate(
                    "exp-growth",
                    "power_history",
                    new ExponentialGrowthSpec(new ConstantParameterExpression(0.69314718056), 0.05, 1e-6, 0.999))
            },
            new DeterministicToleranceSpec(0.0, 0.0));

    private static SequenceValue Sequence(params (double X, double Y)[] points)
    {
        var x = new double[points.Length];
        var y = new double[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            x[i] = points[i].X;
            y[i] = points[i].Y;
        }

        return new SequenceValue(y, x);
    }
}
