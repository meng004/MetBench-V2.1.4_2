namespace MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;

/// <summary>
/// Top-level manifest for a batch execution-artifact export. One <see cref="Items"/> entry per
/// requested execution; failures are recorded per item (export continues on error) so the
/// caller can see exactly which executions exported and which did not.
/// </summary>
public sealed record ExecutionArtifactBatchManifest(
    Guid JobId,
    DateTime ExportedAtUtc,
    IReadOnlyList<ExecutionArtifactBatchItem> Items)
{
    public bool AllSucceeded => Items.All(i => i.Succeeded);
    public int FailureCount => Items.Count(i => !i.Succeeded);
}

/// <summary>One execution's result inside an <see cref="ExecutionArtifactBatchManifest"/>.</summary>
public sealed record ExecutionArtifactBatchItem(
    Guid ExecutionId,
    bool Succeeded,
    string? ManifestRelativePath,
    string? Error);
