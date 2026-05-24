using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ScalarProjectionSpec), "ScalarProjection")]
public abstract record ProjectionSpec;

public sealed record ScalarProjectionSpec(
    string Path) : ProjectionSpec;
