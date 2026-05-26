namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Filesystem + interpreter configuration for the launcher. Production WPF
/// passes real paths from <c>App.xaml.cs</c>; tests pass paths derived from
/// the test bin's TestAssets directory and resolved Python executables.
/// </summary>
/// <param name="SutRoot">
/// Absolute path to the directory that contains per-SUT subdirectories
/// (<c>openmoc/</c>, <c>heat_equation/</c>, ...). The launcher resolves
/// MR script paths relative to this root.
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
/// <param name="OpenMcPython">
/// Python executable that has OpenMC importable. Independent from
/// <see cref="OpenMocPython"/> because OpenMOC and OpenMC have
/// incompatible build requirements and typically live in separate venvs.
/// May equal <see cref="SystemPython"/> when neither is installed
/// (MRs that need OpenMC will fail at runtime; that is expected
/// in CI / VM where OpenMC is not installed).
/// </param>
/// <param name="ScipyPython">
/// Python executable that has SciPy importable. Used by external-solver-pilot
/// SUTs that depend on <c>scipy.integrate</c> (T3C-IVP / T3C-BVP).
/// May equal <see cref="SystemPython"/> when SciPy is installed system-wide;
/// when SciPy is missing the corresponding launcher end-to-end tests skip
/// cleanly via <c>ScipyTestPaths.ScipyImportable()</c>.
/// </param>
public sealed record LauncherOptions(
    string SutRoot,
    string SystemPython,
    string OpenMocPython,
    string? OpenMcPython = null,
    string? ScipyPython = null)
{
    /// <summary>
    /// Resolves to <see cref="OpenMcPython"/> if set, otherwise
    /// <see cref="SystemPython"/>. Lets existing call sites that don't know
    /// about OpenMC continue to compile; OpenMC MRs attempted with the
    /// system Python will simply fail at run time (which is acceptable on
    /// machines that do not have OpenMC installed).
    /// </summary>
    public string EffectiveOpenMcPython => string.IsNullOrWhiteSpace(OpenMcPython) ? SystemPython : OpenMcPython!;

    /// <summary>
    /// Resolves to <see cref="ScipyPython"/> if set, otherwise
    /// <see cref="SystemPython"/>. SciPy-backed SUTs attempted without
    /// a SciPy-equipped Python will simply fail at run time; tests gate
    /// on <c>ScipyTestPaths.ScipyImportable()</c> and skip cleanly when
    /// SciPy is unavailable.
    /// </summary>
    public string EffectiveScipyPython => string.IsNullOrWhiteSpace(ScipyPython) ? SystemPython : ScipyPython!;
}
