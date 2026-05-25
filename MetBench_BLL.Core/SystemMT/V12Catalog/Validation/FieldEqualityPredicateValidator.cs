using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class FieldEqualityPredicateValidator : IPredicateValidator<FieldEqualityPredicate, MrSpec>
{
    public ValidationResult Validate(FieldEqualityPredicate predicate, MrSpec spec)
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

        var leftProjection = ResolveFieldProjection(spec, predicate.LeftMetric);
        if (leftProjection is null)
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].left_metric", $"Unknown field projection '{predicate.LeftMetric}'."));
        }

        var rightProjection = ResolveFieldProjection(spec, predicate.RightMetric);
        if (rightProjection is null)
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].right_metric", $"Unknown field projection '{predicate.RightMetric}'."));
        }

        if (leftProjection is not null &&
            rightProjection is not null &&
            !DimensionsMatch(predicate.Pairing, leftProjection, rightProjection))
        {
            errors.Add(new ValidationError(
                $"predicates[{predicate.PredicateId}].metric_dimensions",
                $"Field dimensions must match for '{predicate.LeftMetric}' and '{predicate.RightMetric}'."));
        }

        if (predicate.Tolerance.Atol < 0 || predicate.Tolerance.Rtol < 0)
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].tolerance", "Field tolerance must be non-negative."));
        }

        if (string.IsNullOrWhiteSpace(predicate.Tolerance.Norm))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].tolerance.norm", "Field norm kind is required."));
        }

        return ValidationResult.Invalid(errors);
    }

    private static Field2DProjectionSpec? ResolveFieldProjection(MrSpec spec, string projectionKey) =>
        spec.Projections is not null &&
        spec.Projections.TryGetValue(projectionKey, out var projection) &&
        projection is Field2DProjectionSpec fieldProjection
            ? fieldProjection
            : null;

    private static bool DimensionsMatch(
        FieldPairing pairing,
        Field2DProjectionSpec leftProjection,
        Field2DProjectionSpec rightProjection) =>
        pairing switch
        {
            TransposeFieldPairing =>
                leftProjection.Rows == rightProjection.Columns &&
                leftProjection.Columns == rightProjection.Rows,
            _ =>
                leftProjection.Rows == rightProjection.Rows &&
                leftProjection.Columns == rightProjection.Columns
        };
}
