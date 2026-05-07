namespace MetBench_BLL.SystemMT;

public sealed record CliRunResult(
    string CaseName,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Elapsed,
    string OutputPath,
    bool Succeeded,
    string FailureReason);
