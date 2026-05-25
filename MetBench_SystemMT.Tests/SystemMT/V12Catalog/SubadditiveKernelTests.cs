using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class SubadditiveKernelTests
{
    [Fact]
    public void Subadditive_passes_when_combined_effect_is_smaller()
    {
        var result = new SubadditiveKernel().Evaluate(
            new SubadditivePredicate("p1", 10.0, 6.0, 13.0),
            null);

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void Subadditive_fails_when_combined_effect_is_larger()
    {
        var result = new SubadditiveKernel().Evaluate(
            new SubadditivePredicate("p1", 10.0, 6.0, 18.0),
            null);

        Assert.Equal(VerifyStatus.Failed, result.Status);
    }
}
