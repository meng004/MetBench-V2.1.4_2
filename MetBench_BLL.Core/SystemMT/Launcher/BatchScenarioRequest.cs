namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// One entry in a batch run. <see cref="ParameterOverrides"/> follows the same
/// semantics as the optional argument on
/// <see cref="ISystemMtScenarioLauncher.RunAsync"/> — keys present here replace
/// the scenario's defaults; missing keys fall back to defaults.
/// </summary>
public sealed record BatchScenarioRequest(
    string ScenarioId,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null);
