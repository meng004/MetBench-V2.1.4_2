namespace MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;

public sealed record ExecutionArtifactExportManifest(
    Guid ExecutionId,
    Guid JobId,
    DateTime ExportedAtUtc,
    string[] Files)
{
    public const string FileName = "manifest.json";
}
