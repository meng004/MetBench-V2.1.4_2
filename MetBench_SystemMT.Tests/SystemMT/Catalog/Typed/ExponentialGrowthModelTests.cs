using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class ExponentialGrowthModelTests
{
    [Fact]
    public void ExponentialGrowthSpec_roundtrips_expected_rate_and_thresholds()
    {
        var spec = new ExponentialGrowthSpec(new ConstantParameterExpression(0.2), 0.05, 1e-3, 0.99);

        var expectedRate = Assert.IsType<ConstantParameterExpression>(spec.ExpectedRate);
        Assert.Equal(0.2, expectedRate.Value);
        Assert.Equal(0.05, spec.RateTolerance);
        Assert.Equal(1e-3, spec.ResidualRelTolerance);
        Assert.Equal(0.99, spec.MinRSquared);
    }

    [Fact]
    public void LogLinearFit_recovers_slope_for_perfect_exponential()
    {
        var fit = LogLinearFit.Compute(
            x: new[] { 0.0, 1.0, 2.0 },
            y: new[] { 1.0, 2.0, 4.0 });

        Assert.Equal(0.0, fit.Intercept, 6);
        Assert.Equal(0.693147, fit.Slope, 6);
        Assert.Equal(1.0, fit.RSquared, 6);
    }
}
