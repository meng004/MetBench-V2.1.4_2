using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

public sealed class LauncherEndToEndMinimumMrSubsetP4Tests
{
    private const string MrId = "p4-energy-invariant";

    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndMinimumMrSubsetP4Tests()
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

    [Fact]
    public async Task RunAsync_p4_energy_invariant_passes_end_to_end()
    {
        var result = await _launcher.RunAsync(MrId);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(MrId, result.MrId);
        Assert.Equal("energy_drift", result.ValueName);
        Assert.True(
            result.FollowUpValue < result.SourceValue,
            $"Doubling n_steps should reduce symplectic energy drift: followup={result.FollowUpValue}, source={result.SourceValue}");

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }
}
