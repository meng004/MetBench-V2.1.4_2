namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// 子进程执行器抽象 — 让 Pipeline 在测试时用 fake，生产时用真实 Process。
/// </summary>
public interface IProcessExecutor
{
    Task<ProcessResult> RunAsync(
        string command,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken);
}

public sealed record ProcessResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    TimeSpan Elapsed,
    bool TimedOut);
