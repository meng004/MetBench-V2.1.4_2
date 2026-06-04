using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

/// <summary>
/// Cloud-safe source guard for the Windows-only async job consumer.
/// Linux CI cannot compile the WPF project, so this pins the cancellation
/// registry wiring that makes UI Cancel interrupt a running System MT job
/// instead of only marking the persisted record Cancelled.
/// </summary>
public sealed class WpfAsyncJobCancellationWiringTests
{
    [Fact]
    public void App_di_registers_singleton_job_cancellation_registry()
    {
        var app = ReadRepoFile("MetBench_Client", "App.xaml.cs");

        Assert.Contains(
            "services.AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>();",
            app);
    }

    [Fact]
    public void Hosted_worker_passes_registered_cancellation_registry_to_worker()
    {
        var hosted = ReadRepoFile("MetBench_Client", "Hosting", "SystemMtJobWorkerHostedService.cs");

        Assert.Contains("IJobCancellationRegistry", hosted);
        Assert.Contains("new SystemMtJobWorker(_store, pipeline, _cancellation)", hosted);
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate repository file {Path.Combine(parts)} from {AppContext.BaseDirectory}.");
    }
}
