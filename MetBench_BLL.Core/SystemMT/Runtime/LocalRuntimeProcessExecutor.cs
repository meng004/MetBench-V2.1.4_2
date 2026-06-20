using MetBench_BLL.SystemMT.Pipeline;

namespace MetBench_BLL.SystemMT.Runtime;

public sealed class LocalRuntimeProcessExecutor : IRuntimeProcessExecutor
{
    private readonly IProcessExecutor _processExecutor;

    public LocalRuntimeProcessExecutor(IProcessExecutor? processExecutor = null)
    {
        _processExecutor = processExecutor ?? new DefaultProcessExecutor();
    }

    public Task<ProcessResult> RunAsync(
        RuntimeProfile? profile,
        ProcessInvocation invocation,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken) =>
        _processExecutor.RunAsync(invocation, workingDirectory, timeoutSeconds, cancellationToken);
}
