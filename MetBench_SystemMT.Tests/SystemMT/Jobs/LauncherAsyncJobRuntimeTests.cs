using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

/// <summary>
/// SP1: async job path (SystemMtJobService -&gt; SystemMtJobWorker -&gt; SystemMtAsyncPipeline
/// -&gt; SystemMtLauncher) exercised against the REAL external runtimes (scipy / openmoc /
/// openmc). Gated like the sync end-to-end tests, so it skips on CI (no venv) and runs
/// for real inside metbench-runtime where the venvs are importable. Proves the async
/// chain works on real runtimes.
/// </summary>
public sealed class LauncherAsyncJobRuntimeTests
{
    [SkippableFact]
    public async Task Async_job_runs_scipy_mr_end_to_end()
    {
        Skip.IfNot(ScipyTestPaths.ScipyImportable(),
            "SciPy runtime not configured for scipy async job test.");
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable(),
            ScipyPython: ScipyTestPaths.ScipyPython());
        await RunAsyncJobAndAssertSucceeded("scipy-ivp-lv-prey-growth-monotone", options);
    }

    [SkippableFact]
    public async Task Async_job_runs_openmoc_mr_end_to_end()
    {
        Skip.IfNot(OpenMocTestPaths.OpenMocImportable(),
            "OpenMOC runtime not importable for openmoc async job test.");
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: OpenMocTestPaths.OpenMocPython(),
            OpenMcPython: TestAssetPaths.PythonExecutable());
        await RunAsyncJobAndAssertSucceeded("openmoc-pincell-nu-sigma-f", options);
    }

    [SkippableFact]
    public async Task Async_job_runs_openmc_mr_end_to_end()
    {
        Skip.IfNot(OpenMcTestPaths.OpenMcImportable(),
            "OpenMC runtime not importable for openmc async job test.");
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable(),
            OpenMcPython: OpenMcTestPaths.OpenMcPython());
        await RunAsyncJobAndAssertSucceeded("openmc-pincell-nu-sigma-f", options);
    }

    private static async Task RunAsyncJobAndAssertSucceeded(string mrId, LauncherOptions options)
    {
        var launcher = new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            new RecordingAnomalyService(),
            new ManifestMrCatalogProvider(options));

        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var worker = new SystemMtJobWorker(store, new SystemMtAsyncPipeline(launcher));

        var handle = await service.SubmitAsync(new SystemMtJobRequest(mrId), default);
        var queuedId = await queue.DequeueAsync(default);
        Assert.Equal(handle.JobId, queuedId);

        await worker.RunJobAsync(queuedId, default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        var result = await service.GetResultAsync(handle.JobId, default);

        Assert.NotNull(status);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);
        Assert.Equal(100, status.ProgressPercent);
        Assert.Null(status.FailureReason);

        Assert.NotNull(result);
        Assert.True(result!.Passed, result.FailureReason);
        Assert.Equal(mrId, result.MrId);
    }
}
