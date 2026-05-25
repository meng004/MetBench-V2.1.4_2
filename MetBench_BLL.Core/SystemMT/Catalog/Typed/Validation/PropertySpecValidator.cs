using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class PropertySpecValidator : ISpecValidator<PropertySpec>
{
    private readonly ValidationRegistry _registry;

    public PropertySpecValidator(ValidationRegistry registry)
    {
        _registry = registry;
    }

    public ValidationResult Validate(PropertySpec spec)
    {
        var errors = new List<ValidationError>();

        if (spec.Projections is null || spec.Projections.Count == 0)
        {
            errors.Add(new ValidationError("projections", "At least one projection is required."));
        }

        var toleranceResult = _registry.ToleranceChecker.Validate(spec.DefaultTolerance, "default_tolerance");
        errors.AddRange(toleranceResult.Errors);

        foreach (var assertion in spec.Assertions)
        {
            var result = assertion switch
            {
                BoundPropertyPredicate bound => _registry.BoundPropertyValidator.Validate(bound, spec),
                ShapePropertyPredicate shape => _registry.ShapePropertyValidator.Validate(shape, spec),
                _ => ValidationResult.Invalid(new ValidationError($"assertions[{assertion.PredicateId}]", $"Unsupported property predicate type '{assertion.GetType().Name}'."))
            };
            errors.AddRange(result.Errors);
        }

        return ValidationResult.Invalid(errors);
    }
}
