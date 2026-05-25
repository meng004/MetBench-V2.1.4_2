using System.Collections.Generic;
using System.IO;
using MetBench_BLL.SystemMT.Catalog.Typed.Property;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Serialization;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Typed;

public sealed class KinPhy02ExecutableTests
{
    [Fact]
    public void KinPhy02_is_executable_after_pr9()
    {
        var spec = Load();

        var result = new PropertyChecker().Check(
            spec,
            new PropertyVerificationContext(
                spec,
                sequences: new Dictionary<string, SequenceValue>
                {
                    ["power_history"] = new(new[] { 1.0, 2.0, 4.1 }, new[] { 0.0, 1.0, 2.0 })
                }));

        Assert.NotEqual(PropertyStatus.InvalidSpec, result.Status);
        Assert.True(result.Passed);
    }

    private static PropertySpec Load() =>
        TypedCatalogSerializer.DeserializePropertySpec(File.ReadAllText(Path.Combine(PropertyRoot(), "kin-phy-02.yaml")));

    private static string PropertyRoot() =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "SystemMT", "Catalog", "Typed", "property");
}
