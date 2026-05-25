using System;
using MetBench_BLL.SystemMT.V12Catalog.Specs;
using MetBench_BLL.SystemMT.V12Catalog.Validation;

namespace MetBench_BLL.SystemMT.V12Catalog.Runtime;

public sealed class PredicateDispatcher : IPredicateDispatcher
{
    private readonly ApplicabilityEvaluator _applicability = new();
    private readonly BinaryComparisonKernel _binary = new();
    private readonly ScaledEqualityKernel _scaled = new();
    private readonly ErrorMonotonicKernel _errorMonotonic = new();
    private readonly FieldEqualityKernel _fieldEquality = new();
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
            ScaledEqualityPredicate scaled => _scaled.Evaluate(scaled, context),
            ErrorMonotonicPredicate monotonic => _errorMonotonic.Evaluate(monotonic, context),
            FieldEqualityPredicate field => _fieldEquality.Evaluate(field, context),
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
            ScaledEqualityPredicate scaled =>
                context.TryGetMetric(scaled.ActualRole, scaled.Metric, out _) &&
                context.TryGetMetric(scaled.ReferenceRole, scaled.Metric, out _),
            ErrorMonotonicPredicate monotonic =>
                context.TryGetMetric(monotonic.ReferenceRole, monotonic.Metric, out _) &&
                HasAllOrderedRoleMetrics(monotonic, context),
            FieldEqualityPredicate field =>
                context.TryGetField(field.LeftRole, field.LeftMetric, out _) &&
                context.TryGetField(field.RightRole, field.RightMetric, out _),
            DerivedInvariantPredicate derived =>
                context.TryGetField(derived.LeftRole, derived.LeftMetric, out _) &&
                context.TryGetField(derived.RightRole, derived.RightMetric, out _),
            _ => true
        };

    private static bool HasAllOrderedRoleMetrics(ErrorMonotonicPredicate monotonic, VerificationContext context)
    {
        foreach (var role in monotonic.OrderedRoles)
        {
            if (!context.TryGetMetric(role, monotonic.Metric, out _))
            {
                return false;
            }
        }

        return true;
    }
}
