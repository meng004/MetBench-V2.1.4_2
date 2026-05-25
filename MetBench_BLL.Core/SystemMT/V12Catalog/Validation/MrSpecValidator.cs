using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class MrSpecValidator : ISpecValidator<MrSpec>
{
    private readonly ValidationRegistry _registry;

    public MrSpecValidator(ValidationRegistry registry)
    {
        _registry = registry;
    }

    public ValidationResult Validate(MrSpec spec)
    {
        var errors = new List<ValidationError>();

        if (spec.Roles is null || spec.Roles.Count == 0)
        {
            errors.Add(new ValidationError("roles", "At least one role is required."));
        }

        if (spec.Projections is null || spec.Projections.Count == 0)
        {
            errors.Add(new ValidationError("projections", "At least one projection is required."));
        }

        var toleranceResult = _registry.ToleranceChecker.Validate(spec.DefaultTolerance, "default_tolerance");
        errors.AddRange(toleranceResult.Errors);

        if (spec.Parameters is not null)
        {
            foreach (var parameter in spec.Parameters)
            {
                errors.AddRange(_registry.ParameterResolver.Validate(parameter.Value, $"parameters.{parameter.Key}", spec).Errors);
            }
        }

        foreach (var predicate in spec.Predicates)
        {
            var result = predicate switch
            {
                BinaryComparisonPredicate binary => _registry.BinaryComparisonValidator.Validate(binary, spec),
                ScaledEqualityPredicate scaled => _registry.ScaledEqualityValidator.Validate(scaled, spec),
                _ => ValidationResult.Invalid(new ValidationError($"predicates[{predicate.PredicateId}]", $"Unsupported predicate type '{predicate.GetType().Name}'."))
            };
            errors.AddRange(result.Errors);
        }

        return ValidationResult.Invalid(errors);
    }
}
