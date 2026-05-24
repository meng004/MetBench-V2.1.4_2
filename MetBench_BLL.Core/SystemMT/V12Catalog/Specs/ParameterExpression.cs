using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ConstantParameterExpression), "ConstantParameter")]
public abstract record ParameterExpression;

public sealed record ConstantParameterExpression(
    double Value) : ParameterExpression;
