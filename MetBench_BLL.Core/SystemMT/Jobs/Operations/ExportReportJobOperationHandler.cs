using MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// Exports only the rendered report files (HTML plus any available Word/Excel/PDF)
/// for one execution — no execution-result.json or execution-evidence.json data
/// files. This is the report-only counterpart to
/// <see cref="ExportExecutionArtifactsJobOperationHandler"/>, which exports the full
/// data+report bundle.
/// </summary>
public sealed class ExportReportJobOperationHandler : ISystemMtJobOperationHandler
{
    private readonly ExecutionArtifactExporter _exporter;

    public ExportReportJobOperationHandler(ExecutionArtifactExporter exporter)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
    }

    public SystemMtJobKind Kind => SystemMtJobKind.ExportReport;

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
                FailureReason: "ExecutionId and ExportRoot are required for report export.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "loading execution result", 20));

            var manifestPath = await _exporter.ExportAsync(
                new ExecutionArtifactExportRequest(
                    record.ExecutionId.Value,
                    record.ExportRoot,
                    IncludeResultJson: false,
                    IncludeEvidence: false,
                    IncludeMarkdown: false,
                    IncludeHtml: true,
                    IncludeWord: _exporter.HasWordRenderer,
                    IncludeExcel: _exporter.HasExcelRenderer,
                    IncludePdf: _exporter.HasPdfRenderer),
                jobId,
                cancellationToken);

            progress?.Report(new SystemMtJobProgress(SystemMtJobState.ParsingOutputs, "report exported", 95));
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
