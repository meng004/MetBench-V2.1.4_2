namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// One entry in a batch MR run. <see cref="ParameterOverrides"/> follows the same
/// semantics as the optional argument on
/// <see cref="ISystemMtMrLauncher.RunAsync"/> — keys present here replace
/// the MR.s defaults; missing keys fall back to defaults.
/// </summary>
public sealed record BatchMrRunRequest(
    string MrId,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null);
