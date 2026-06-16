using System.ComponentModel;
using System.Diagnostics;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// Executes a child process directly with structured argv. Shells are not used.
/// </summary>
public sealed class DefaultProcessExecutor : IProcessExecutor
{
    public async Task<ProcessResult> RunAsync(
        ProcessInvocation invocation,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        if (string.IsNullOrWhiteSpace(invocation.FileName))
            throw new ArgumentException("Executable file name is required.", nameof(invocation));

        var psi = new ProcessStartInfo
        {
            FileName = invocation.FileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in invocation.Arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        var sw = Stopwatch.StartNew();
        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            sw.Stop();
            return new ProcessResult(
                ExitCode: -1,
                Stdout: string.Empty,
                Stderr: ex.ToString(),
                Elapsed: sw.Elapsed,
                TimedOut: false);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryKillProcessTree(process);
            await DrainAfterKillAsync(stdoutTask, stderrTask).ConfigureAwait(false);
            throw new OperationCanceledException(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            TryKillProcessTree(process);
        }

        var stdout = timedOut
            ? await ReadAfterKillAsync(stdoutTask).ConfigureAwait(false)
            : await stdoutTask.ConfigureAwait(false);
        var stderr = timedOut
            ? await ReadAfterKillAsync(stderrTask).ConfigureAwait(false)
            : await stderrTask.ConfigureAwait(false);
        sw.Stop();

        return new ProcessResult(
            ExitCode: timedOut ? -1 : process.ExitCode,
            Stdout: stdout,
            Stderr: stderr,
            Elapsed: sw.Elapsed,
            TimedOut: timedOut);
    }

    private static void TryKillProcessTree(Process process)
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
            // Timeout/cancellation handling is already the reported outcome.
        }
    }

    private static async Task DrainAfterKillAsync(Task<string> stdoutTask, Task<string> stderrTask)
    {
        await ReadAfterKillAsync(stdoutTask).ConfigureAwait(false);
        await ReadAfterKillAsync(stderrTask).ConfigureAwait(false);
    }

    private static async Task<string> ReadAfterKillAsync(Task<string> task)
    {
        try
        {
            return await task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        catch
        {
            return string.Empty;
        }
    }
}
