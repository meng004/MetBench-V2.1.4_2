using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.V12Catalog.Specs;

namespace MetBench_BLL.SystemMT.V12Catalog.Property;

public sealed class BoundPropertyChecker : IPropertyChecker
{
    public PropertyResult Check(PropertySpec spec, PropertyVerificationContext context)
    {
        if (!context.SpecValidation.IsValid)
        {
            return new PropertyResult(spec.PropertyId, PropertyStatus.InvalidSpec, Array.Empty<PropertyPredicateResult>(), "Property validation failed before runtime checking.");
        }

        var results = new List<PropertyPredicateResult>();
        foreach (var assertion in spec.Assertions)
        {
            if (assertion is not BoundPropertyPredicate bound)
            {
                continue;
            }

            if (!context.TryGetMetric(bound.Metric, out var actual))
            {
                results.Add(new PropertyPredicateResult(bound.PredicateId, nameof(BoundPropertyPredicate), PropertyStatus.SkippedMissingObservable, Reason: $"Missing metric '{bound.Metric}'."));
                return new PropertyResult(spec.PropertyId, PropertyStatus.SkippedMissingObservable, results, $"Missing metric '{bound.Metric}'.");
            }

            var expected = ResolveBound(bound.Bound);
            var held = bound.Operator switch
            {
                "Greater" => actual > expected,
                "GreaterOrEqual" => actual >= expected,
                "Less" => actual < expected,
                "LessOrEqual" => actual <= expected,
                "Equal" => Math.Abs(actual - expected) <= 1e-9,
                _ => throw new ArgumentException($"Unsupported property operator '{bound.Operator}'.", nameof(spec))
            };

            results.Add(new PropertyPredicateResult(
                bound.PredicateId,
                nameof(BoundPropertyPredicate),
                held ? PropertyStatus.Held : PropertyStatus.Violated,
                Actual: actual,
                Expected: expected,
                Residual: Math.Abs(actual - expected),
                Tolerance: bound.Operator == "Equal" ? 1e-9 : null,
                Reason: held ? null : $"{bound.Metric} {bound.Operator} {expected} was not satisfied."));

            if (!held)
            {
                return new PropertyResult(spec.PropertyId, PropertyStatus.Violated, results, $"{bound.PredicateId} violated.");
            }
        }

        return new PropertyResult(spec.PropertyId, PropertyStatus.Held, results);
    }

    private static double ResolveBound(ParameterExpression expression) =>
        expression switch
        {
            ConstantParameterExpression constant => constant.Value,
            _ => throw new ArgumentException($"Unsupported property bound expression '{expression.GetType().Name}'.", nameof(expression))
        };
}
