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
}
