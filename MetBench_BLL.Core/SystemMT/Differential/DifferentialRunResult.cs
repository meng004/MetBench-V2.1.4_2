using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Differential;

/// <summary>
/// Outcome of one <see cref="IDifferentialTestRunner.RunPairAsync"/> call. Carries both
/// underlying <see cref="MrRunResult"/> objects unchanged so callers (audit reports,
/// anomaly investigation, future UI) can drill in without re-fetching from persistence,
/// plus the deterministic <see cref="DifferentialAgreementStatus"/> /
/// <see cref="DifferentialDisagreementReason"/> verdict and an optional diagnostic.
/// </summary>
/// <param name="Left">Left side's MR run result, as returned by the launcher.</param>
/// <param name="Right">Right side's MR run result.</param>
/// <param name="Status">Whether the two results satisfied the criterion.</param>
/// <param name="Reason">
/// Why the verdict is what it is; <see cref="DifferentialDisagreementReason.None"/> only when
/// <see cref="Status"/> is <see cref="DifferentialAgreementStatus.Agree"/>.
/// </param>
/// <param name="Diagnostic">
/// Human-readable detail naming offending values where applicable; <c>null</c> on Agree.
/// </param>
public sealed record DifferentialRunResult(
    MrRunResult Left,
    MrRunResult Right,
    DifferentialAgreementStatus Status,
    DifferentialDisagreementReason Reason,
    string? Diagnostic);
