using MetBench_BLL.Core.SystemMT.ImportExport.Put;

namespace MetBench_BLL.SystemMT.Jobs;

public sealed class ExportAssetsJobOperationHandler : ISystemMtJobOperationHandler
{
    public SystemMtJobKind Kind => SystemMtJobKind.ExportAssets;

    public Task<JobExecutionOutcome> ExecuteAsync(
        Guid jobId,
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(record.PackageRoot) || string.IsNullOrWhiteSpace(record.ExportRoot))
        {
            return Task.FromResult(new JobExecutionOutcome(
                SystemMtJobState.Failed,
                record.SutName,
                Result: null,
                FailureReason: "PackageRoot and ExportRoot are required for asset export."));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "reading source package", 20));
            var unit = SutImportPackageExporter.Import(record.PackageRoot);

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SystemMtJobProgress(SystemMtJobState.RunningSource, "writing export package", 80));
            var artifactPath = SutImportPackageExporter.Export(unit, record.ExportRoot);

            progress?.Report(new SystemMtJobProgress(SystemMtJobState.ParsingOutputs, "asset export complete", 95));
            return Task.FromResult(new JobExecutionOutcome(
                SystemMtJobState.Succeeded,
                unit.Sut.SutId,
                Result: null,
                FailureReason: null,
                ArtifactPath: artifactPath));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Task.FromResult(new JobExecutionOutcome(
                SystemMtJobState.Failed,
                record.SutName,
                Result: null,
                FailureReason: ex.Message));
        }
    }
}
