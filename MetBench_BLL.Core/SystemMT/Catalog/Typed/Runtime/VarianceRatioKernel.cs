using System;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

public sealed class VarianceRatioKernel : IVerifierKernel<VarianceRatioPredicate>
{
    public VerificationResult Evaluate(VarianceRatioPredicate predicate, VerificationContext context)
    {
        var low = context.GetStatistical(predicate.LowSampleRole, predicate.StatisticalMetric);
        var high = context.GetStatistical(predicate.HighSampleRole, predicate.StatisticalMetric);
        var sampleRatio = ResolveSampleRatio(predicate.SampleRatio);
        var expectedStdError = low.StdError / Math.Sqrt(sampleRatio);
        var threshold = expectedStdError * predicate.Tolerance.SigmaMultiplier;
        var residual = Math.Max(0.0, high.StdError - threshold);
        var passed = high.StdError <= threshold;

        var assertion = new SystemMtAssertionResultV2(
            "VarianceRatio",
            passed,
            expectedStdError,
            high.StdError,
            residual,
            threshold,
            $"{predicate.HighSampleRole}.{predicate.StatisticalMetric}.stderr <= {predicate.LowSampleRole}.{predicate.StatisticalMetric}.stderr / sqrt({sampleRatio})",
            passed ? null : $"VarianceRatio failed: actual ratio std_error={high.StdError}, expected_threshold={threshold}, ratio_sample={sampleRatio}");

        return VerificationResult.FromAssertion(assertion, new VerificationDiagnostic(expectedStdError, high.StdError, residual, threshold));
    }

    private static double ResolveSampleRatio(ParameterExpression expression) =>
        expression switch
        {
            ConstantParameterExpression constant => constant.Value,
            _ => throw new ArgumentException($"Unsupported sample ratio expression '{expression.GetType().Name}'.", nameof(expression))
        };
}
