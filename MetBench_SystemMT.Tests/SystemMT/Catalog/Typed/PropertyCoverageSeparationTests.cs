using MetBench_BLL.SystemMT.Catalog.Typed.Property;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class PropertyCoverageSeparationTests
{
    [Fact]
    public void Property_catalog_entries_are_not_counted_as_mr_entries()
    {
        var report = PropertyCoverageSnapshot.Build(mrCount: 43, propertyCount: 4);

        Assert.Equal(43, report.MrCount);
        Assert.Equal(4, report.PropertyCount);
    }
}
