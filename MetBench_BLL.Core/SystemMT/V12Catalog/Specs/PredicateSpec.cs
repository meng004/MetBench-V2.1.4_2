using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(BinaryComparisonPredicate), "BinaryComparison")]
[JsonDerivedType(typeof(ScaledEqualityPredicate), "ScaledEquality")]
public abstract record PredicateSpec(string PredicateId);

public sealed record BinaryComparisonPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string Metric,
    string Operator) : PredicateSpec(PredicateId);

public sealed record ScaledEqualityPredicate(
    string PredicateId,
    string ActualRole,
    string ReferenceRole,
    string Metric,
    ParameterExpression Factor,
    double Exponent) : PredicateSpec(PredicateId);
