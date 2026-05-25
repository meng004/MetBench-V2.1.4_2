using MetBench_BLL.SystemMT.V12Catalog.Serialization;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.V12Catalog;

public sealed class SequenceShapeModelTests
{
    [Theory]
    [InlineData("BellShape")]
    [InlineData("SShape")]
    [InlineData("SignChange")]
    [InlineData("NonMonotonic")]
    [InlineData("ConstantSlope")]
    public void ShapeSpec_roundtrips(string kind)
    {
        var yaml =
$"""
kind: PropertySpec
property_id: shape-smoke
name: shape-smoke
assertions:
  - kind: ShapeProperty
    predicate_id: shape-check
    metric: sequence_metric
    shape:
      kind: {kind}
""";

        var spec = V12CatalogSerializer.DeserializePropertySpec(yaml);

        var predicate = Assert.IsType<ShapePropertyPredicate>(spec.Assertions[0]);
        Assert.Equal(kind, predicate.Shape.GetType().Name.Replace("Spec", string.Empty));
    }
}
