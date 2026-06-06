using MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;

namespace MetBench_BLL.SystemMT.Jobs;

public sealed class ExportExecutionArtifactsJobOperationHandler : ISystemMtJobOperationHandler
{
    private readonly ExecutionArtifactExporter _exporter;

    public ExportExecutionArtifactsJobOperationHandler(ExecutionArtifactExporter exporter)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
    }

    public SystemMtJobKind Kind => SystemMtJobKind.ExportExecutionArtifacts;

    public async Task<JobExecutionOutcome> ExecuteAsync(
        Guid jobId,
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken)
    {
        var isBatch = record.ExecutionIds is { Count: > 0 };
        if (string.IsNullOrWhiteSpace(record.ExportRoot) ||
            (!isBatch && (record.ExecutionId is null || record.ExecutionId == Guid.Empty)))
        {
            return new JobExecutionOutcome(
                SystemMtJobState.Failed,
                record.SutName,
                Result: null,
                FailureReason: "ExportRoot and either ExecutionId or a non-empty ExecutionIds batch are required for execution artifact export.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "loading execution result", 20));

            if (isBatch)
                return await ExportBatchAsync(jobId, record, progress, cancellationToken);

            var manifestPath = await _exporter.ExportAsync(
                new ExecutionArtifactExportRequest(
                    record.ExecutionId!.Value,
                    record.ExportRoot,
                    IncludeMarkdown: false,
                    IncludeWord: _exporter.HasWordRenderer,
                    IncludeExcel: _exporter.HasExcelRenderer,
                    IncludePdf: _exporter.HasPdfRenderer),
                jobId,
                cancellationToken);

            progress?.Report(new SystemMtJobProgress(SystemMtJobState.ParsingOutputs, "execution artifacts exported", 95));
            return new JobExecutionOutcome(
                SystemMtJobState.Succeeded,
                record.SutName,
                Result: null,
                FailureReason: null,
                ArtifactPath: manifestPath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new JobExecutionOutcome(
                SystemMtJobState.Failed,
                record.SutName,
                Result: null,
                FailureReason: ex.Message);
        }
    }

    private async Task<JobExecutionOutcome> ExportBatchAsync(
        Guid jobId,
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken)
    {
        var batch = new ExecutionArtifactBatchExporter(_exporter);
        var manifest = await batch.ExportBatchAsync(
            record.ExecutionIds!,
            record.ExportRoot!,
            jobId,
            includeEvidence: true,
            includeMarkdown: false,
            cancellationToken);

        var manifestPath = Path.Combine(record.ExportRoot!, ExecutionArtifactBatchExporter.BatchManifestFileName);
        progress?.Report(new SystemMtJobProgress(SystemMtJobState.ParsingOutputs, "batch execution artifacts exported", 95));

        // Surface partial failure explicitly: the batch manifest records every item, but the
        // job is Failed if any execution did not export, naming the failure count.
        return manifest.AllSucceeded
            ? new JobExecutionOutcome(
                SystemMtJobState.Succeeded,
                record.SutName,
                Result: null,
                FailureReason: null,
                ArtifactPath: manifestPath)
            : new JobExecutionOutcome(
                SystemMtJobState.Failed,
                record.SutName,
                Result: null,
                FailureReason: $"{manifest.FailureCount} of {manifest.Items.Count} executions failed to export; see batch-manifest.json.",
                ArtifactPath: manifestPath);
    }
}
