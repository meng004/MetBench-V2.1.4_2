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
        if (record.ExecutionId is null || record.ExecutionId == Guid.Empty || string.IsNullOrWhiteSpace(record.ExportRoot))
        {
            return new JobExecutionOutcome(
                SystemMtJobState.Failed,
                record.SutName,
                Result: null,
                FailureReason: "ExecutionId and ExportRoot are required for execution artifact export.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "loading execution result", 20));

            var manifestPath = await _exporter.ExportAsync(
                new ExecutionArtifactExportRequest(
                    record.ExecutionId.Value,
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
}
