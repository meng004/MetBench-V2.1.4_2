using System;
using MetBench_BLL.SystemMT.Catalog.Typed.Property;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class PropertyRuntimeContractTests
{
    [Fact]
    public void PropertyStatus_is_separate_from_mr_status()
    {
        Assert.DoesNotContain("SkippedNotApplicable", Enum.GetNames<PropertyStatus>());
    }
}
