using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class ErrorMonotonicPredicateValidator
    : IPredicateValidator<ErrorMonotonicPredicate, MrSpec>
{
    private readonly SharedReferenceResolver _resolver;

    public ErrorMonotonicPredicateValidator(SharedReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public ValidationResult Validate(ErrorMonotonicPredicate predicate, MrSpec spec)
    {
        var errors = new List<ValidationError>();

        if (predicate.OrderedRoles.Count < 2)
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].ordered_roles", "Ordered roles must contain at least two entries."));
        }

        if (predicate.OrderedRoles.Distinct().Count() != predicate.OrderedRoles.Count)
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].ordered_roles", "Ordered roles must not contain duplicates."));
        }

        foreach (var role in predicate.OrderedRoles)
        {
            if (!_resolver.RoleExists(spec, role))
            {
                errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].ordered_roles", $"Unknown role '{role}'."));
            }
        }

        if (!_resolver.RoleExists(spec, predicate.ReferenceRole))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].reference_role", $"Unknown role '{predicate.ReferenceRole}'."));
        }

        if (!_resolver.MetricExists(spec, predicate.Metric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].metric", $"Unknown metric '{predicate.Metric}'."));
        }

        return ValidationResult.Invalid(errors);
    }
}
