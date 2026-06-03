namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 执行后端 seam（设计 spec §8）。v1 只定义接口，**不接** Docker/remote/HPC 实现
/// —— 见 cloud-plan §0 架构决策待清理项（<c>DockerBackend</c> / <c>RemoteServerBackend</c> /
/// <c>HpcQueueBackend</c>）。v1 的 <c>SystemMtAsyncPipeline</c> 直接委托
/// <c>ISystemMtLauncher</c>，不经过本接口。保留此 seam 供后续后端接入。
/// </summary>
public interface ISutExecutionBackend
{
    Task<SutRunHandle> SubmitAsync(SutExecutionRequest request, CancellationToken cancellationToken);
    Task<SutRunStatus> GetStatusAsync(SutRunHandle handle, CancellationToken cancellationToken);
    Task<SutRunArtifacts> FetchArtifactsAsync(SutRunHandle handle, CancellationToken cancellationToken);
    Task CancelAsync(SutRunHandle handle, CancellationToken cancellationToken);
}

public sealed record SutExecutionRequest(string SutName, string WorkingDirectory, int TimeoutSeconds);
public sealed record SutRunHandle(string BackendKind, string ExternalId);
public sealed record SutRunStatus(bool Completed, bool Faulted, string? Diagnostic);
public sealed record SutRunArtifacts(bool AllPresent, IReadOnlyList<string> MissingPaths);
