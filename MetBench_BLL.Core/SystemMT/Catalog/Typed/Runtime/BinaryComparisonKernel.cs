using System;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class BinaryComparisonKernel : IVerifierKernel<BinaryComparisonPredicate>
{
    private readonly DeterministicScalarToleranceEvaluator _tolerance = new();

    public VerificationResult Evaluate(BinaryComparisonPredicate predicate, VerificationContext context)
    {
        var expected = context.GetMetric(predicate.RightRole, predicate.Metric);
        var actual = context.GetMetric(predicate.LeftRole, predicate.Metric);
        var tolerance = context.Spec.DefaultTolerance as DeterministicToleranceSpec
            ?? new DeterministicToleranceSpec(0.0, 0.0);

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
                passed = _tolerance.Within(actual, expected, tolerance);
                break;
            default:
                throw new ArgumentException($"Unsupported binary operator '{predicate.Operator}'.", nameof(predicate));
        }

        var residual = Math.Abs(actual - expected);
        var computedTolerance = _tolerance.ComputeTolerance(expected, tolerance);
        var assertion = new SystemMtAssertionResultV2(
            predicate.Operator,
            passed,
            expected,
            actual,
            residual,
            computedTolerance,
            $"{predicate.LeftRole}.{predicate.Metric} {predicate.Operator} {predicate.RightRole}.{predicate.Metric}",
            passed ? null : $"BinaryComparison {predicate.Operator} failed: actual={actual}, expected={expected}, residual={residual}, tolerance={computedTolerance}");

        return VerificationResult.FromAssertion(assertion, new VerificationDiagnostic(expected, actual, residual, computedTolerance));
    }
}
