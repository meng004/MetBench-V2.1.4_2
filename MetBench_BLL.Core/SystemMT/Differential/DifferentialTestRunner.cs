using System;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Differential;

/// <summary>
/// Stateless, deterministic implementation of <see cref="IDifferentialTestRunner"/>. The
/// runner is the only sanctioned production path for the "two MRs, one differential
/// verdict" semantic (T1 §2.1 element 3); other code paths must compose this type rather
/// than reinventing the criterion logic.
/// </summary>
public sealed class DifferentialTestRunner : IDifferentialTestRunner
{
    private readonly ISystemMtLauncher _launcher;

    public DifferentialTestRunner(ISystemMtLauncher launcher)
    {
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
    }

    public async Task<DifferentialRunResult> RunPairAsync(
        DifferentialRunRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        // Tolerance pre-check fires before launcher I/O so a misconfigured request returns
        // a structured Inconclusive even when the criterion does not use tolerance — the
        // user almost certainly meant to set it and we'd rather surface the typo than
        // silently ignore the field.
        if (request.Tolerance < 0)
        {
            // We still need MrRunResult shells to populate the result record; run the
            // launcher anyway so callers see what both sides actually produced. The
            // verdict is decided up-front but the underlying observations are preserved.
            var leftEarly = await _launcher.RunAsync(request.LeftMrId, request.LeftParameterOverrides, cancellationToken).ConfigureAwait(false);
            var rightEarly = await _launcher.RunAsync(request.RightMrId, request.RightParameterOverrides, cancellationToken).ConfigureAwait(false);
            return new DifferentialRunResult(
                Left: leftEarly,
                Right: rightEarly,
                Status: DifferentialAgreementStatus.Inconclusive,
                Reason: DifferentialDisagreementReason.ToleranceNegative,
                Diagnostic: FormattableString.Invariant($"Tolerance must be >= 0, got {request.Tolerance}."));
        }

        // Sequential ordering is intentional: predictable diagnostics + same Calls[]
        // ordering whether the request is wrapped by a future progress reporter or not.
        var left = await _launcher.RunAsync(request.LeftMrId, request.LeftParameterOverrides, cancellationToken).ConfigureAwait(false);
        var right = await _launcher.RunAsync(request.RightMrId, request.RightParameterOverrides, cancellationToken).ConfigureAwait(false);

        if (!string.Equals(left.ValueName, right.ValueName, StringComparison.Ordinal))
        {
            return new DifferentialRunResult(
                left, right,
                DifferentialAgreementStatus.Inconclusive,
                DifferentialDisagreementReason.MetricNameMismatch,
                FormattableString.Invariant($"Left metric '{left.ValueName}' != right metric '{right.ValueName}'."));
        }

        if (HasNonFiniteValue(left, right) is { } nonFiniteDiag)
        {
            return new DifferentialRunResult(
                left, right,
                DifferentialAgreementStatus.Inconclusive,
                DifferentialDisagreementReason.NonFiniteValue,
                nonFiniteDiag);
        }

        switch (request.Criterion)
        {
            case DifferentialAgreementCriterion.BothPassed:
                return EvaluateBothPassed(left, right);
            case DifferentialAgreementCriterion.DirectionConcordant:
                return EvaluateDirectionConcordant(left, right);
            case DifferentialAgreementCriterion.FollowUpRatioWithinTolerance:
                return EvaluateFollowUpRatio(left, right, request.Tolerance);
            default:
                // The enum is closed — but a future variant would land here. Fail loud so
                // the test surface immediately catches the omission rather than silently
                // mapping to Inconclusive.
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Criterion,
                    "Unhandled DifferentialAgreementCriterion; add a case in DifferentialTestRunner.");
        }
    }

    private static string? HasNonFiniteValue(MrRunResult left, MrRunResult right)
    {
        if (!double.IsFinite(left.SourceValue))   return FormattableString.Invariant($"Left source value is non-finite: {left.SourceValue}.");
        if (!double.IsFinite(left.FollowUpValue)) return FormattableString.Invariant($"Left follow-up value is non-finite: {left.FollowUpValue}.");
        if (!double.IsFinite(right.SourceValue))   return FormattableString.Invariant($"Right source value is non-finite: {right.SourceValue}.");
        if (!double.IsFinite(right.FollowUpValue)) return FormattableString.Invariant($"Right follow-up value is non-finite: {right.FollowUpValue}.");
        return null;
    }

    private static DifferentialRunResult EvaluateBothPassed(MrRunResult left, MrRunResult right)
    {
        if (left.Passed && right.Passed)
            return new DifferentialRunResult(left, right, DifferentialAgreementStatus.Agree, DifferentialDisagreementReason.None, null);

        DifferentialDisagreementReason reason;
        string diag;
        if (!left.Passed && !right.Passed)
        {
            reason = DifferentialDisagreementReason.BothFailed;
            diag = FormattableString.Invariant($"Both sides failed. Left: {left.FailureReason}; right: {right.FailureReason}.");
        }
        else if (!left.Passed)
        {
            reason = DifferentialDisagreementReason.LeftFailed;
            diag = FormattableString.Invariant($"Left failed: {left.FailureReason}.");
        }
        else
        {
            reason = DifferentialDisagreementReason.RightFailed;
            diag = FormattableString.Invariant($"Right failed: {right.FailureReason}.");
        }
        return new DifferentialRunResult(left, right, DifferentialAgreementStatus.Disagree, reason, diag);
    }

    private static DifferentialRunResult EvaluateDirectionConcordant(MrRunResult left, MrRunResult right)
    {
        var leftDir = Math.Sign(left.FollowUpValue - left.SourceValue);
        var rightDir = Math.Sign(right.FollowUpValue - right.SourceValue);
        if (leftDir == rightDir)
            return new DifferentialRunResult(left, right, DifferentialAgreementStatus.Agree, DifferentialDisagreementReason.None, null);

        return new DifferentialRunResult(
            left, right,
            DifferentialAgreementStatus.Disagree,
            DifferentialDisagreementReason.DirectionsDisagreed,
            FormattableString.Invariant($"Left direction sign({left.FollowUpValue} - {left.SourceValue}) = {leftDir}; ")
            + FormattableString.Invariant($"right direction sign({right.FollowUpValue} - {right.SourceValue}) = {rightDir}."));
    }

    private static DifferentialRunResult EvaluateFollowUpRatio(MrRunResult left, MrRunResult right, double tolerance)
    {
        if (left.SourceValue == 0.0 || right.SourceValue == 0.0)
        {
            return new DifferentialRunResult(
                left, right,
                DifferentialAgreementStatus.Inconclusive,
                DifferentialDisagreementReason.NonFiniteValue,
                "FollowUpRatioWithinTolerance requires non-zero source on both sides; "
                + FormattableString.Invariant($"got zero source (left={left.SourceValue}, right={right.SourceValue})."));
        }

        var leftRatio = left.FollowUpValue / left.SourceValue;
        var rightRatio = right.FollowUpValue / right.SourceValue;
        var gap = Math.Abs(leftRatio - rightRatio);
        if (gap <= tolerance)
            return new DifferentialRunResult(left, right, DifferentialAgreementStatus.Agree, DifferentialDisagreementReason.None, null);

        return new DifferentialRunResult(
            left, right,
            DifferentialAgreementStatus.Disagree,
            DifferentialDisagreementReason.RatioOutsideTolerance,
            FormattableString.Invariant($"|left.ratio ({leftRatio}) - right.ratio ({rightRatio})| = {gap} > tolerance {tolerance}."));
    }

}
