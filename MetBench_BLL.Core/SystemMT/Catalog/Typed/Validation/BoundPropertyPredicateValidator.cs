using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class BoundPropertyPredicateValidator
    : IPropertyPredicateValidator<BoundPropertyPredicate, PropertySpec>
{
    private readonly SharedReferenceResolver _resolver;
    private readonly ParameterExpressionResolver _parameterResolver;

    public BoundPropertyPredicateValidator(
        SharedReferenceResolver resolver,
        ParameterExpressionResolver parameterResolver)
    {
        _resolver = resolver;
        _parameterResolver = parameterResolver;
    }

    public ValidationResult Validate(BoundPropertyPredicate predicate, PropertySpec spec)
    {
        var errors = new List<ValidationError>();

        if (!_resolver.MetricExists(spec, predicate.Metric))
        {
            errors.Add(new ValidationError($"assertions[{predicate.PredicateId}].metric", $"Unknown metric '{predicate.Metric}'."));
        }

        var boundResult = _parameterResolver.Validate(predicate.Bound, $"assertions[{predicate.PredicateId}].bound");
        errors.AddRange(boundResult.Errors);

        return ValidationResult.Invalid(errors);
    }
}
