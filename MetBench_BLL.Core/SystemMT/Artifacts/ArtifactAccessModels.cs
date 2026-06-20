namespace MetBench_BLL.Core.SystemMT.Artifacts;

public sealed record SystemMtArtifactDescriptor(
    string ArtifactId,
    string FileName,
    long Length,
    string ContentType);

public sealed record SystemMtArtifactContent(
    string ArtifactId,
    string FileName,
    string ContentType,
    byte[] Content);
