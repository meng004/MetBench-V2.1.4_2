using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pipeline;

public sealed class DefaultProcessExecutorSmokeTests
{
    [Fact]
    public async Task RunAsync_executes_argv_without_shell_interpretation()
    {
        var workDir = Path.Combine(
            Path.GetTempPath(),
            "MetBenchArgvExecutorSmoke",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var scriptPath = Path.Combine(workDir, "argv echo.py");
            await File.WriteAllTextAsync(
                scriptPath,
                """
                import sys
                print(sys.argv[1])
                """);

            var exec = new DefaultProcessExecutor();
            var result = await exec.RunAsync(
                new ProcessInvocation(
                    TestAssetPaths.PythonExecutable(),
                    new[] { scriptPath, "literal && exit 9" }),
                workDir,
                timeoutSeconds: 5,
                CancellationToken.None);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("literal && exit 9", result.Stdout);
            Assert.False(result.TimedOut);
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { /* test cleanup best effort */ }
        }
    }

    [Fact]
    public async Task RunAsync_executes_process_and_captures_stdout()
    {
        var exec = new DefaultProcessExecutor();
        var result = await exec.RunAsync(
            new ProcessInvocation(
                TestAssetPaths.PythonExecutable(),
                new[] { "-c", "print('metbench-smoke')" }),
            Path.GetTempPath(),
            timeoutSeconds: 5,
            CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("metbench-smoke", result.Stdout);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_returns_process_result_when_executable_is_missing()
    {
        var exec = new DefaultProcessExecutor();
        var missingExecutable = Path.Combine(
            Path.GetTempPath(),
            $"metbench-missing-{Guid.NewGuid():N}");

        var result = await exec.RunAsync(
            new ProcessInvocation(missingExecutable, Array.Empty<string>()),
            Path.GetTempPath(),
            timeoutSeconds: 5,
            CancellationToken.None);

        Assert.Equal(-1, result.ExitCode);
        Assert.Empty(result.Stdout);
        Assert.Contains(Path.GetFileName(missingExecutable), result.Stderr);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_propagates_nonzero_exit_code()
    {
        var exec = new DefaultProcessExecutor();
        var result = await exec.RunAsync(
            new ProcessInvocation(
                TestAssetPaths.PythonExecutable(),
                new[] { "-c", "import sys; sys.exit(7)" }),
            Path.GetTempPath(),
            timeoutSeconds: 5,
            CancellationToken.None);

        Assert.Equal(7, result.ExitCode);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_times_out_when_process_runs_too_long()
    {
        var exec = new DefaultProcessExecutor();
        var result = await exec.RunAsync(
            new ProcessInvocation(
                TestAssetPaths.PythonExecutable(),
                new[] { "-c", "import time; time.sleep(5)" }),
            Path.GetTempPath(),
            timeoutSeconds: 1,
            CancellationToken.None);

        Assert.True(result.TimedOut);
        Assert.Equal(-1, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_captures_stderr_separately_from_stdout()
    {
        var exec = new DefaultProcessExecutor();
        var result = await exec.RunAsync(
            new ProcessInvocation(
                TestAssetPaths.PythonExecutable(),
                new[] { "-c", "import sys; print('only-err', file=sys.stderr)" }),
            Path.GetTempPath(),
            timeoutSeconds: 5,
            CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("only-err", result.Stderr);
        Assert.DoesNotContain("only-err", result.Stdout);
    }
}
