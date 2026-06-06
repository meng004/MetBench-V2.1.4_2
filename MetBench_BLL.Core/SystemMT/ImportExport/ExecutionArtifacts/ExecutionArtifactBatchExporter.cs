using System.Text.Json;

namespace MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;

/// <summary>
/// Exports a batch of executions by delegating to the single-execution
/// <see cref="ExecutionArtifactExporter"/> once per id (each into its own sub-directory under
/// the export root) and writing a top-level <c>batch-manifest.json</c>. Export is
/// continue-on-error: a missing / failing execution is recorded as a failed item and the
/// remaining executions still export.
/// </summary>
public sealed class ExecutionArtifactBatchExporter
{
    public const string BatchManifestFileName = "batch-manifest.json";

    private readonly ExecutionArtifactExporter _single;
    private readonly Func<DateTime> _utcNow;

    public ExecutionArtifactBatchExporter(ExecutionArtifactExporter single, Func<DateTime>? utcNow = null)
    {
        _single = single ?? throw new ArgumentNullException(nameof(single));
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    /// <summary>
    /// Exports every id in <paramref name="executionIds"/> under <paramref name="exportRoot"/>,
    /// writes <c>batch-manifest.json</c>, and returns the manifest. The written manifest lives at
    /// <c>Path.Combine(exportRoot, <see cref="BatchManifestFileName"/>)</c>. Inspect
    /// <see cref="ExecutionArtifactBatchManifest.AllSucceeded"/> for the terminal outcome.
    /// </summary>
    public async Task<ExecutionArtifactBatchManifest> ExportBatchAsync(
        IReadOnlyList<Guid> executionIds,
        string exportRoot,
        Guid jobId,
        bool includeEvidence,
        bool includeMarkdown,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executionIds);
        if (executionIds.Count == 0)
            throw new ArgumentException("Batch export requires at least one execution id.", nameof(executionIds));
        if (string.IsNullOrWhiteSpace(exportRoot))
            throw new ArgumentException("Export root must be non-blank.", nameof(exportRoot));

        Directory.CreateDirectory(exportRoot);

        var items = new List<ExecutionArtifactBatchItem>(executionIds.Count);
        foreach (var executionId in executionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var subDirName = executionId.ToString("N");
            var subDir = Path.Combine(exportRoot, subDirName);
            try
            {
                await _single.ExportAsync(
                    new ExecutionArtifactExportRequest(
                        executionId,
                        subDir,
                        IncludeEvidence: includeEvidence,
                        IncludeMarkdown: includeMarkdown,
                        IncludeWord: _single.HasWordRenderer,
                        IncludeExcel: _single.HasExcelRenderer,
                        IncludePdf: _single.HasPdfRenderer),
                    jobId,
                    cancellationToken);

                items.Add(new ExecutionArtifactBatchItem(
                    executionId,
                    Succeeded: true,
                    ManifestRelativePath: $"{subDirName}/manifest.json",
                    Error: null));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                items.Add(new ExecutionArtifactBatchItem(
                    executionId,
                    Succeeded: false,
                    ManifestRelativePath: null,
                    Error: ex.Message));
            }
        }

        var manifest = new ExecutionArtifactBatchManifest(jobId, _utcNow(), items);
        await File.WriteAllTextAsync(
            Path.Combine(exportRoot, BatchManifestFileName),
            JsonSerializer.Serialize(manifest, ExecutionArtifactExporter.JsonOptions),
            cancellationToken);
        return manifest;
    }
}
