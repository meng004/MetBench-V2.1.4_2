namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Stable, UI-facing description of an MT scenario the launcher can execute.
/// All members are primitives or dictionaries — no internal BLL.Core types
/// leak through this contract. WPF code (or any other caller) can persist
/// <see cref="Id"/> in user state and survive future BLL.Core refactors.
/// </summary>
/// <param name="MrFamily">
/// Optional taxonomy slug grouping scenarios that exercise the same
/// metamorphic relation across different programs. Two scenarios with the
/// same <see cref="MrFamily"/> value should produce assertion results
/// consistent with each other under the same parameter overrides (e.g. both
/// pass or both fail for matching MR direction). UI may use this to render
/// "this MR runs on N solvers" groupings or to drive batch dispatch.
/// Empty string (default) means "no cross-program relationship".
/// </param>
public sealed record ScenarioDescriptor(
    string Id,
    string DisplayName,
    string SutName,
    string TransformationName,
    string AssertionName,
    string ValueName,
    IReadOnlyDictionary<string, string> DefaultParameters,
    string Description,
    string MrFamily = "");
