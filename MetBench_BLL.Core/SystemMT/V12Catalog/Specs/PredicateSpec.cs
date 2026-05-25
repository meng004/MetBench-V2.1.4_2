using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.V12Catalog.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(BinaryComparisonPredicate), "BinaryComparison")]
[JsonDerivedType(typeof(ScaledEqualityPredicate), "ScaledEquality")]
[JsonDerivedType(typeof(ErrorMonotonicPredicate), "ErrorMonotonic")]
[JsonDerivedType(typeof(SubadditivePredicate), "Subadditive")]
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

public sealed record ErrorMonotonicPredicate(
    string PredicateId,
    IReadOnlyList<string> OrderedRoles,
    string ReferenceRole,
    string Metric,
    NormKind NormKind) : PredicateSpec(PredicateId);

public sealed record SubadditivePredicate(
    string PredicateId,
    double DeltaA,
    double DeltaB,
    double DeltaAB) : PredicateSpec(PredicateId);
