using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// Minimum-MR-SubSet A-group live-launcher acceptance: the previously staged
/// P5/P4/P9 imported MR ids now have cloud-safe, pure-stdlib runtime slices in
/// the production manifest catalog and can execute through the unchanged
/// SystemMtLauncher -> SystemMtPipeline -> SystemMtExecutionRecorder path.
/// </summary>
public sealed class LauncherEndToEndMinimumMrSubsetAGroupTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndMinimumMrSubsetAGroupTests()
    {
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable());
        _launcher = new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(_execs, _results),
            _anomalyService,
            new ManifestMrCatalogProvider(options));
    }

    [Theory]
    [InlineData("p5-power-response", "power_extrema")]
    [InlineData("p4-energy-invariant", "energy")]
    [InlineData("p9-k-eff-noise-aware", "sigma_k")]
    public async Task RunAsync_A_group_live_launcher_MR_passes_end_to_end(string mrId, string valueName)
    {
        var result = await _launcher.RunAsync(mrId);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(valueName, result.ValueName);
    }

    [Fact]
    public async Task RunBatchAsync_A_group_records_three_successful_runtime_results()
    {
        var requests = new[]
        {
            new BatchMrRunRequest("p5-power-response"),
            new BatchMrRunRequest("p4-energy-invariant"),
            new BatchMrRunRequest("p9-k-eff-noise-aware"),
        };

        var results = await _launcher.RunBatchAsync(requests);

        Assert.All(results, result => Assert.True(result.Passed, result.FailureReason));
        Assert.Equal(3, _execs.Data.Count);
        Assert.Equal(3, _results.Data.Count);
        Assert.All(_results.Data, result => Assert.True(result.AssertionPassed));
        Assert.Empty(_anomalyService.Recorded);
    }

    [Fact]
    public void Manifest_catalog_contains_A_group_promoted_runtime_bindings()
    {
        var ids = _launcher.ListAvailableAsync().GetAwaiter().GetResult().Select(mr => mr.Id).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("p5-power-response", ids);
        Assert.Contains("p4-energy-invariant", ids);
        Assert.Contains("p9-k-eff-noise-aware", ids);
    }
}
