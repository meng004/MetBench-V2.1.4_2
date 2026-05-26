using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Catalog;

/// <summary>
/// Built-in runtime keys recognized by <see cref="MetBench_BLL.SystemMT.Launcher.LauncherOptions"/>
/// for backwards compatibility. These constants are kept for legacy callers that switched on
/// <c>python_executable_kind</c> directly; manifest authors may also use them.
/// </summary>
/// <remarks>
/// Important: this is NOT a closed vocabulary. New SUT runtime families (e.g. <c>fenics</c>,
/// <c>fipy</c>, <c>torch-surrogate</c>) are introduced by adding an entry to
/// <see cref="MetBench_BLL.SystemMT.Launcher.LauncherOptions.RuntimePythons"/> — no new constant
/// here, no new <c>LauncherOptions</c> field. <see cref="All"/> remains only for parity tests
/// that need to enumerate the legacy compat-field set; it must not be used to reject manifest
/// runtime keys that are not in the list. Fail-closed behaviour for unknown non-system keys is
/// owned by <see cref="MetBench_BLL.SystemMT.Launcher.LauncherOptions.ResolvePythonExecutable"/>.
/// </remarks>
public static class PythonExecutableKinds
{
    /// <summary>System python (used by lightweight ODE SUTs: decay-chain, projectile, etc.).</summary>
    public const string System = "system";

    /// <summary>OpenMOC-aware venv (resolved via LauncherOptions.OpenMocPython compat field or RuntimePythons["openmoc"]).</summary>
    public const string OpenMoc = "openmoc";

    /// <summary>OpenMC-aware venv (resolved via LauncherOptions.EffectiveOpenMcPython compat path or RuntimePythons["openmc"]).</summary>
    public const string OpenMc = "openmc";

    /// <summary>SciPy-aware venv (resolved via LauncherOptions.EffectiveScipyPython compat path or RuntimePythons["scipy"]); used by T3C-IVP / T3C-BVP external-solver-pilot SUTs.</summary>
    public const string Scipy = "scipy";

    /// <summary>
    /// Built-in runtime keys with a dedicated compat field on <see cref="MetBench_BLL.SystemMT.Launcher.LauncherOptions"/>.
    /// Future runtime families do NOT need to be added here — they are configured via
    /// <see cref="MetBench_BLL.SystemMT.Launcher.LauncherOptions.RuntimePythons"/>. Kept for
    /// legacy parity tests only.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[] { System, OpenMoc, OpenMc, Scipy };
}
