using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// PR-A T1 non-JSON I/O adapter — end-to-end integration of the synthetic
/// <c>_test_csv</c> SUT through the unchanged
/// <c>SystemMtLauncher → SystemMtPipeline → SystemMtExecutionRecorder</c> stack.
///
/// The point of this test is NOT to validate physics. It is to prove that the
/// MetBench launcher / pipeline / catalog / manifest provider stay completely
/// agnostic about the on-disk wire format (CSV here), and that the
/// <c>metbench_io</c> Python helper is the single integration point that
/// translates between CSV and the canonical params-dict shape.
/// </summary>
public sealed class LauncherEndToEndTestCsvTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndTestCsvTests()
    {
        _recorder = new SystemMtExecutionRecorder(_execs, _results);
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable());
        _launcher = new SystemMtLauncher(
            options,
            _pipeline,
            _recorder,
            _anomalyService,
            new ManifestMrCatalogProvider(options));
    }

    [Fact]
    public async Task RunAsync_csv_roundtrip_identity_passes_end_to_end()
    {
        // ScaleField on /params/factor: source factor=1.0 → echo_value=1.0;
        // followup factor=2.0 → echo_value=2.0. GreaterThan → followup (2.0) > source (1.0).
        var result = await _launcher.RunAsync("csv-roundtrip-identity");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("echo_value", result.ValueName);
        Assert.Equal(1.0, result.SourceValue);
        Assert.Equal(2.0, result.FollowUpValue);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }
}
