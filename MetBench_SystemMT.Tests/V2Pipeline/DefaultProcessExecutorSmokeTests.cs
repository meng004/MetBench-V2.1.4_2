using MetBench_BLL.SystemMT.Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pipeline;

/// <summary>
/// G1 smoke：确认 <see cref="DefaultProcessExecutor"/> 真正能 spawn 子进程并捕获 stdout/stderr。
/// 跨平台：用 echo（Windows: cmd /c echo / Linux: /bin/sh -c echo）。
/// </summary>
public sealed class DefaultProcessExecutorSmokeTests
{
    [Fact]
    public async Task RunAsync_executes_echo_and_captures_stdout()
    {
        var exec = new DefaultProcessExecutor();
        var result = await exec.RunAsync(
            command: "echo metbench-smoke",
            workingDirectory: Path.GetTempPath(),
            timeoutSeconds: 5,
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("metbench-smoke", result.Stdout);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_propagates_nonzero_exit_code()
    {
        var exec = new DefaultProcessExecutor();
        // exit 7 is portable across cmd and /bin/sh
        var cmd = OperatingSystem.IsWindows() ? "exit 7" : "exit 7";
        var result = await exec.RunAsync(cmd, Path.GetTempPath(), 5, CancellationToken.None);
        Assert.Equal(7, result.ExitCode);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_times_out_when_command_runs_too_long()
    {
        var exec = new DefaultProcessExecutor();
        // sleep 5 (Linux) / timeout 5 (Windows). 给 1 秒预算，必超时。
        var cmd = OperatingSystem.IsWindows() ? "ping -n 6 127.0.0.1 > nul" : "sleep 5";
        var result = await exec.RunAsync(cmd, Path.GetTempPath(),
            timeoutSeconds: 1, cancellationToken: CancellationToken.None);

        Assert.True(result.TimedOut);
        Assert.Equal(-1, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_captures_stderr_separately_from_stdout()
    {
        var exec = new DefaultProcessExecutor();
        // 跨 sh / cmd 都 work：write 到 stderr
        var cmd = OperatingSystem.IsWindows()
            ? "echo only-err 1>&2"
            : "echo only-err 1>&2";
        var result = await exec.RunAsync(cmd, Path.GetTempPath(), 5, CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("only-err", result.Stderr);
        Assert.DoesNotContain("only-err", result.Stdout);
    }
}
