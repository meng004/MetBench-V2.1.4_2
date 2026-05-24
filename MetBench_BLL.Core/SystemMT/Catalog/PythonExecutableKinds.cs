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

    public static readonly IReadOnlyList<string> All = new[] { System, OpenMoc, OpenMc };
}
