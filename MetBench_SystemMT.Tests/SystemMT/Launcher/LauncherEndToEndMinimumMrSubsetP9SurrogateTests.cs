using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

public sealed class LauncherEndToEndMinimumMrSubsetP9SurrogateTests
{
    private const string MrId = "p9-k-eff-noise-aware";

    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndMinimumMrSubsetP9SurrogateTests()
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
    public async Task RunAsync_p9_surrogate_variance_ratio_passes_end_to_end()
    {
        var result = await _launcher.RunAsync(MrId);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(MrId, result.MrId);
        Assert.Equal("sigma_k", result.ValueName);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        var sourceSigma = ReadMetric(Path.Combine(exec.ArtifactsDirectory, "source.out.json"), "sigma_k");
        var followupSigma = ReadMetric(Path.Combine(exec.ArtifactsDirectory, "followup.out.json"), "sigma_k");
        Assert.True(
            followupSigma < sourceSigma,
            $"Scaling surrogate particle count should reduce sigma_k: followup={followupSigma}, source={sourceSigma}");
        Assert.Equal(0.5, followupSigma / sourceSigma, precision: 12);

        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }

    private static double ReadMetric(string path, string metricName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty(metricName).GetDouble();
    }
}
