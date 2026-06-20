using MetBench_BLL.SystemMT.Pipeline;

namespace MetBench_BLL.SystemMT.Runtime;

public sealed class RuntimeProcessExecutorRegistry : IRuntimeProcessExecutor
{
    private readonly IRuntimeProcessExecutor _localExecutor;
    private readonly IRuntimeProcessExecutor _dockerExecutor;

    public RuntimeProcessExecutorRegistry(
        IRuntimeProcessExecutor? localExecutor = null,
        IRuntimeProcessExecutor? dockerExecutor = null)
    {
        _localExecutor = localExecutor ?? new LocalRuntimeProcessExecutor();
        _dockerExecutor = dockerExecutor ?? new DockerRuntimeProcessExecutor();
    }

    public Task<ProcessResult> RunAsync(
        RuntimeProfile? profile,
        ProcessInvocation invocation,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (profile is null)
        {
            return _localExecutor.RunAsync(
                profile,
                invocation,
                workingDirectory,
                timeoutSeconds,
                cancellationToken);
        }

        return profile.Kind switch
        {
            RuntimeKind.LocalPython or RuntimeKind.PythonVirtualEnvironment =>
                _localExecutor.RunAsync(profile, invocation, workingDirectory, timeoutSeconds, cancellationToken),
            RuntimeKind.Docker =>
                _dockerExecutor.RunAsync(profile, invocation, workingDirectory, timeoutSeconds, cancellationToken),
            _ => throw new NotSupportedException(
                $"Runtime kind '{profile.Kind}' is not supported by the runtime process executor registry."),
        };
    }
}
