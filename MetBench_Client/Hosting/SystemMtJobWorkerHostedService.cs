using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;
using MetBench_BLL.Core.SystemMT.ImportExport.Put;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MetBench_Client.Hosting;

/// <summary>
/// Hosts the System MT job worker inside the WPF process without touching the UI dispatcher.
/// </summary>
public sealed class SystemMtJobWorkerHostedService : BackgroundService
{
    private readonly IJobQueue _queue;
    private readonly IJobStore _store;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IJobCancellationRegistry _cancellation;
    private readonly ILogger<SystemMtJobWorkerHostedService> _logger;

    public SystemMtJobWorkerHostedService(
        IJobQueue queue,
        IJobStore store,
        IServiceScopeFactory scopeFactory,
        IJobCancellationRegistry cancellation,
        ILogger<SystemMtJobWorkerHostedService> logger)
    {
        _queue = queue;
        _store = store;
        _scopeFactory = scopeFactory;
        _cancellation = cancellation;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;
            try
            {
                jobId = await _queue.DequeueAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var launcher = scope.ServiceProvider.GetRequiredService<ISystemMtLauncher>();
                var resultRepository = scope.ServiceProvider.GetRequiredService<ISystemMtResultRepository>();
                var reportRenderer = scope.ServiceProvider.GetRequiredService<ISystemMtResultReportRenderer>();
                var evidenceRepository = scope.ServiceProvider.GetService<IExecutionEvidenceRepository>();
                var pipeline = new SystemMtAsyncPipeline(launcher, evidenceRepository);
                var operationDispatcher = new SystemMtJobOperationDispatcher(new ISystemMtJobOperationHandler[]
                {
                    new RunBatchJobOperationHandler(launcher, evidenceRepository),
                    new ImportAssetsJobOperationHandler(new SutImportStagingService()),
                    new ExportAssetsJobOperationHandler(),
                    new ExportExecutionArtifactsJobOperationHandler(new ExecutionArtifactExporter(
                        resultRepository,
                        evidenceRepository,
                        reportRenderer)),
                });
                var worker = new SystemMtJobWorker(
                    _store,
                    pipeline,
                    _cancellation,
                    operationDispatcher: operationDispatcher);
                await worker.RunJobAsync(jobId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                await MarkJobFailedAsync(jobId, ex).ConfigureAwait(false);
            }
        }
    }

    private async Task MarkJobFailedAsync(Guid jobId, Exception exception)
    {
        try
        {
            var record = await _store.GetAsync(jobId, CancellationToken.None).ConfigureAwait(false);
            if (record is null || record.State.IsTerminal()) return;

            var now = DateTime.UtcNow;
            await _store.UpdateStatusAsync(record with
            {
                State = SystemMtJobState.Failed,
                CurrentPhase = "failed",
                FailureReason = $"{exception.GetType().Name}: {exception.Message}",
                UpdatedAtUtc = now,
                FinishedAtUtc = now,
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception markFailedException)
        {
            _logger.LogError(
                markFailedException,
                "Failed to mark System MT async job {JobId} as failed after worker infrastructure error.",
                jobId);
        }
    }
}
