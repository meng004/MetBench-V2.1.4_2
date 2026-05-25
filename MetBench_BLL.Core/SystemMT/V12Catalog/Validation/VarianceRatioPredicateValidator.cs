using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Validation;

public sealed class VarianceRatioPredicateValidator
    : IPredicateValidator<VarianceRatioPredicate, MrSpec>
{
    private readonly SharedReferenceResolver _resolver;
    private readonly ParameterExpressionResolver _parameterResolver;

    public VarianceRatioPredicateValidator(
        SharedReferenceResolver resolver,
        ParameterExpressionResolver parameterResolver)
    {
        _resolver = resolver;
        _parameterResolver = parameterResolver;
    }

    public ValidationResult Validate(VarianceRatioPredicate predicate, MrSpec spec)
    {
        var errors = new List<ValidationError>();

        if (!_resolver.RoleExists(spec, predicate.LowSampleRole))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].low_sample_role", $"Unknown role '{predicate.LowSampleRole}'."));
        }

        if (!_resolver.RoleExists(spec, predicate.HighSampleRole))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].high_sample_role", $"Unknown role '{predicate.HighSampleRole}'."));
        }

        if (!_resolver.MetricExists(spec, predicate.StatisticalMetric))
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].statistical_metric", $"Unknown metric '{predicate.StatisticalMetric}'."));
        }

        errors.AddRange(_parameterResolver.Validate(predicate.SampleRatio, $"predicates[{predicate.PredicateId}].sample_ratio", spec).Errors);

        if (predicate.Tolerance.SigmaMultiplier <= 0)
        {
            errors.Add(new ValidationError($"predicates[{predicate.PredicateId}].tolerance", "Sigma multiplier must be positive."));
        }

        return ValidationResult.Invalid(errors);
    }
}
