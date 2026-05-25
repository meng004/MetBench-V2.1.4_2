using MetBench_BLL.SystemMT.V12Catalog.Property;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

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
