using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// T3 expansion: 1D linear wave SUT (second hyperbolic-PDE coverage after
/// Advection) end-to-end via the unified MT flow:
/// <c>SystemMtLauncher → SystemMtPipeline → SystemMtExecutionRecorder</c>.
///
/// Pure-stdlib Python solver (second-order leapfrog FD, Dirichlet BC,
/// zero initial velocity, no numpy / scipy) → CI-runnable on the cloud
/// Linux image.
/// </summary>
public sealed class LauncherEndToEndWaveTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndWaveTests()
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
    public async Task RunAsync_wave_amplitude_linearity_passes_end_to_end()
    {
        // d'Alembert: smooth Gaussian initial condition with zero initial
        // velocity splits into two half-amplitude pulses traveling ±c.
        // amplitude=1 → final peak_amplitude ≈ 0.5 (each half-pulse).
        // amplitude=2 (factor=2 default) → ≈ 1.0 exactly under linear leapfrog.
        // GreaterThan → followup > source → pass.
        var result = await _launcher.RunAsync("wave-amplitude-linearity");

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
    public async Task RunAsync_wave_mesh_energy_convergence_passes_end_to_end()
    {
        // num_points 201 → 402 (factor=2); internal dt halving keeps
        // Courant=0.5. Second-order leapfrog on a smooth Gaussian preserves
        // the L² invariant 0.5·∫u² dx to ~1e-12 between source and follow-up.
        // Well within ToleranceRel=1e-3 / ToleranceAbs=1e-6.
        var result = await _launcher.RunAsync("wave-mesh-energy-convergence");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("energy_proxy", result.ValueName);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Empty(_anomalyService.Recorded);
    }
}
