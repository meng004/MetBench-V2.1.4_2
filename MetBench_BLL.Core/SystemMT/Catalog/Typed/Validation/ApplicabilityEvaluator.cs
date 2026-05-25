using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Validation;

public sealed class ApplicabilityEvaluator
{
    public ApplicabilityDecision Evaluate(
        ApplicabilitySpec? applicability,
        IReadOnlyDictionary<string, double>? inputs)
    {
        if (applicability is null || applicability.Conditions.Count == 0)
        {
            return new ApplicabilityDecision(true, null);
        }

        foreach (var condition in applicability.Conditions)
        {
            if (!EvaluateCondition(condition, inputs ?? new Dictionary<string, double>(), out var reason))
            {
                return new ApplicabilityDecision(false, reason);
            }
        }

        return new ApplicabilityDecision(true, null);
    }

    private static bool EvaluateCondition(
        ConditionExpr condition,
        IReadOnlyDictionary<string, double> inputs,
        out string? reason)
    {
        if (condition is not ComparisonConditionExpr comparison)
        {
            reason = $"Unsupported applicability condition '{condition.GetType().Name}'.";
            return false;
        }

        var left = Resolve(comparison.Left, inputs, out var leftReason);
        var right = Resolve(comparison.Right, inputs, out var rightReason);
        if (!left.HasValue || !right.HasValue)
        {
            reason = leftReason ?? rightReason ?? "Applicability reference could not be resolved.";
            return false;
        }

        var matched = comparison.Operator switch
        {
            "LessOrEqual" => left.Value <= right.Value,
            "GreaterOrEqual" => left.Value >= right.Value,
            "Equal" => Math.Abs(left.Value - right.Value) <= 1e-12,
            _ => throw new ArgumentException($"Unsupported applicability operator '{comparison.Operator}'.", nameof(condition))
        };

        reason = matched ? null : $"Applicability condition failed: {Describe(comparison.Left)} {comparison.Operator} {Describe(comparison.Right)}.";
        return matched;
    }

    private static double? Resolve(
        RefExpr expression,
        IReadOnlyDictionary<string, double> inputs,
        out string? reason)
    {
        switch (expression)
        {
            case ConstantRefExpr constant:
                reason = null;
                return constant.Value;
            case InputRefExpr inputRef when inputs.TryGetValue(inputRef.Name, out var value):
                reason = null;
                return value;
            case InputRefExpr inputRef:
                reason = $"Missing applicability input '{inputRef.Name}'.";
                return null;
            default:
                throw new ArgumentException($"Unsupported applicability reference '{expression.GetType().Name}'.", nameof(expression));
        }
    }

    private static string Describe(RefExpr expression) =>
        expression switch
        {
            InputRefExpr inputRef => inputRef.Name,
            ConstantRefExpr constant => constant.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => expression.GetType().Name
        };
}

public sealed record ApplicabilityDecision(bool ShouldRun, string? Reason);
