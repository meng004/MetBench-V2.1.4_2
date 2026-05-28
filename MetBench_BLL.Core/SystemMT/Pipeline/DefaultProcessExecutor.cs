using System.Diagnostics;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// 真实子进程执行器 — 用 <see cref="Process"/> 跑命令；带超时 + stderr / stdout 捕获。
/// </summary>
public sealed class DefaultProcessExecutor : IProcessExecutor
{
    public async Task<ProcessResult> RunAsync(
        string command,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        // 简化：command 是单个 shell 命令字符串；用 sh -c (Linux/Mac) 或 cmd /c (Windows) 执行
        var (fileName, arguments) = PlatformShell(command);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var sw = Stopwatch.StartNew();
        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        bool timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            try { process.Kill(entireProcessTree: true); } catch { /* swallow */ }
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        sw.Stop();

        return new ProcessResult(
            ExitCode: timedOut ? -1 : process.ExitCode,
            Stdout: stdout,
            Stderr: stderr,
            Elapsed: sw.Elapsed,
            TimedOut: timedOut);
    }

    private static (string FileName, string Arguments) PlatformShell(string command)
    {
        if (OperatingSystem.IsWindows())
        {
            // cmd.exe /c with a command containing multiple quoted tokens
            // (e.g. `"python" "C:\path\script.py" parse --input "C:\path\in.json"`)
            // triggers cmd's documented rule-2 quote-stripping (`cmd /?`):
            // > if more than 2 quotes are present, strip the leading and trailing
            // > quote chars from the command, leaving the middle intact.
            // That mangles the command into `python" "C:\path\script.py" ...` and
            // cmd prints `系统找不到指定的路径` on stderr. Pass `/s /c "<command>"`
            // so cmd treats the entire command as one quoted block and only strips
            // the outer pair we explicitly add.
            return ("cmd.exe", $"/s /c \"{command}\"");
        }
        return ("/bin/sh", $"-c \"{command.Replace("\"", "\\\"")}\"");
    }
}
