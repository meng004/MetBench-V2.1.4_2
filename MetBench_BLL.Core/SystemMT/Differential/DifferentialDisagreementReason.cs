namespace MetBench_BLL.SystemMT.Differential;

/// <summary>
/// Why a differential run pair did not return <see cref="DifferentialAgreementStatus.Agree"/>.
/// Explicit reasons let the caller (and the audit trail) tell "both failed", "directions
/// disagreed", "ratio outside tolerance", "metric names mismatched", "one side errored", etc.
/// apart without parsing the diagnostic string.
/// </summary>
public enum DifferentialDisagreementReason
{
    /// <summary>The result is <see cref="DifferentialAgreementStatus.Agree"/>; no reason to report.</summary>
    None = 0,

    /// <summary>BothPassed criterion: neither side's MR assertion passed.</summary>
    BothFailed = 1,

    /// <summary>BothPassed criterion: only the left side's MR assertion failed.</summary>
    LeftFailed = 2,

    /// <summary>BothPassed criterion: only the right side's MR assertion failed.</summary>
    RightFailed = 3,

    /// <summary>DirectionConcordant criterion: the two sides moved the metric in opposite directions.</summary>
    DirectionsDisagreed = 4,

    /// <summary>FollowUpRatioWithinTolerance criterion: |left.ratio − right.ratio| > tolerance.</summary>
    RatioOutsideTolerance = 5,

    /// <summary>The two sides reported different <c>ValueName</c> metrics; cross-side comparison is undefined.</summary>
    MetricNameMismatch = 6,

    /// <summary>NaN, ±∞, or (under a ratio criterion) zero <c>SourceValue</c> appeared on at least one side.</summary>
    NonFiniteValue = 7,

    /// <summary><c>Tolerance</c> was negative; only non-negative tolerances are accepted.</summary>
    ToleranceNegative = 8,
}
