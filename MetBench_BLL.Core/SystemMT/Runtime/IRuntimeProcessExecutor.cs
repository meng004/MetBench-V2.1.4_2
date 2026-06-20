using MetBench_BLL.SystemMT.Pipeline;

namespace MetBench_BLL.SystemMT.Runtime;

public interface IRuntimeProcessExecutor
{
    Task<ProcessResult> RunAsync(
        RuntimeProfile? profile,
        ProcessInvocation invocation,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken);
}
