using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Property;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class BoundPropertyCheckerTests
{
    [Fact]
    public void Range_property_supports_two_bound_assertions()
    {
        var result = Checker().Check(RangeProperty(), ContextWithScalar("dancoff", 0.42));

        Assert.True(result.Passed);
        Assert.Equal(2, result.PredicateResults.Count);
    }

    private static BoundPropertyChecker Checker() => new();

    private static PropertySpec RangeProperty() =>
        new(
            "PropertySpec",
            "res-alg-03",
            "range",
            null,
            new Dictionary<string, string>(),
            new PropertyCaseSpec("ScalarCase"),
            new Dictionary<string, ProjectionSpec>
            {
                ["dancoff"] = new ScalarProjectionSpec("/results/dancoff")
            },
            new PropertyPredicateSpec[]
            {
                new BoundPropertyPredicate("lower", "dancoff", "Greater", new ConstantParameterExpression(0.0)),
                new BoundPropertyPredicate("upper", "dancoff", "Less", new ConstantParameterExpression(1.0))
            },
            new DeterministicToleranceSpec(1e-6, 1e-6));

    private static PropertyVerificationContext ContextWithScalar(string metric, double value) =>
        new(
            RangeProperty(),
            new Dictionary<string, double>
            {
                [metric] = value
            });
}
