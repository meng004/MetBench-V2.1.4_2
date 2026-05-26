using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class NoiseAwareBinaryComparisonPredicateValidator
    : IPredicateValidator<NoiseAwareBinaryComparisonPredicate, MrSpec>
{
    private readonly SharedReferenceResolver _resolver;

    public NoiseAwareBinaryComparisonPredicateValidator(SharedReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public ValidationResult Validate(NoiseAwareBinaryComparisonPredicate predicate, MrSpec spec)
    {
        var errors = new List<ValidationError>();
        var path = $"predicates[{predicate.PredicateId}]";

        if (!_resolver.RoleExists(spec, predicate.LeftRole))
        {
            errors.Add(new ValidationError($"{path}.left_role", $"Unknown role '{predicate.LeftRole}'."));
        }

        if (!_resolver.RoleExists(spec, predicate.RightRole))
        {
            errors.Add(new ValidationError($"{path}.right_role", $"Unknown role '{predicate.RightRole}'."));
        }

        if (!_resolver.MetricExists(spec, predicate.Metric))
        {
            errors.Add(new ValidationError($"{path}.metric", $"Unknown metric '{predicate.Metric}'."));
        }

        if (string.IsNullOrWhiteSpace(predicate.SourceStdMetric))
        {
            errors.Add(new ValidationError($"{path}.source_std_metric", "SourceStdMetric is required (non-empty)."));
        }
        else if (!_resolver.MetricExists(spec, predicate.SourceStdMetric))
        {
            errors.Add(new ValidationError($"{path}.source_std_metric", $"Unknown metric '{predicate.SourceStdMetric}'."));
        }

        if (string.IsNullOrWhiteSpace(predicate.FollowupStdMetric))
        {
            errors.Add(new ValidationError($"{path}.followup_std_metric", "FollowupStdMetric is required (non-empty)."));
        }
        else if (!_resolver.MetricExists(spec, predicate.FollowupStdMetric))
        {
            errors.Add(new ValidationError($"{path}.followup_std_metric", $"Unknown metric '{predicate.FollowupStdMetric}'."));
        }

        if (predicate.Operator != "Greater" && predicate.Operator != "Less")
        {
            errors.Add(new ValidationError(
                $"{path}.operator",
                $"Operator '{predicate.Operator}' is not supported; expected 'Greater' or 'Less' "
                + "('Equal' is intentionally NOT supported on noise-aware scalar comparisons)."));
        }

        if (!double.IsFinite(predicate.NoiseMultiplier) || predicate.NoiseMultiplier <= 0)
        {
            errors.Add(new ValidationError(
                $"{path}.noise_multiplier",
                $"NoiseMultiplier '{predicate.NoiseMultiplier}' is not supported; must be finite and > 0."));
        }

        return ValidationResult.Invalid(errors);
    }
}
