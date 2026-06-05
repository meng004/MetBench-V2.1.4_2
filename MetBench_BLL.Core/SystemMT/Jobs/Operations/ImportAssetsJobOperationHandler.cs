using MetBench_BLL.Core.SystemMT.ImportExport.Put;

namespace MetBench_BLL.SystemMT.Jobs;

public sealed class ImportAssetsJobOperationHandler : ISystemMtJobOperationHandler
{
    private readonly SutImportStagingService _staging;

    public ImportAssetsJobOperationHandler(SutImportStagingService? staging = null)
    {
        _staging = staging ?? new SutImportStagingService();
    }

    public SystemMtJobKind Kind => SystemMtJobKind.ImportAssets;

    public Task<JobExecutionOutcome> ExecuteAsync(
        Guid jobId,
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(record.PackageRoot) || string.IsNullOrWhiteSpace(record.StagingRoot))
        {
            return Task.FromResult(new JobExecutionOutcome(
                SystemMtJobState.Failed,
                record.SutName,
                Result: null,
                FailureReason: "PackageRoot and StagingRoot are required for asset import."));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "reading source package", 20));
            var unit = SutImportPackageExporter.Import(record.PackageRoot);

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SystemMtJobProgress(SystemMtJobState.RunningSource, "staging import package", 80));
            var manifestPath = _staging.Stage(unit, record.PackageRoot, record.StagingRoot);

            progress?.Report(new SystemMtJobProgress(SystemMtJobState.ParsingOutputs, "asset import staged", 95));
            return Task.FromResult(new JobExecutionOutcome(
                SystemMtJobState.Succeeded,
                unit.Sut.SutId,
                Result: null,
                FailureReason: null,
                ArtifactPath: manifestPath));
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
