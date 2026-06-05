using System.Text.Json.Serialization;

namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public sealed record SutImportStagingManifest(
    string SutId,
    string PackageId,
    string SourcePackageRoot,
    string StagedPackageRoot,
    DateTime StagedAtUtc)
{
    [JsonIgnore]
    public const string FileName = "staging-manifest.json";
}
