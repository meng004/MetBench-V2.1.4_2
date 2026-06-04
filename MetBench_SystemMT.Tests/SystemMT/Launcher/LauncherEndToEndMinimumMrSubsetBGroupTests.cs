using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

public sealed class LauncherEndToEndMinimumMrSubsetBGroupTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndMinimumMrSubsetBGroupTests()
    {
        var recorder = new SystemMtExecutionRecorder(_execs, _results);
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable());
        _launcher = new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            recorder,
            _anomalyService,
            new ManifestMrCatalogProvider(options));
    }

    [Theory]
    [InlineData("p3-trajectory-sensitivity")]
    [InlineData("p8-norm-conservation")]
    public async Task ListAvailableAsync_includes_B_group_live_MRs(string mrId)
    {
        var descriptors = await _launcher.ListAvailableAsync();

        Assert.Contains(descriptors, descriptor => descriptor.Id == mrId);
    }

    [Fact]
    public async Task RunAsync_p3_trajectory_sensitivity_passes_end_to_end()
    {
        var result = await _launcher.RunAsync("p3-trajectory-sensitivity");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("p3-trajectory-sensitivity", result.MrId);
        Assert.Equal("separation", result.ValueName);
        Assert.True(
            result.FollowUpValue > result.SourceValue,
            $"Increasing the initial perturbation should increase trajectory separation: followup={result.FollowUpValue}, source={result.SourceValue}");

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }

    [Fact]
    public async Task RunAsync_p8_norm_conservation_passes_end_to_end()
    {
        var result = await _launcher.RunAsync("p8-norm-conservation");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("p8-norm-conservation", result.MrId);
        Assert.Equal("norm_drift", result.ValueName);
        Assert.True(
            result.FollowUpValue < result.SourceValue,
            $"Increasing propagation steps should reduce norm drift in the MetBench runtime slice: followup={result.FollowUpValue}, source={result.SourceValue}");

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }
}
