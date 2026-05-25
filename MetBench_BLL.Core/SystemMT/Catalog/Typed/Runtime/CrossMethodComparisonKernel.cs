using System;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class CrossMethodComparisonKernel : IVerifierKernel<CrossMethodComparisonPredicate>
{
    private readonly DeterministicScalarToleranceEvaluator _tolerance = new();

    public VerificationResult Evaluate(CrossMethodComparisonPredicate predicate, VerificationContext context)
    {
        var expected = context.GetMetric(predicate.RightRole, predicate.Metric);
        var actual = context.GetMetric(predicate.LeftRole, predicate.Metric);
        var computedTolerance = _tolerance.ComputeTolerance(expected, predicate.Tolerance);
        var residual = Math.Abs(actual - expected);

        bool passed;
        switch (predicate.Operator)
        {
            case "Greater":
                passed = actual > expected;
                break;
            case "Less":
                passed = actual < expected;
                break;
            case "Equal":
                passed = _tolerance.Within(actual, expected, predicate.Tolerance);
                break;
            default:
                throw new ArgumentException($"Unsupported cross-method operator '{predicate.Operator}'.", nameof(predicate));
        }

        var assertion = new SystemMtAssertionResultV2(
            "CrossMethodComparison",
            passed,
            expected,
            actual,
            residual,
            computedTolerance,
            $"{predicate.LeftRole}.{predicate.Metric} {predicate.Operator} {predicate.RightRole}.{predicate.Metric}",
            passed ? null : $"CrossMethodComparison failed: actual={actual}, expected={expected}, residual={residual}, tolerance={computedTolerance}");

        return VerificationResult.FromAssertion(assertion, new VerificationDiagnostic(expected, actual, residual, computedTolerance));
    }
}
