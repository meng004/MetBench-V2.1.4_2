using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class BinaryComparisonPredicateValidator
    : IPredicateValidator<BinaryComparisonPredicate, MrSpec>
{
    private readonly SharedReferenceResolver _resolver;

    public BinaryComparisonPredicateValidator(SharedReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public ValidationResult Validate(BinaryComparisonPredicate predicate, MrSpec spec)
    {
        var errors = new List<ValidationError>();

        if (!_resolver.RoleExists(spec, predicate.LeftRole))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].left_role", $"Unknown role '{predicate.LeftRole}'."));
        }

        if (!_resolver.RoleExists(spec, predicate.RightRole))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].right_role", $"Unknown role '{predicate.RightRole}'."));
        }

        if (!_resolver.MetricExists(spec, predicate.Metric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].metric", $"Unknown metric '{predicate.Metric}'."));
        }

        return ValidationResult.Invalid(errors);
    }
}
