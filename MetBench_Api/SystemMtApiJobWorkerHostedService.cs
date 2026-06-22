using MetBench_BLL.SystemMT.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MetBench_Api;

public sealed class SystemMtApiJobWorkerHostedService : BackgroundService
{
    private readonly IJobQueue _queue;
    private readonly IJobStore _store;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IJobCancellationRegistry _cancellation;
    private readonly ILogger<SystemMtApiJobWorkerHostedService> _logger;

    public SystemMtApiJobWorkerHostedService(
        IJobQueue queue,
        IJobStore store,
        IServiceScopeFactory scopeFactory,
        IJobCancellationRegistry cancellation,
        ILogger<SystemMtApiJobWorkerHostedService> logger)
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
                var worker = new SystemMtJobWorker(
                    _store,
                    scope.ServiceProvider.GetRequiredService<ISystemMtAsyncPipeline>(),
                    _cancellation);
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
            if (record is null || record.State.IsTerminal())
                return;

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
                "Failed to mark System MT API job {JobId} as failed after worker infrastructure error.",
                jobId);
        }
    }
}
