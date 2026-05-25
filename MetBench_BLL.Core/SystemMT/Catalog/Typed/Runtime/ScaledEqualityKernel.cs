using System;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;
using MetBench_BLL.SystemMT.Catalog.Typed.Validation;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class ScaledEqualityKernel : IVerifierKernel<ScaledEqualityPredicate>
{
    private readonly DeterministicScalarToleranceEvaluator _tolerance = new();
    private readonly ParameterExpressionResolver _parameterResolver = new();

    public VerificationResult Evaluate(ScaledEqualityPredicate predicate, VerificationContext context)
    {
        var reference = context.GetMetric(predicate.ReferenceRole, predicate.Metric);
        var actual = context.GetMetric(predicate.ActualRole, predicate.Metric);
        var factor = ResolveFactor(predicate.Factor, context.Spec);
        var expected = Math.Pow(factor, predicate.Exponent) * reference;
        var tolerance = context.Spec.DefaultTolerance as DeterministicToleranceSpec
            ?? new DeterministicToleranceSpec(0.0, 0.0);
        var residual = Math.Abs(actual - expected);
        var computedTolerance = _tolerance.ComputeTolerance(expected, tolerance);
        var passed = residual <= computedTolerance;

        var assertion = new SystemMtAssertionResultV2(
            "ScaledEquality",
            passed,
            reference,
            actual,
            residual,
            computedTolerance,
            $"{predicate.ActualRole}.{predicate.Metric} ~= factor^{predicate.Exponent} * {predicate.ReferenceRole}.{predicate.Metric}",
            passed ? null : $"ScaledEquality failed: actual={actual}, expected={expected}, residual={residual}, tolerance={computedTolerance}");

        return VerificationResult.FromAssertion(assertion, new VerificationDiagnostic(expected, actual, residual, computedTolerance));
    }

    private double ResolveFactor(ParameterExpression expression, MrSpec spec) =>
        expression switch
        {
            ConstantParameterExpression constant => constant.Value,
            MrParameterRefExpression reference when spec.Parameters is not null && spec.Parameters.TryGetValue(reference.Name, out var parameter) =>
                parameter switch
                {
                    ConstantParameterExpression constant => constant.Value,
                    _ => throw new ArgumentException($"Resolved MR parameter '{reference.Name}' is not a scalar constant.")
                },
            _ => throw new ArgumentException($"Unsupported factor expression '{expression.GetType().Name}'.")
        };
}
