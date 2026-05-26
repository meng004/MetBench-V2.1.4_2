using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class PredicateDispatcher : IPredicateDispatcher
{
    private readonly ApplicabilityEvaluator _applicability = new();
    private readonly BinaryComparisonKernel _binary = new();
    private readonly NoiseAwareBinaryComparisonKernel _noiseAwareBinary = new();
    private readonly ScaledEqualityKernel _scaled = new();
    private readonly CrossMethodComparisonKernel _crossMethod = new();
    private readonly ErrorMonotonicKernel _errorMonotonic = new();
    private readonly OrderedSequenceShapeKernel _orderedSequenceShape = new();
    private readonly VarianceRatioKernel _varianceRatio = new();
    private readonly SubadditiveKernel _subadditive = new();
    private readonly FieldEqualityKernel _fieldEquality = new();
    private readonly FieldProportionalityKernel _fieldProportionality = new();
    private readonly DerivedInvariantKernel _derivedInvariant = new();

    public VerificationResult Dispatch(PredicateSpec predicate, VerificationContext context)
    {
        if (!context.SpecValidation.IsValid)
        {
            return VerificationResult.InvalidSpec("Spec validation failed before runtime dispatch.");
        }

        var applicability = _applicability.Evaluate(context.Spec.Applicability, context.Inputs);
        if (!applicability.ShouldRun)
        {
            return VerificationResult.SkippedNotApplicable(applicability.Reason ?? "Applicability conditions were not satisfied.");
        }

        if (!HasObservable(predicate, context))
        {
            return VerificationResult.SkippedMissingObservable("Required observable is missing from role outputs.");
        }

        return predicate switch
        {
            BinaryComparisonPredicate binary => _binary.Evaluate(binary, context),
            NoiseAwareBinaryComparisonPredicate noiseAwareBinary => _noiseAwareBinary.Evaluate(noiseAwareBinary, context),
            ScaledEqualityPredicate scaled => _scaled.Evaluate(scaled, context),
            CrossMethodComparisonPredicate crossMethod => _crossMethod.Evaluate(crossMethod, context),
            ErrorMonotonicPredicate monotonic => _errorMonotonic.Evaluate(monotonic, context),
            OrderedSequenceShapePredicate orderedShape => _orderedSequenceShape.Evaluate(orderedShape, context),
            VarianceRatioPredicate varianceRatio => _varianceRatio.Evaluate(varianceRatio, context),
            SubadditivePredicate subadditive => _subadditive.Evaluate(subadditive, context),
            FieldEqualityPredicate field => _fieldEquality.Evaluate(field, context),
            FieldProportionalityPredicate fieldProportionality => _fieldProportionality.Evaluate(fieldProportionality, context),
            DerivedInvariantPredicate derived => _derivedInvariant.Evaluate(derived, context),
            _ => throw new ArgumentException($"Unsupported predicate type '{predicate.GetType().Name}'.", nameof(predicate))
        };
    }

    private static bool HasObservable(PredicateSpec predicate, VerificationContext context) =>
        predicate switch
        {
            BinaryComparisonPredicate binary =>
                context.TryGetMetric(binary.LeftRole, binary.Metric, out _) &&
                context.TryGetMetric(binary.RightRole, binary.Metric, out _),
            NoiseAwareBinaryComparisonPredicate noiseAwareBinary =>
                context.TryGetMetric(noiseAwareBinary.LeftRole, noiseAwareBinary.Metric, out _) &&
                context.TryGetMetric(noiseAwareBinary.RightRole, noiseAwareBinary.Metric, out _) &&
                context.TryGetMetric(noiseAwareBinary.LeftRole, noiseAwareBinary.SourceStdMetric, out _) &&
                context.TryGetMetric(noiseAwareBinary.RightRole, noiseAwareBinary.FollowupStdMetric, out _),
            ScaledEqualityPredicate scaled =>
                context.TryGetMetric(scaled.ActualRole, scaled.Metric, out _) &&
                context.TryGetMetric(scaled.ReferenceRole, scaled.Metric, out _),
            CrossMethodComparisonPredicate crossMethod =>
                context.TryGetMetric(crossMethod.LeftRole, crossMethod.Metric, out _) &&
                context.TryGetMetric(crossMethod.RightRole, crossMethod.Metric, out _),
            ErrorMonotonicPredicate monotonic =>
                context.TryGetMetric(monotonic.ReferenceRole, monotonic.Metric, out _) &&
                HasAllOrderedRoleMetrics(monotonic, context),
            OrderedSequenceShapePredicate orderedShape =>
                HasAllOrderedRoleMetrics(orderedShape.OrderedRoles, orderedShape.Metric, context),
            VarianceRatioPredicate varianceRatio =>
                context.TryGetStatistical(varianceRatio.LowSampleRole, varianceRatio.StatisticalMetric, out _) &&
                context.TryGetStatistical(varianceRatio.HighSampleRole, varianceRatio.StatisticalMetric, out _),
            SubadditivePredicate => true,
            FieldEqualityPredicate field =>
                context.TryGetField(field.LeftRole, field.LeftMetric, out _) &&
                context.TryGetField(field.RightRole, field.RightMetric, out _),
            FieldProportionalityPredicate fieldProportionality =>
                context.TryGetField(fieldProportionality.LeftRole, fieldProportionality.LeftMetric, out _) &&
                context.TryGetField(fieldProportionality.RightRole, fieldProportionality.RightMetric, out _),
            DerivedInvariantPredicate derived =>
                context.TryGetField(derived.LeftRole, derived.LeftMetric, out _) &&
                context.TryGetField(derived.RightRole, derived.RightMetric, out _),
            _ => true
        };

    private static bool HasAllOrderedRoleMetrics(ErrorMonotonicPredicate monotonic, VerificationContext context)
    {
        return HasAllOrderedRoleMetrics(monotonic.OrderedRoles, monotonic.Metric, context);
    }

    private static bool HasAllOrderedRoleMetrics(
        IReadOnlyList<string> orderedRoles,
        string metric,
        VerificationContext context)
    {
        foreach (var role in orderedRoles)
        {
            if (!context.TryGetMetric(role, metric, out _))
            {
                return false;
            }
        }

        return true;
    }
}
