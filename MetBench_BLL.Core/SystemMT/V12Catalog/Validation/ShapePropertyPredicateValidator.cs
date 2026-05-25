using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class ShapePropertyPredicateValidator
    : IPropertyPredicateValidator<ShapePropertyPredicate, PropertySpec>
{
    private readonly SharedReferenceResolver _resolver;

    public ShapePropertyPredicateValidator(SharedReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public ValidationResult Validate(ShapePropertyPredicate predicate, PropertySpec spec)
    {
        var errors = new List<ValidationError>();

        if (!_resolver.MetricExists(spec, predicate.Metric))
        {
            errors.Add(new ValidationError($"assertions[{predicate.PredicateId}].metric", $"Unknown metric '{predicate.Metric}'."));
        }

        return ValidationResult.Invalid(errors);
    }
}
