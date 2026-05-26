using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Differential;

/// <summary>
/// Deterministic input DTO for <see cref="IDifferentialTestRunner.RunPairAsync"/>.
/// </summary>
/// <param name="LeftMrId">MR id passed to <c>ISystemMtLauncher.RunAsync</c> for the left side.</param>
/// <param name="RightMrId">MR id for the right side. May equal <paramref name="LeftMrId"/>.</param>
/// <param name="Criterion">How the two results are compared (see <see cref="DifferentialAgreementCriterion"/>).</param>
/// <param name="Tolerance">
/// Non-negative tolerance used by <see cref="DifferentialAgreementCriterion.FollowUpRatioWithinTolerance"/>;
/// ignored by other criteria. Negative values produce
/// <see cref="DifferentialDisagreementReason.ToleranceNegative"/>.
/// </param>
/// <param name="LeftParameterOverrides">
/// Optional parameter overrides forwarded to the left-side <c>ISystemMtLauncher.RunAsync</c> call;
/// <c>null</c> uses the MR's default parameters.
/// </param>
/// <param name="RightParameterOverrides">
/// Optional parameter overrides forwarded to the right-side <c>ISystemMtLauncher.RunAsync</c> call.
/// </param>
public sealed record DifferentialRunRequest(
    string LeftMrId,
    string RightMrId,
    DifferentialAgreementCriterion Criterion,
    double Tolerance = 0.0,
    IReadOnlyDictionary<string, string>? LeftParameterOverrides = null,
    IReadOnlyDictionary<string, string>? RightParameterOverrides = null);
