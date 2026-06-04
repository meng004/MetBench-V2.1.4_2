using System.Diagnostics;
using System.Text;

namespace MetBench_SystemMT.Tests.SystemMT.ImportExport;

internal sealed record MinimumMrSubsetExternalPrerequisites(
    string Root,
    MinimumMrSubsetPythonCommand Python,
    string Commit,
    string DependencyVersions);

internal sealed record MinimumMrSubsetPythonCommand(string FileName, string[] PrefixArguments)
{
    public string DisplayName =>
        PrefixArguments.Length == 0
            ? FileName
            : FileName + " " + string.Join(" ", PrefixArguments);
}

internal sealed record MinimumMrSubsetCommandResult(
    int ExitCode,
    string CommandLine,
    string StandardOutput,
    string StandardError)
{
    public string CombinedOutput
    {
        get
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(StandardOutput))
            {
                builder.Append("STDOUT: ").Append(StandardOutput.Trim());
            }

            if (!string.IsNullOrWhiteSpace(StandardError))
            {
                if (builder.Length > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append("STDERR: ").Append(StandardError.Trim());
            }

            return builder.Length == 0 ? "<no output>" : builder.ToString();
        }
    }
}

internal static class MinimumMrSubsetExternalTestPaths
{
    private static readonly string[] RequiredRelativeFiles =
    [
        Path.Combine("experiments", "puts", "p3_lorenz.py"),
        Path.Combine("experiments", "puts", "p8_schrodinger.py"),
        Path.Combine("tests", "puts", "test_smoke.py")
    ];

    public static string? ResolveRoot()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("MINIMUM_MR_SUBSET_ROOT"),
            @"C:\tmp\Minimum-MR-SubSet",
            @"C:\tmp\minimum-mr-subset",
            @"C:\Users\limeng\Codes\Minimum-MR-SubSet",
            @"C:\Users\limeng\Codes\minimum-mr-subset",
            @"D:\Codes\Minimum-MR-SubSet",
            "/private/tmp/minimum-mr-subset",
            "/private/tmp/Minimum-MR-SubSet",
            "/tmp/minimum-mr-subset",
            "/tmp/Minimum-MR-SubSet"
        };

        return candidates
            .OfType<string>()
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(Path.GetFullPath)
            .FirstOrDefault(Directory.Exists);
    }

    public static IReadOnlyList<string> FindMissingRequiredFiles(string root)
    {
        return RequiredRelativeFiles
            .Select(relativePath => Path.Combine(root, relativePath))
            .Where(path => !File.Exists(path))
            .ToArray();
    }

    public static MinimumMrSubsetPythonCommand? ResolvePython()
    {
        var configured = Environment.GetEnvironmentVariable("MINIMUM_MR_SUBSET_PYTHON");
        var candidates = new List<MinimumMrSubsetPythonCommand>();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            candidates.Add(new MinimumMrSubsetPythonCommand(configured, []));
        }

        candidates.Add(new MinimumMrSubsetPythonCommand("python3", []));
        candidates.Add(new MinimumMrSubsetPythonCommand("python", []));
        candidates.Add(new MinimumMrSubsetPythonCommand("py", ["-3"]));

        return candidates.FirstOrDefault(candidate =>
            RunPython(
                Directory.GetCurrentDirectory(),
                candidate,
                ["-c", "import sys; print(sys.executable); print(sys.version)"],
                TimeSpan.FromSeconds(10)).ExitCode == 0);
    }

    public static MinimumMrSubsetCommandResult ReadGitCommit(string root)
    {
        return RunProcess("git", ["-C", root, "rev-parse", "HEAD"], root, TimeSpan.FromSeconds(10));
    }

    public static MinimumMrSubsetCommandResult ReadGitStatus(string root)
    {
        return RunProcess("git", ["-C", root, "status", "--short", "--branch"], root, TimeSpan.FromSeconds(10));
    }

    public static MinimumMrSubsetCommandResult CheckDependencies(string root, MinimumMrSubsetPythonCommand python)
    {
        const string script =
            "import numpy, scipy, pytest; " +
            "print('numpy=' + numpy.__version__); " +
            "print('scipy=' + scipy.__version__); " +
            "print('pytest=' + pytest.__version__)";
        return RunPython(root, python, ["-c", script], TimeSpan.FromSeconds(30));
    }

    public static MinimumMrSubsetCommandResult RunPython(
        string workingDirectory,
        MinimumMrSubsetPythonCommand python,
        IReadOnlyList<string> arguments,
        TimeSpan timeout)
    {
        return RunProcess(
            python.FileName,
            [.. python.PrefixArguments, .. arguments],
            workingDirectory,
            timeout);
    }

    private static MinimumMrSubsetCommandResult RunProcess(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        var commandLine = FormatCommandLine(fileName, arguments);
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                return new MinimumMrSubsetCommandResult(
                    -1,
                    commandLine,
                    stdoutTask.GetAwaiter().GetResult(),
                    $"Timed out after {timeout.TotalSeconds:0} seconds.");
            }

            return new MinimumMrSubsetCommandResult(
                process.ExitCode,
                commandLine,
                stdoutTask.GetAwaiter().GetResult(),
                stderrTask.GetAwaiter().GetResult());
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return new MinimumMrSubsetCommandResult(-1, commandLine, string.Empty, ex.Message);
        }
    }

    private static string FormatCommandLine(string fileName, IReadOnlyList<string> arguments)
    {
        return string.Join(" ", new[] { fileName }.Concat(arguments.Select(QuoteIfNeeded)));
    }

    private static string QuoteIfNeeded(string value)
    {
        return value.Any(char.IsWhiteSpace) ? "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"" : value;
    }
}
