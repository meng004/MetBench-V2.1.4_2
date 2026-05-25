using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class DerivedInvariantPredicateValidator : IPredicateValidator<DerivedInvariantPredicate, MrSpec>
{
    private static readonly HashSet<string> SupportedDerivedMetrics = new(StringComparer.OrdinalIgnoreCase)
    {
        "mass_number_sum",
        "l2_norm",
        "linf_norm",
        "field_region_mean"
    };

    public ValidationResult Validate(DerivedInvariantPredicate predicate, MrSpec spec)
    {
        var errors = new List<ValidationError>();

        if (spec.Roles is null || !spec.Roles.ContainsKey(predicate.LeftRole))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].left_role", $"Unknown role '{predicate.LeftRole}'."));
        }

        if (spec.Roles is null || !spec.Roles.ContainsKey(predicate.RightRole))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].right_role", $"Unknown role '{predicate.RightRole}'."));
        }

        if (!IsFieldProjection(spec, predicate.LeftMetric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].left_metric", $"Unknown field projection '{predicate.LeftMetric}'."));
        }

        if (!IsFieldProjection(spec, predicate.RightMetric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].right_metric", $"Unknown field projection '{predicate.RightMetric}'."));
        }

        if (!SupportedDerivedMetrics.Contains(predicate.DerivedMetric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].derived_metric", $"Unsupported derived metric '{predicate.DerivedMetric}'."));
        }

        if (predicate.Tolerance.Atol < 0 || predicate.Tolerance.Rtol < 0)
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].tolerance", "Deterministic tolerance must be non-negative."));
        }

        return ValidationResult.Invalid(errors);
    }

    private static bool IsFieldProjection(MrSpec spec, string metric) =>
        spec.Projections is not null &&
        spec.Projections.TryGetValue(metric, out var projection) &&
        projection is Field2DProjectionSpec;
}
