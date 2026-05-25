using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(BellShapeSpec), "BellShape")]
[JsonDerivedType(typeof(SShapeSpec), "SShape")]
[JsonDerivedType(typeof(SignChangeSpec), "SignChange")]
[JsonDerivedType(typeof(NonMonotonicSpec), "NonMonotonic")]
[JsonDerivedType(typeof(ConstantSlopeSpec), "ConstantSlope")]
public abstract record ShapeSpec;

public sealed record BellShapeSpec() : ShapeSpec;

public sealed record SShapeSpec() : ShapeSpec;

public sealed record SignChangeSpec() : ShapeSpec;

public sealed record NonMonotonicSpec() : ShapeSpec;

public sealed record ConstantSlopeSpec() : ShapeSpec;
