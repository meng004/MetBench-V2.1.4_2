using System;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class ApplicabilityModelTests
{
    [Fact]
    public void VerifyStatus_contains_five_states()
    {
        var values = Enum.GetValues<VerifyStatus>();

        Assert.Contains(VerifyStatus.Passed, values);
        Assert.Contains(VerifyStatus.Failed, values);
        Assert.Contains(VerifyStatus.SkippedNotApplicable, values);
        Assert.Contains(VerifyStatus.SkippedMissingObservable, values);
        Assert.Contains(VerifyStatus.InvalidSpec, values);
    }
}
