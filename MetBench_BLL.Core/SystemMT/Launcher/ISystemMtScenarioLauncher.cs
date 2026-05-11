namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Stage 4 facade: WPF (and other UI / CLI) callers go through this interface
/// to list available system-MT scenarios and execute them. The interface owns
/// the responsibility of routing a scenario id to the correct SUT runner +
/// transformation + assertion combination, persisting the result, and
/// returning a UI-friendly summary.
/// </summary>
public interface ISystemMtScenarioLauncher
{
    /// <summary>
    /// Enumerate the scenarios this launcher knows about. Stable across
    /// process restarts; UI may cache.
    /// </summary>
    Task<IReadOnlyList<ScenarioDescriptor>> ListAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run one scenario. <paramref name="parameterOverrides"/> replaces the
    /// scenario's default parameters where keys overlap; missing keys fall
    /// back to defaults. The persisted record is keyed by the scenario's
    /// <see cref="ScenarioDescriptor.DisplayName"/> as
    /// <c>SystemMtResultRecord.ScenarioName</c>.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="scenarioId"/> is unknown or blank.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> trips.</exception>
    Task<ScenarioRunResult> RunAsync(
        string scenarioId,
        IReadOnlyDictionary<string, string>? parameterOverrides = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Run a sequence of scenarios. Pre-validates the whole batch (blank or
    /// unknown ids fail fast before any scenario starts). Scenarios run
    /// sequentially in the order given. An infrastructure exception thrown
    /// inside one scenario (Python failure, missing SUT file, etc.) is
    /// captured as a failed <see cref="ScenarioRunResult"/> so the remaining
    /// scenarios continue; <see cref="OperationCanceledException"/> propagates
    /// immediately. <paramref name="progress"/>, if provided, receives two
    /// events per scenario (see <see cref="BatchProgress"/>).
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="requests"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">If any request has a blank or unknown scenario id.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> trips.</exception>
    Task<IReadOnlyList<ScenarioRunResult>> RunBatchAsync(
        IReadOnlyList<BatchScenarioRequest> requests,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
