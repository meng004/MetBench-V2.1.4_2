using System.Collections.Generic;
using System.Linq;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class OrderedSequenceShapePredicateValidator
    : IPredicateValidator<OrderedSequenceShapePredicate, MrSpec>
{
    private readonly SharedReferenceResolver _resolver;
    private readonly ParameterExpressionResolver _parameterResolver;

    public OrderedSequenceShapePredicateValidator(
        SharedReferenceResolver resolver,
        ParameterExpressionResolver parameterResolver)
    {
        _resolver = resolver;
        _parameterResolver = parameterResolver;
    }

    public ValidationResult Validate(OrderedSequenceShapePredicate predicate, MrSpec spec)
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

        if (!_resolver.MetricExists(spec, predicate.Metric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].metric", $"Unknown metric '{predicate.Metric}'."));
        }

        if (predicate.Shape is ExponentialGrowthSpec exponential)
        {
            errors.AddRange(_parameterResolver.Validate(exponential.ExpectedRate, $"predicates[{predicate.PredicateId}].shape.expected_rate", spec).Errors);
        }

        return ValidationResult.Invalid(errors);
    }
}
