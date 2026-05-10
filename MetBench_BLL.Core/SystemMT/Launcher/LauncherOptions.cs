namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Filesystem + interpreter configuration for the launcher. Production WPF
/// passes real paths from <c>App.xaml.cs</c>; tests pass paths derived from
/// the test bin's TestAssets directory and resolved Python executables.
/// </summary>
/// <param name="SutRoot">
/// Absolute path to the directory that contains per-SUT subdirectories
/// (<c>openmoc/</c>, <c>heat_equation/</c>, ...). The launcher resolves
/// scenario script paths relative to this root.
/// </param>
/// <param name="SystemPython">
/// Python executable for SUTs that only need the standard library (e.g.
/// heat-equation). Typically <c>"python3"</c> or an absolute path.
/// </param>
/// <param name="OpenMocPython">
/// Python executable that has OpenMOC importable. May equal
/// <see cref="SystemPython"/> when OpenMOC is installed system-wide; in
/// CI / cloud sandbox setups this points at the OpenMOC venv.
/// </param>
public sealed record LauncherOptions(
    string SutRoot,
    string SystemPython,
    string OpenMocPython);
