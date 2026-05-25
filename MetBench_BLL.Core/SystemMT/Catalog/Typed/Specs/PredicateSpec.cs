using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Specs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(BinaryComparisonPredicate), "BinaryComparison")]
[JsonDerivedType(typeof(ScaledEqualityPredicate), "ScaledEquality")]
[JsonDerivedType(typeof(CrossMethodComparisonPredicate), "CrossMethodComparison")]
[JsonDerivedType(typeof(ErrorMonotonicPredicate), "ErrorMonotonic")]
[JsonDerivedType(typeof(OrderedSequenceShapePredicate), "OrderedSequenceShape")]
[JsonDerivedType(typeof(VarianceRatioPredicate), "VarianceRatio")]
[JsonDerivedType(typeof(SubadditivePredicate), "Subadditive")]
[JsonDerivedType(typeof(FieldEqualityPredicate), "FieldEquality")]
[JsonDerivedType(typeof(FieldProportionalityPredicate), "FieldProportionality")]
[JsonDerivedType(typeof(DerivedInvariantPredicate), "DerivedInvariant")]
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

public sealed record CrossMethodComparisonPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string Metric,
    string Operator,
    DeterministicToleranceSpec Tolerance) : PredicateSpec(PredicateId);

public sealed record ErrorMonotonicPredicate(
    string PredicateId,
    IReadOnlyList<string> OrderedRoles,
    string ReferenceRole,
    string Metric,
    NormKind NormKind) : PredicateSpec(PredicateId);

public sealed record OrderedSequenceShapePredicate(
    string PredicateId,
    IReadOnlyList<string> OrderedRoles,
    string Metric,
    ShapeSpec Shape) : PredicateSpec(PredicateId);

public sealed record VarianceRatioPredicate(
    string PredicateId,
    string LowSampleRole,
    string HighSampleRole,
    string StatisticalMetric,
    ParameterExpression SampleRatio,
    StatisticalToleranceSpec Tolerance) : PredicateSpec(PredicateId);

public sealed record SubadditivePredicate(
    string PredicateId,
    double DeltaA,
    double DeltaB,
    double DeltaAB) : PredicateSpec(PredicateId);

public sealed record FieldEqualityPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string LeftMetric,
    string RightMetric,
    FieldPairing Pairing,
    FieldNormToleranceSpec Tolerance) : PredicateSpec(PredicateId);

public enum ConstantEstimator
{
    LeastSquaresThroughOrigin,
    MedianRatio
}

public sealed record FieldProportionalityPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string LeftMetric,
    string RightMetric,
    ConstantEstimator Estimator,
    FieldNormToleranceSpec Tolerance) : PredicateSpec(PredicateId);

public sealed record DerivedInvariantPredicate(
    string PredicateId,
    string LeftRole,
    string RightRole,
    string LeftMetric,
    string RightMetric,
    string DerivedMetric,
    DeterministicToleranceSpec Tolerance) : PredicateSpec(PredicateId);
