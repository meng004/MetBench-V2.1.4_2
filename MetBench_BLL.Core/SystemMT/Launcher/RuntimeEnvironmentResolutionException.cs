using System;

namespace MetBench_BLL.SystemMT.Launcher;

/// <summary>
/// Thrown when <see cref="LauncherOptions.ResolvePythonExecutable"/> cannot map a manifest
/// runtime key (e.g. <c>"fenics"</c>) to a configured Python executable. Surfaces the
/// missing key in the diagnostic so a manifest author can see exactly which
/// <see cref="LauncherOptions.RuntimePythons"/> entry needs to be supplied.
/// </summary>
public sealed class RuntimeEnvironmentResolutionException : InvalidOperationException
{
    public RuntimeEnvironmentResolutionException(string message) : base(message)
    {
    }
}
