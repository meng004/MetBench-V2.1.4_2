using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// T3 expansion: 1D linear advection SUT (first hyperbolic-PDE coverage after
/// the Poisson elliptic anchor) end-to-end via the unified MT flow:
/// <c>SystemMtLauncher → SystemMtPipeline → SystemMtExecutionRecorder</c>.
///
/// Pure-stdlib Python solver (first-order upwind FD, periodic BC, no numpy /
/// scipy) → CI-runnable on the cloud Linux image.
/// </summary>
public sealed class LauncherEndToEndAdvectionTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndAdvectionTests()
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
    public async Task RunAsync_advection_amplitude_linearity_passes_end_to_end()
    {
        // Initial amplitude=1 → final peak_amplitude ≈ 0.756 (with first-order
        // upwind numerical diffusion). amplitude=2 (factor=2 default) → 1.512.
        // GreaterThan → followup (1.512) > source (0.756) → pass.
        var result = await _launcher.RunAsync("advection-amplitude-linearity");

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
    public async Task RunAsync_advection_mesh_conservation_passes_end_to_end()
    {
        // num_points 200 → 400 (factor=2). Conservative upwind preserves
        // ∫u dx exactly per step; the only difference between src and flw
        // is the initial-Gaussian-sampling error (O(dx²) ~ 4e-9). Well
        // within ToleranceRel=1e-3 / ToleranceAbs=1e-6.
        var result = await _launcher.RunAsync("advection-mesh-conservation");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("mass_integral", result.ValueName);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Empty(_anomalyService.Recorded);
    }
}
