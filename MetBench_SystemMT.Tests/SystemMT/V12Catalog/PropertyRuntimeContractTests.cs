using System;
using MetBench_BLL.SystemMT.V12Catalog.Property;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class PropertyRuntimeContractTests
{
    [Fact]
    public void PropertyStatus_is_separate_from_mr_status()
    {
        Assert.DoesNotContain("SkippedNotApplicable", Enum.GetNames<PropertyStatus>());
    }
}
