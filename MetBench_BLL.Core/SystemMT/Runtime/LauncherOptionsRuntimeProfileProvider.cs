using System;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Runtime;

public sealed class LauncherOptionsRuntimeProfileProvider : IRuntimeProfileProvider
{
    private readonly LauncherOptions _options;

    public LauncherOptionsRuntimeProfileProvider(LauncherOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public RuntimeProfile GetProfile(string? runtimeKey)
    {
        var key = RuntimeProfile.NormalizeRuntimeKey(runtimeKey);
        var executable = _options.ResolvePythonExecutable(key);

        return new RuntimeProfile(
            key,
            DisplayNameFor(key),
            KindFor(key),
            executable);
    }

    private static RuntimeKind KindFor(string runtimeKey) =>
        string.Equals(runtimeKey, PythonExecutableKinds.OpenMoc, StringComparison.OrdinalIgnoreCase)
        || string.Equals(runtimeKey, PythonExecutableKinds.OpenMc, StringComparison.OrdinalIgnoreCase)
        || string.Equals(runtimeKey, PythonExecutableKinds.Scipy, StringComparison.OrdinalIgnoreCase)
            ? RuntimeKind.PythonVirtualEnvironment
            : RuntimeKind.LocalPython;

    private static string DisplayNameFor(string runtimeKey)
    {
        if (string.Equals(runtimeKey, PythonExecutableKinds.System, StringComparison.OrdinalIgnoreCase))
            return "System Python";
        if (string.Equals(runtimeKey, PythonExecutableKinds.OpenMoc, StringComparison.OrdinalIgnoreCase))
            return "OpenMOC Python";
        if (string.Equals(runtimeKey, PythonExecutableKinds.OpenMc, StringComparison.OrdinalIgnoreCase))
            return "OpenMC Python";
        if (string.Equals(runtimeKey, PythonExecutableKinds.Scipy, StringComparison.OrdinalIgnoreCase))
            return "SciPy Python";

        return $"{runtimeKey} Python";
    }
}
