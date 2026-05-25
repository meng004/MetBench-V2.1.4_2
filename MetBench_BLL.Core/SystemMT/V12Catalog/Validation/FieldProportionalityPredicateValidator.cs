using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class FieldProportionalityPredicateValidator
    : IPredicateValidator<FieldProportionalityPredicate, MrSpec>
{
    public ValidationResult Validate(FieldProportionalityPredicate predicate, MrSpec spec)
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

        if (!HasFieldProjection(spec, predicate.LeftMetric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].left_metric", $"Unknown field projection '{predicate.LeftMetric}'."));
        }

        if (!HasFieldProjection(spec, predicate.RightMetric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].right_metric", $"Unknown field projection '{predicate.RightMetric}'."));
        }

        return ValidationResult.Invalid(errors);
    }

    private static bool HasFieldProjection(MrSpec spec, string metric) =>
        spec.Projections is not null &&
        spec.Projections.TryGetValue(metric, out var projection) &&
        projection is Field2DProjectionSpec;
}
