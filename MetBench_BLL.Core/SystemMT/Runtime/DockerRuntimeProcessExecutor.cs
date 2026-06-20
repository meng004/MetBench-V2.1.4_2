using MetBench_BLL.SystemMT.Pipeline;

namespace MetBench_BLL.SystemMT.Runtime;

public sealed class DockerRuntimeProcessExecutor : IRuntimeProcessExecutor
{
    private readonly DockerMcpProcessExecutor _processExecutor;

    public DockerRuntimeProcessExecutor(DockerMcpProcessExecutor? processExecutor = null)
    {
        _processExecutor = processExecutor ?? new DockerMcpProcessExecutor();
    }

    public Task<ProcessResult> RunAsync(
        RuntimeProfile? profile,
        ProcessInvocation invocation,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (profile?.DockerMcp is null)
        {
            var runtimeKey = profile?.RuntimeKey ?? "<missing>";
            throw new InvalidOperationException(
                $"Docker runtime profile '{runtimeKey}' has no Docker MCP options.");
        }

        return _processExecutor.RunAsync(
            profile.DockerMcp,
            invocation,
            timeoutSeconds,
            cancellationToken);
    }
}
