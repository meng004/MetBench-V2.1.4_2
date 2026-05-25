using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class CrossMethodPredicateValidator
    : IPredicateValidator<CrossMethodComparisonPredicate, MrSpec>
{
    private readonly SharedReferenceResolver _resolver;

    public CrossMethodPredicateValidator(SharedReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public ValidationResult Validate(CrossMethodComparisonPredicate predicate, MrSpec spec)
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

        if (spec.Roles is not null &&
            spec.Roles.TryGetValue(predicate.LeftRole, out var leftRole) &&
            leftRole.MethodBinding is null)
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].left_role.method_binding", $"Role '{predicate.LeftRole}' requires MethodBinding."));
        }

        if (spec.Roles is not null &&
            spec.Roles.TryGetValue(predicate.RightRole, out var rightRole) &&
            rightRole.MethodBinding is null)
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].right_role.method_binding", $"Role '{predicate.RightRole}' requires MethodBinding."));
        }

        return ValidationResult.Invalid(errors);
    }
}
