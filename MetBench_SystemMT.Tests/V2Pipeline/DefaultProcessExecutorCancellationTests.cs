using System.Diagnostics;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pipeline;

public sealed class DefaultProcessExecutorCancellationTests : IDisposable
{
    private readonly string _workDir = Path.Combine(
        Path.GetTempPath(),
        "MetBenchDefaultProcessExecutorCancellation",
        Guid.NewGuid().ToString("N"));

    public DefaultProcessExecutorCancellationTests()
    {
        Directory.CreateDirectory(_workDir);
    }

    [Fact]
    public async Task RunAsync_user_cancellation_kills_process_tree()
    {
        var script = WriteSleepingPidScript();
        var pidFile = Path.Combine(_workDir, "cancel.pid");
        var command = $"{Quote(TestAssetPaths.PythonExecutable())} {Quote(script)} {Quote(pidFile)}";
        using var cts = new CancellationTokenSource();

        var runTask = new DefaultProcessExecutor().RunAsync(command, _workDir, timeoutSeconds: 30, cts.Token);
        var pid = await WaitForPidFileAsync(pidFile);

        try
        {
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
            await AssertProcessExitedAsync(pid);
        }
        finally
        {
            TryKill(pid);
        }
    }

    [Fact]
    public async Task RunAsync_timeout_kills_process_tree_and_returns_timed_out()
    {
        var script = WriteSleepingPidScript();
        var pidFile = Path.Combine(_workDir, "timeout.pid");
        var command = $"{Quote(TestAssetPaths.PythonExecutable())} {Quote(script)} {Quote(pidFile)}";

        var result = await new DefaultProcessExecutor().RunAsync(
            command,
            _workDir,
            timeoutSeconds: 1,
            CancellationToken.None);

        var pid = await WaitForPidFileAsync(pidFile);
        try
        {
            Assert.True(result.TimedOut);
            Assert.Equal(-1, result.ExitCode);
            await AssertProcessExitedAsync(pid);
        }
        finally
        {
            TryKill(pid);
        }
    }

    [Fact]
    public async Task RunAsync_normal_exit_preserves_stdout_and_exit_code()
    {
        var script = Path.Combine(_workDir, "normal_exit.py");
        await File.WriteAllTextAsync(
            script,
            """
            import sys
            print("normal-output")
            sys.exit(3)
            """);

        var command = $"{Quote(TestAssetPaths.PythonExecutable())} {Quote(script)}";

        var result = await new DefaultProcessExecutor().RunAsync(
            command,
            _workDir,
            timeoutSeconds: 5,
            CancellationToken.None);

        Assert.Equal(3, result.ExitCode);
        Assert.Contains("normal-output", result.Stdout);
        Assert.False(result.TimedOut);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_workDir))
            {
                Directory.Delete(_workDir, recursive: true);
            }
        }
        catch
        {
            // Temp cleanup failure should not hide process-control assertions.
        }
    }

    private string WriteSleepingPidScript()
    {
        var script = Path.Combine(_workDir, "sleeping_pid.py");
        File.WriteAllText(
            script,
            """
            import os
            import sys
            import time

            with open(sys.argv[1], "w", encoding="utf-8") as handle:
                handle.write(str(os.getpid()))
                handle.flush()

            time.sleep(30)
            """);
        return script;
    }

    private static async Task<int> WaitForPidFileAsync(string pidFile)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (File.Exists(pidFile))
            {
                var text = await File.ReadAllTextAsync(pidFile);
                if (int.TryParse(text.Trim(), out var pid))
                {
                    return pid;
                }
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"Timed out waiting for PID file: {pidFile}");
    }

    private static async Task AssertProcessExitedAsync(int pid)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (!IsProcessAlive(pid))
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.False(IsProcessAlive(pid), $"Process {pid} remained alive after cancellation/timeout.");
    }

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static void TryKill(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort cleanup for failed process-control tests.
        }
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
