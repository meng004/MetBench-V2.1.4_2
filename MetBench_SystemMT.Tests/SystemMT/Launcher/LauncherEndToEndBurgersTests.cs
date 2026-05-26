using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// T3 expansion: 1D inviscid Burgers SUT (first nonlinear-PDE coverage
/// after the linear hyperbolic family Advection / Wave) end-to-end via
/// the unified MT flow:
/// <c>SystemMtLauncher → SystemMtPipeline → SystemMtExecutionRecorder</c>.
///
/// Pure-stdlib Python solver (Lax-Friedrichs conservative flux differencing,
/// periodic BC, no numpy / scipy) → CI-runnable on the cloud Linux image.
/// </summary>
public sealed class LauncherEndToEndBurgersTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndBurgersTests()
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
    public async Task RunAsync_burgers_amplitude_peak_monotone_passes_end_to_end()
    {
        // Inviscid Burgers with positive Gaussian IC self-steepens; LxF
        // smears the shock so peak(2A) ≈ 1.99·peak(A) (not exactly 2× due
        // to nonlinear dissipation), still strictly greater.
        // amp=1 → peak≈0.815; amp=2 (factor=2 default) → peak≈1.617.
        // GreaterThan → followup > source → pass.
        var result = await _launcher.RunAsync("burgers-amplitude-peak-monotone");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("peak_amplitude", result.ValueName);
        Assert.True(result.FollowUpValue > result.SourceValue,
            $"factor=2 默认 → followup ({result.FollowUpValue}) 应严格大于 source ({result.SourceValue})");

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }

    [Fact]
    public async Task RunAsync_burgers_mesh_conservation_passes_end_to_end()
    {
        // num_points 200 → 400 (factor=2). LxF conservative flux differencing
        // with periodic BC preserves ∫u dx exactly per step regardless of the
        // shock structure; the only delta between source and follow-up is the
        // initial Gaussian sampling error O(dx²) ~ 6e-10. Well within
        // ToleranceRel=1e-3 / ToleranceAbs=1e-6.
        var result = await _launcher.RunAsync("burgers-mesh-conservation");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("mass_integral", result.ValueName);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Empty(_anomalyService.Recorded);
    }
}
