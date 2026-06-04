using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public sealed class MinimumMrSubsetBGroupAsyncJobTests
{
    [Theory]
    [InlineData("p3-trajectory-sensitivity", "minimum-mr-subset-p3", "separation")]
    [InlineData("p8-norm-conservation", "minimum-mr-subset-p8", "norm_drift")]
    public async Task Submit_and_worker_run_B_group_MR_through_real_launcher_pipeline(
        string mrId,
        string expectedSutName,
        string expectedValueName)
    {
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var worker = new SystemMtJobWorker(store, new SystemMtAsyncPipeline(BuildLauncher()));

        var handle = await service.SubmitAsync(new SystemMtJobRequest(mrId), default);
        var queuedId = await queue.DequeueAsync(default);

        Assert.Equal(handle.JobId, queuedId);

        await worker.RunJobAsync(queuedId, default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        var result = await service.GetResultAsync(handle.JobId, default);

        Assert.NotNull(status);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);
        Assert.Equal(expectedSutName, status.SutName);
        Assert.Equal(100, status.ProgressPercent);
        Assert.Null(status.FailureReason);

        Assert.NotNull(result);
        Assert.True(result!.Passed, result.FailureReason);
        Assert.Equal(mrId, result.MrId);
        Assert.Equal(expectedValueName, result.ValueName);
    }

    private static SystemMtLauncher BuildLauncher()
    {
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable());
        return new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            new RecordingAnomalyService(),
            new ManifestMrCatalogProvider(options));
    }
}
