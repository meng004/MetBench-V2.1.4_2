using System.Diagnostics;

namespace MetBench_BLL.SystemMT;

public sealed class CliProgramRunner
{
    public async Task<CliRunResult> RunAsync(
        SystemProgram program,
        SystemMtCase testCase,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(testCase.InputPath))
        {
            return Failed(testCase, -1, string.Empty, string.Empty, TimeSpan.Zero,
                $"Configuration failure: Input file does not exist for case '{testCase.CaseName}': {testCase.InputPath}");
        }

        Directory.CreateDirectory(testCase.WorkingDirectory);
        var arguments = BuildArguments(program.ArgumentTemplate, testCase);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = program.ExecutablePath,
            Arguments = arguments,
            WorkingDirectory = testCase.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var item in testCase.EnvironmentVariables)
        {
            process.StartInfo.Environment[item.Key] = item.Value;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            var completed = await WaitForExitAsync(process, timeout, cancellationToken);
            stopwatch.Stop();
            if (!completed)
            {
                TryKill(process);
                return Failed(testCase, -1, await stdoutTask, await stderrTask, stopwatch.Elapsed,
                    $"CLI execution failure: case '{testCase.CaseName}' timed out after {timeout.TotalSeconds:0.###} seconds");
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var exitCodeAccepted = program.AcceptableExitCodes.Contains(process.ExitCode);
            if (!exitCodeAccepted)
            {
                return Failed(testCase, process.ExitCode, stdout, stderr, stopwatch.Elapsed,
                    $"CLI execution failure: case '{testCase.CaseName}' exited with code {process.ExitCode}");
            }

            if (!File.Exists(testCase.OutputPath))
            {
                return Failed(testCase, process.ExitCode, stdout, stderr, stopwatch.Elapsed,
                    $"Output artifact failure: expected output file is missing for case '{testCase.CaseName}': {testCase.OutputPath}");
            }

            return new CliRunResult(
                testCase.CaseName,
                process.ExitCode,
                stdout,
                stderr,
                stopwatch.Elapsed,
                testCase.OutputPath,
                true,
                string.Empty);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Failed(testCase, -1, string.Empty, ex.ToString(), stopwatch.Elapsed,
                $"CLI execution failure: case '{testCase.CaseName}' could not start: {ex.Message}");
        }
    }

    private static string BuildArguments(string argumentTemplate, SystemMtCase testCase)
    {
        return argumentTemplate
            .Replace("{input}", Quote(testCase.InputPath), StringComparison.Ordinal)
            .Replace("{output}", Quote(testCase.OutputPath), StringComparison.Ordinal);
    }

    private static string Quote(string value)
    {
        return value.Contains(' ') ? $"\"{value}\"" : value;
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var exitTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cancellationToken);
        return await Task.WhenAny(exitTask, timeoutTask) == exitTask;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Failure to kill is already represented as a timeout failure.
        }
    }

    private static CliRunResult Failed(
        SystemMtCase testCase,
        int exitCode,
        string stdout,
        string stderr,
        TimeSpan elapsed,
        string reason)
    {
        return new CliRunResult(testCase.CaseName, exitCode, stdout, stderr, elapsed, testCase.OutputPath, false, reason);
    }
}
