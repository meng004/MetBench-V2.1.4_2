using System;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Catalog.Typed.Runtime;

/// <summary>
/// Evaluates <see cref="NoiseAwareBinaryComparisonPredicate"/>. The kernel widens the
/// directional inequality by <c>noiseTol = NoiseMultiplier · √(σ_source² + σ_followup²)</c>:
/// <list type="bullet">
///   <item><c>Greater</c>: pass iff <c>right.metric > left.metric - noiseTol</c>.</item>
///   <item><c>Less</c>:    pass iff <c>right.metric &lt; left.metric + noiseTol</c>.</item>
/// </list>
/// "Equal" is intentionally rejected as <see cref="VerifyStatus.InvalidSpec"/> — a noise-aware
/// equality check is a different two-sided test (<c>|left − right| ≤ noiseTol</c>) that has
/// not been requested by any MR catalog binding yet and would be a separate predicate.
///
/// Missing metrics are filtered earlier by <see cref="PredicateDispatcher"/>'s
/// <c>HasObservable</c> arm; this kernel additionally checks for NaN / ±∞ on any of the
/// four resolved doubles and returns <see cref="VerifyStatus.SkippedMissingObservable"/>
/// (the metric existed but its value is non-finite — treat as missing data so an upstream
/// numerical breakdown does not silently project to Pass / Fail).
/// </summary>
public sealed class NoiseAwareBinaryComparisonKernel
    : IVerifierKernel<NoiseAwareBinaryComparisonPredicate>
{
    private readonly NoiseAwareScalarToleranceEvaluator _tolerance = new();

    public VerificationResult Evaluate(
        NoiseAwareBinaryComparisonPredicate predicate,
        VerificationContext context)
    {
        if (predicate.Operator != "Greater" && predicate.Operator != "Less")
        {
            var reason = predicate.Operator == "Equal"
                ? "NoiseAwareBinaryComparison does not support Operator 'Equal'; "
                  + "noise-aware equality is a separate two-sided test and must be expressed "
                  + "by a dedicated predicate when needed."
                : $"NoiseAwareBinaryComparison rejected Operator '{predicate.Operator}'; "
                  + "only 'Greater' and 'Less' are supported.";
            return VerificationResult.InvalidSpec(reason);
        }

        if (!double.IsFinite(predicate.NoiseMultiplier) || predicate.NoiseMultiplier <= 0)
        {
            return VerificationResult.InvalidSpec(
                $"NoiseAwareBinaryComparison rejected NoiseMultiplier '{predicate.NoiseMultiplier}'; "
                + "must be finite and > 0.");
        }

        var left = context.GetMetric(predicate.LeftRole, predicate.Metric);
        var right = context.GetMetric(predicate.RightRole, predicate.Metric);
        var sourceStd = context.GetMetric(predicate.LeftRole, predicate.SourceStdMetric);
        var followupStd = context.GetMetric(predicate.RightRole, predicate.FollowupStdMetric);

        if (!double.IsFinite(left) || !double.IsFinite(right)
            || !double.IsFinite(sourceStd) || !double.IsFinite(followupStd))
        {
            return VerificationResult.SkippedMissingObservable(
                $"NoiseAwareBinaryComparison: one or more metric values are non-finite "
                + $"(left={left}, right={right}, sourceStd={sourceStd}, followupStd={followupStd}). "
                + "Treat as missing data rather than evaluating the inequality.");
        }

        if (sourceStd < 0 || followupStd < 0)
        {
            return VerificationResult.SkippedMissingObservable(
                $"NoiseAwareBinaryComparison: σ values must be non-negative "
                + $"(sourceStd={sourceStd}, followupStd={followupStd}).");
        }

        var noiseTolerance = _tolerance.ComputeTolerance(sourceStd, followupStd, predicate.NoiseMultiplier);

        bool passed = predicate.Operator switch
        {
            "Greater" => right > left - noiseTolerance,
            "Less" => right < left + noiseTolerance,
            _ => throw new InvalidOperationException("unreachable; operator vetted above"),
        };

        var residual = Math.Abs(right - left);
        var description =
            $"{predicate.RightRole}.{predicate.Metric} {predicate.Operator} "
            + $"{predicate.LeftRole}.{predicate.Metric} "
            + $"± noise({predicate.NoiseMultiplier}·√(σ_source² + σ_followup²))";

        var assertion = new SystemMtAssertionResultV2(
            predicate.Operator,
            passed,
            left,
            right,
            residual,
            noiseTolerance,
            description,
            passed
                ? null
                : $"NoiseAwareBinaryComparison {predicate.Operator} failed: "
                  + $"left={left}, right={right}, residual={residual}, noiseTolerance={noiseTolerance} "
                  + $"(σ_source={sourceStd}, σ_followup={followupStd}, k={predicate.NoiseMultiplier}).");

        return VerificationResult.FromAssertion(
            assertion,
            new VerificationDiagnostic(left, right, residual, noiseTolerance));
    }
}
