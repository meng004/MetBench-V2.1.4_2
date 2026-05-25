using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ScalarProjectionSpec), "ScalarProjection")]
[JsonDerivedType(typeof(SequenceProjectionSpec), "SequenceProjection")]
public abstract record ProjectionSpec;

public sealed record ScalarProjectionSpec(
    string Path) : ProjectionSpec;

public sealed record SequenceProjectionSpec(
    string Path) : ProjectionSpec;
