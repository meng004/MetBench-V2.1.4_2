namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Stable, UI-facing description of an MT scenario the launcher can execute.
/// All members are primitives or dictionaries — no internal BLL.Core types
/// leak through this contract. WPF code (or any other caller) can persist
/// <see cref="Id"/> in user state and survive future BLL.Core refactors.
/// </summary>
public sealed record ScenarioDescriptor(
    string Id,
    string DisplayName,
    string SutName,
    string TransformationName,
    string AssertionName,
    string ValueName,
    IReadOnlyDictionary<string, string> DefaultParameters,
    string Description);
