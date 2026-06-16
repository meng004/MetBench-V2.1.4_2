namespace MetBench_SystemMT.Tests.SystemMT;

internal static class TestPythonExecutableResolver
{
    public static string Resolve(string? configured, bool isWindows, Func<string, bool> commandExists)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var candidates = isWindows
            ? new[] { "python", "python3", "py" }
            : new[] { "python3", "python" };

        foreach (var candidate in candidates)
        {
            if (commandExists(candidate))
            {
                return candidate;
            }
        }

        return isWindows ? "python" : "python3";
    }

    public static bool CommandExists(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        if (Path.IsPathRooted(command)
            || command.Contains(Path.DirectorySeparatorChar)
            || command.Contains(Path.AltDirectorySeparatorChar))
        {
            return File.Exists(command);
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extensions = OperatingSystem.IsWindows()
            ? GetWindowsExecutableExtensions(command)
            : new[] { string.Empty };

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(directory, command + extension);
                if (File.Exists(candidate))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string[] GetWindowsExecutableExtensions(string command)
    {
        if (Path.HasExtension(command))
        {
            return new[] { string.Empty };
        }

        var configured = Environment.GetEnvironmentVariable("PATHEXT");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return new[] { string.Empty, ".EXE", ".CMD", ".BAT" };
        }

        return configured
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Prepend(string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
