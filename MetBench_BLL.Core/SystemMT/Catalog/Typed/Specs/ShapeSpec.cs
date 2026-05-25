using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(BellShapeSpec), "BellShape")]
[JsonDerivedType(typeof(SShapeSpec), "SShape")]
[JsonDerivedType(typeof(SignChangeSpec), "SignChange")]
[JsonDerivedType(typeof(NonMonotonicSpec), "NonMonotonic")]
[JsonDerivedType(typeof(ConstantSlopeSpec), "ConstantSlope")]
[JsonDerivedType(typeof(ExponentialGrowthSpec), "ExponentialGrowth")]
public abstract record ShapeSpec;

public sealed record BellShapeSpec() : ShapeSpec;

public sealed record SShapeSpec() : ShapeSpec;

public sealed record SignChangeSpec() : ShapeSpec;

public sealed record NonMonotonicSpec() : ShapeSpec;

public sealed record ConstantSlopeSpec() : ShapeSpec;

public sealed record ExponentialGrowthSpec(
    ParameterExpression ExpectedRate,
    double RateTolerance,
    double ResidualRelTolerance,
    double MinRSquared) : ShapeSpec;
