namespace MetBench_BLL.Core.SystemMT.Artifacts;

public interface ISystemMtArtifactAccessService
{
    Task<IReadOnlyList<SystemMtArtifactDescriptor>> ListAsync(
        string manifestPath,
        CancellationToken cancellationToken = default);

    Task<SystemMtArtifactContent> ReadAsync(
        string manifestPath,
        string artifactId,
        CancellationToken cancellationToken = default);
}
