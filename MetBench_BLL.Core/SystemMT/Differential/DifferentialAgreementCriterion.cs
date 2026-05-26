namespace MetBench_BLL.SystemMT.Differential;

/// <summary>
/// How <see cref="IDifferentialTestRunner"/> compares two <see cref="Launcher.MrRunResult"/>
/// objects to decide whether they agree. Each criterion is total — the runner's resolution
/// rules cover every input the criterion can see (NaN, ±∞, zero source, etc.).
/// </summary>
public enum DifferentialAgreementCriterion
{
    /// <summary>
    /// Both MR assertions pass independently. No numeric comparison between the two results.
    /// Cheapest criterion; suitable when each MR already encodes the cross-solver expectation
    /// internally and the differential runner just confirms both fired green.
    /// </summary>
    BothPassed = 0,

    /// <summary>
    /// Both runs moved the metric in the same direction
    /// (<c>sign(FollowUp - Source)</c> matches across the two sides; ties on both sides
    /// count as agreement).
    /// </summary>
    DirectionConcordant = 1,

    /// <summary>
    /// |left.FollowUpRatio − right.FollowUpRatio| ≤ <c>Tolerance</c>, where
    /// <c>FollowUpRatio = FollowUp / Source</c>. Requires non-zero <c>Source</c> on both
    /// sides; zero source under this criterion is rejected as
    /// <see cref="DifferentialDisagreementReason.NonFiniteValue"/>.
    /// </summary>
    FollowUpRatioWithinTolerance = 2,
}
