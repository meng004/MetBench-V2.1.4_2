using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ScalarProjectionSpec), "ScalarProjection")]
public abstract record ProjectionSpec(string Kind);

public sealed record ScalarProjectionSpec(
    string Kind,
    string Path) : ProjectionSpec(Kind);
