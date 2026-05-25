using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class SequenceShapeKernelTests
{
    [Fact]
    public void BellShape_passes_on_peak_in_middle()
    {
        var result = new SequenceShapeKernel().Evaluate(new BellShapeSpec(), Sequence(1.0, 3.0, 5.0, 3.0, 1.0));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void SignChange_passes_on_crossing_zero()
    {
        var result = new SequenceShapeKernel().Evaluate(new SignChangeSpec(), Sequence(2.0, 1.0, -1.0, -3.0));

        Assert.Equal(VerifyStatus.Passed, result.Status);
    }

    [Fact]
    public void ConstantSlope_reports_failure_when_differences_vary()
    {
        var result = new SequenceShapeKernel().Evaluate(new ConstantSlopeSpec(), Sequence(1.0, 2.0, 4.0, 7.0));

        Assert.Equal(VerifyStatus.Failed, result.Status);
        Assert.Contains("worst_point", result.Assertion!.FailureReason!);
    }

    private static SequenceValue Sequence(params double[] samples) => new(samples);
}
