using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class ScaledEqualityPredicateValidator
    : IPredicateValidator<ScaledEqualityPredicate, MrSpec>
{
    private readonly SharedReferenceResolver _resolver;
    private readonly ParameterExpressionResolver _parameterResolver;

    public ScaledEqualityPredicateValidator(
        SharedReferenceResolver resolver,
        ParameterExpressionResolver parameterResolver)
    {
        _resolver = resolver;
        _parameterResolver = parameterResolver;
    }

    public ValidationResult Validate(ScaledEqualityPredicate predicate, MrSpec spec)
    {
        var errors = new List<ValidationError>();

        if (!_resolver.RoleExists(spec, predicate.ActualRole))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].actual_role", $"Unknown role '{predicate.ActualRole}'."));
        }

        if (!_resolver.RoleExists(spec, predicate.ReferenceRole))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].reference_role", $"Unknown role '{predicate.ReferenceRole}'."));
        }

        if (!_resolver.MetricExists(spec, predicate.Metric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].metric", $"Unknown metric '{predicate.Metric}'."));
        }

        errors.AddRange(_parameterResolver.Validate(predicate.Factor, $"predicates[{predicate.PredicateId}].factor", spec).Errors);

        return ValidationResult.Invalid(errors);
    }
}
