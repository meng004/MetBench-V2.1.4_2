using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Property;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class ShapePropertyCheckerTests
{
    [Fact]
    public void Shape_property_reuses_sequence_shape_evaluator()
    {
        var result = Checker().Check(BellProperty(), ContextWithSequence("curve", 1.0, 3.0, 5.0, 3.0, 1.0));

        Assert.True(result.Passed);
        Assert.Single(result.PredicateResults);
    }

    private static ShapePropertyChecker Checker() => new();

    private static PropertySpec BellProperty() =>
        new(
            "PropertySpec",
            "dif-phy-08",
            "bell-shape",
            null,
            new Dictionary<string, string>(),
            new PropertyCaseSpec("SequenceCase"),
            new Dictionary<string, ProjectionSpec>
            {
                ["curve"] = new SequenceProjectionSpec("/results/curve")
            },
            new PropertyPredicateSpec[]
            {
                new ShapePropertyPredicate("shape", "curve", new BellShapeSpec())
            },
            new DeterministicToleranceSpec(1e-6, 1e-6));

    private static PropertyVerificationContext ContextWithSequence(string metric, params double[] values) =>
        new(
            BellProperty(),
            sequences: new Dictionary<string, SequenceValue>
            {
                [metric] = new(values)
            });
}
