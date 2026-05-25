using System.Collections.Generic;
using System.IO;
using MetBench_BLL.SystemMT.V12Catalog.Property;
using MetBench_BLL.SystemMT.V12Catalog.Runtime;
using MetBench_BLL.SystemMT.V12Catalog.Serialization;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class ExecutablePropertyFixturesTests
{
    [Fact]
    public void ResAlg03_range_property_executes()
    {
        var spec = Load("res-alg-03.yaml");

        var result = new PropertyChecker().Check(
            spec,
            new PropertyVerificationContext(
                spec,
                new Dictionary<string, double>
                {
                    ["dancoff"] = 0.42
                }));

        Assert.True(result.Passed);
    }

    [Fact]
    public void DifPhy08_bound_property_executes()
    {
        var spec = Load("dif-phy-08.yaml");

        var result = new PropertyChecker().Check(
            spec,
            new PropertyVerificationContext(
                spec,
                new Dictionary<string, double>
                {
                    ["edge_minus_center"] = -0.12
                }));

        Assert.True(result.Passed);
    }

    [Fact]
    public void BurPhy04_shape_property_executes()
    {
        var spec = Load("bur-phy-04.yaml");

        var result = new PropertyChecker().Check(
            spec,
            new PropertyVerificationContext(
                spec,
                sequences: new Dictionary<string, SequenceValue>
                {
                    ["iodine_pit"] = new(new[] { 0.0, 3.0, 5.0, 8.0, 12.0 })
                }));

        Assert.True(result.Passed);
    }

    [Fact]
    public void KinPhy02_is_schema_valid_but_not_executable_yet()
    {
        var spec = Load("kin-phy-02.yaml");

        Assert.True(spec.Validate().IsValid);

        var result = new PropertyChecker().Check(
            spec,
            new PropertyVerificationContext(
                spec,
                sequences: new Dictionary<string, SequenceValue>
                {
                    ["power_history"] = new(new[] { 1.0, 1.2, 1.5, 1.9 })
                }));

        Assert.Equal(PropertyStatus.InvalidSpec, result.Status);
    }

    private static PropertySpec Load(string fileName) =>
        V12CatalogSerializer.DeserializePropertySpec(File.ReadAllText(Path.Combine(PropertyRoot(), fileName)));

    private static string PropertyRoot() =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "V12Catalog", "property");
}
