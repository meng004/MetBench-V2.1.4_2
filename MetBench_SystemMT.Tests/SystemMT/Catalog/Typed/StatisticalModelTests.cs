using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class StatisticalModelTests
{
    [Fact]
    public void StatisticalTolerance_roundtrips_mean_and_std_error()
    {
        var projection = new StatisticalProjectionSpec("/mean", "/stderr");
        var tolerance = new StatisticalToleranceSpec(2.0);

        Assert.Equal("/stderr", projection.StdErrorPath);
        Assert.Equal(2.0, tolerance.SigmaMultiplier);
    }

    [Fact]
    public void StatisticalValue_keeps_mean_and_std_error()
    {
        var value = new StatisticalValue(1.23, 0.04);

        Assert.Equal(1.23, value.Mean);
        Assert.Equal(0.04, value.StdError);
    }
}
