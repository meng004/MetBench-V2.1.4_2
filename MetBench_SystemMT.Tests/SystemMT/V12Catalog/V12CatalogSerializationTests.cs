using System;
using System.IO;
using MetBench_BLL.SystemMT.V12Catalog.Schema;
using MetBench_BLL.SystemMT.V12Catalog.Serialization;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class V12CatalogSerializationTests
{
    private static string Asset(string relativePath) =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "V12Catalog", relativePath);

    [Fact]
    public void Mr_yaml_deserializes_into_typed_MrSpec()
    {
        var yaml = File.ReadAllText(Asset(Path.Combine("samples", "mr-sample.yaml")));

        var spec = V12CatalogSerializer.DeserializeMrSpec(yaml);

        Assert.Equal("MrSpec", spec.Kind);
        Assert.Equal("dif-phy-doppler-negative", spec.MrId);
        Assert.Single(spec.Predicates);
        Assert.IsType<BinaryComparisonPredicate>(spec.Predicates[0]);
    }

    [Fact]
    public void Property_yaml_deserializes_into_typed_PropertySpec()
    {
        var yaml = File.ReadAllText(Asset(Path.Combine("samples", "property-sample.yaml")));

        var spec = V12CatalogSerializer.DeserializePropertySpec(yaml);

        Assert.Equal("PropertySpec", spec.Kind);
        Assert.Equal("res-alg-dancoff-range", spec.PropertyId);
        Assert.Equal(2, spec.Assertions.Count);
        Assert.All(spec.Assertions, a => Assert.IsType<BoundPropertyPredicate>(a));
    }

    [Fact]
    public void Missing_root_kind_is_rejected()
    {
        var yaml = File.ReadAllText(Asset(Path.Combine("invalid", "missing-root-kind.yaml")));

        var ex = Assert.Throws<V12CatalogSchemaException>(() => V12CatalogSerializer.DeserializeMrSpec(yaml));

        Assert.Contains("kind", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_nested_predicate_kind_is_rejected()
    {
        var yaml = File.ReadAllText(Asset(Path.Combine("invalid", "missing-predicate-kind.yaml")));

        var ex = Assert.Throws<V12CatalogSchemaException>(() => V12CatalogSerializer.DeserializeMrSpec(yaml));

        Assert.Contains("predicates[0]", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kind", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
