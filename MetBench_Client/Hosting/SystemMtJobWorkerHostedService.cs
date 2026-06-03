using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    public SystemMtJobWorkerHostedService(IJobQueue queue, IJobStore store, IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _store = store;
        _scopeFactory = scopeFactory;
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
                var pipeline = new SystemMtAsyncPipeline(launcher);
                var worker = new SystemMtJobWorker(_store, pipeline);
                await worker.RunJobAsync(jobId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // SystemMtJobWorker records per-job failures; keep the host alive for later jobs.
            }
        }
    }
}
