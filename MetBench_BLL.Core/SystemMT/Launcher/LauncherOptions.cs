using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Catalog;

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
/// Also acts as the fallback for the legacy <c>system</c> runtime key and
/// for any compat field whose dedicated parameter is blank.
/// </param>
/// <param name="OpenMocPython">
/// Compatibility field for the <c>openmoc</c> runtime key. Kept so existing
/// callers (tests, App.xaml.cs) compile unchanged. New code should prefer
/// <see cref="RuntimePythons"/> instead.
/// </param>
/// <param name="OpenMcPython">
/// Compatibility field for the <c>openmc</c> runtime key. Falls back to
/// <see cref="SystemPython"/> when blank. Kept so existing callers compile
/// unchanged. New code should prefer <see cref="RuntimePythons"/>.
/// </param>
/// <param name="ScipyPython">
/// Compatibility field for the <c>scipy</c> runtime key. Falls back to
/// <see cref="SystemPython"/> when blank. Kept so existing callers compile
/// unchanged. New code should prefer <see cref="RuntimePythons"/>.
/// </param>
/// <param name="RuntimePythons">
/// Generic manifest-driven runtime resolver. Maps a manifest
/// <c>python_executable_kind</c> string (e.g. <c>"fenics"</c>, <c>"fipy"</c>,
/// <c>"torch-surrogate"</c>) to an absolute Python executable path.
/// Lookup is case-insensitive; blank values are ignored (resolver continues
/// to compat-field fallback). When set, this entry takes precedence over
/// <see cref="OpenMocPython"/> / <see cref="OpenMcPython"/> / <see cref="ScipyPython"/>
/// for the matching key. Adding a new runtime family in the future is a
/// config-only change — no new <c>LauncherOptions</c> field is required.
/// </param>
public sealed record LauncherOptions(
    string SutRoot,
    string SystemPython,
    string OpenMocPython,
    string? OpenMcPython = null,
    string? ScipyPython = null,
    IReadOnlyDictionary<string, string>? RuntimePythons = null)
{
    /// <summary>
    /// Resolves to the <c>openmc</c> runtime via <see cref="ResolvePythonExecutable"/>.
    /// Kept for source compatibility with existing callers.
    /// </summary>
    public string EffectiveOpenMcPython => ResolvePythonExecutable(PythonExecutableKinds.OpenMc);

    /// <summary>
    /// Resolves to the <c>scipy</c> runtime via <see cref="ResolvePythonExecutable"/>.
    /// Kept for source compatibility with existing callers.
    /// </summary>
    public string EffectiveScipyPython => ResolvePythonExecutable(PythonExecutableKinds.Scipy);

    /// <summary>
    /// Generic runtime resolver. Order of resolution:
    /// <list type="number">
    ///   <item><see cref="RuntimePythons"/> entry whose key matches <paramref name="runtimeKey"/>
    ///     case-insensitively and whose value is non-blank.</item>
    ///   <item>The compat field for known built-in keys:
    ///     <c>system</c> → <see cref="SystemPython"/>;
    ///     <c>openmoc</c> → <see cref="OpenMocPython"/> (falls back to <see cref="SystemPython"/> when blank);
    ///     <c>openmc</c> → <see cref="OpenMcPython"/> (falls back to <see cref="SystemPython"/> when blank);
    ///     <c>scipy</c> → <see cref="ScipyPython"/> (falls back to <see cref="SystemPython"/> when blank).</item>
    ///   <item>Blank or null <paramref name="runtimeKey"/> → <see cref="SystemPython"/>.</item>
    ///   <item>Otherwise fail closed with <see cref="RuntimeEnvironmentResolutionException"/>,
    ///     naming the missing key.</item>
    /// </list>
    /// </summary>
    /// <param name="runtimeKey">
    /// Manifest <c>python_executable_kind</c> value (e.g. <c>"openmoc"</c>, <c>"fenics"</c>).
    /// Null, empty, or whitespace is treated as the <c>system</c> key.
    /// </param>
    /// <param name="fallback">
    /// Optional fallback executable for unknown non-system keys. When supplied and the key
    /// resolves to neither a <see cref="RuntimePythons"/> entry nor a built-in compat field,
    /// the fallback is returned instead of throwing. Used by code paths that have a sensible
    /// last-resort executable (currently none in production; reserved for future use).
    /// </param>
    public string ResolvePythonExecutable(string? runtimeKey, string? fallback = null)
    {
        var key = string.IsNullOrWhiteSpace(runtimeKey)
            ? PythonExecutableKinds.System
            : runtimeKey.Trim();

        if (RuntimePythons is not null)
        {
            foreach (var pair in RuntimePythons)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(pair.Value))
                {
                    return pair.Value;
                }
            }
        }

        if (string.Equals(key, PythonExecutableKinds.System, StringComparison.OrdinalIgnoreCase))
            return SystemPython;
        if (string.Equals(key, PythonExecutableKinds.OpenMoc, StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(OpenMocPython) ? SystemPython : OpenMocPython;
        if (string.Equals(key, PythonExecutableKinds.OpenMc, StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(OpenMcPython) ? SystemPython : OpenMcPython!;
        if (string.Equals(key, PythonExecutableKinds.Scipy, StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(ScipyPython) ? SystemPython : ScipyPython!;

        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback;

        throw new RuntimeEnvironmentResolutionException(
            $"Runtime python for key '{key}' is not configured. " +
            $"Add an entry to LauncherOptions.RuntimePythons (e.g. RuntimePythons[\"{key}\"] = \"/path/to/python\") " +
            "or set the equivalent environment variable in WPF DI.");
    }
}
