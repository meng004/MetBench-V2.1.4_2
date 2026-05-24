using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(BoundPropertyPredicate), "BoundProperty")]
public abstract record PropertyPredicateSpec(string Kind, string PredicateId);

public sealed record BoundPropertyPredicate(
    string Kind,
    string PredicateId,
    string Metric,
    string Operator,
    ParameterExpression Bound) : PropertyPredicateSpec(Kind, PredicateId);
