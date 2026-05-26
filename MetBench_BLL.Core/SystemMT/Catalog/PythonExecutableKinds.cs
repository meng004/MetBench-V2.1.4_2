using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// Closed vocabulary of Python executables a SUT can target.
/// Mirrors <c>LauncherOptions.SystemPython / OpenMocPython / EffectiveOpenMcPython</c>.
/// </summary>
public static class PythonExecutableKinds
{
    /// <summary>System python (used by lightweight ODE SUTs: decay-chain, projectile, etc.).</summary>
    public const string System = "system";

    /// <summary>OpenMOC-aware venv (resolved via LauncherOptions.OpenMocPython).</summary>
    public const string OpenMoc = "openmoc";

    /// <summary>OpenMC-aware venv (resolved via LauncherOptions.EffectiveOpenMcPython).</summary>
    public const string OpenMc = "openmc";

    /// <summary>SciPy-aware venv (resolved via LauncherOptions.EffectiveScipyPython); used by T3C-IVP / T3C-BVP external-solver-pilot SUTs.</summary>
    public const string Scipy = "scipy";

    public static readonly IReadOnlyList<string> All = new[] { System, OpenMoc, OpenMc, Scipy };
}
