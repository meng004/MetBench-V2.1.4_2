using MetBench_BLL.SystemMT.V12Catalog.Derived;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class DerivedExpressionTests
{
    [Fact]
    public void Finite_difference_returns_adjacent_deltas()
    {
        var values = FiniteDifferenceDerivedExpression.Compute(new[] { 1.0, 4.0, 9.0 });

        Assert.Equal(new[] { 3.0, 5.0 }, values);
    }

    [Fact]
    public void Coefficient_of_variation_returns_sigma_over_mean()
    {
        var value = CoefficientOfVariation.Compute(new[] { 2.0, 4.0, 6.0 });

        Assert.True(value > 0.0);
        Assert.InRange(value, 0.40, 0.41);
    }
}
