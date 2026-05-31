using System.Text.Json;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// PR-Bol-2B / Bol-Alg-01: end-to-end exercise of the new
/// <c>openmoc-pincell-ray-track-convergence</c> MR through the multi-phase MT flow
/// <c>SystemMtLauncher → SystemMtPipeline.ExecuteMultiPhaseAsync → SystemMtExecutionRecorder</c>.
/// First catalog consumer of the error-monotonic launcher pipeline wired by PR-Bol-2A (#179).
/// Skips cleanly when OpenMOC is not importable from the resolved Python.
/// </summary>
public sealed class LauncherEndToEndOpenMocRayTrackConvergenceTests
{
    private const string MrId = "openmoc-pincell-ray-track-convergence";
    private const string SkipReason =
        "OpenMOC is not importable from the resolved Python. " +
        "Set METBENCH_OPENMOC_PYTHON to a Python where `import openmoc` succeeds.";

    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndOpenMocRayTrackConvergenceTests()
    {
        _recorder = new SystemMtExecutionRecorder(_execs, _results);
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: OpenMocTestPaths.OpenMocPython(),
            OpenMcPython: TestAssetPaths.PythonExecutable());
        _launcher = new SystemMtLauncher(
            options,
            _pipeline,
            _recorder,
            _anomalyService,
            new ManifestMrCatalogProvider(options));
    }

    [SkippableFact]
    public async Task RunAsync_ray_track_convergence_passes_end_to_end_with_default_phases()
    {
        Skip.IfNot(OpenMocTestPaths.OpenMocImportable(), SkipReason);

        // Three calibrated phases: coarse (num_azim=16, spacing=0.05) → medium (48, 0.0166667)
        // → reference (128, 0.00625). ErrorMonotonicKernel (NormKind.Relative) passes
        // iff |k_eff(medium)−k_eff(reference)| ≤ |k_eff(coarse)−k_eff(reference)|.
        var result = await _launcher.RunAsync(MrId);

        Assert.True(result.Passed,
            "ray-track-convergence MR must pass on the canonical pincell baseline. "
            + "FailureReason: " + result.FailureReason);
        Assert.Equal(MrId, result.MrId);
        Assert.Equal("k_eff", result.ValueName);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }

    [SkippableFact]
    public async Task RunAsync_ray_track_convergence_writes_calibrated_phase_inputs()
    {
        Skip.IfNot(OpenMocTestPaths.OpenMocImportable(), SkipReason);

        // Regression guard for the v2 pipeline path: the catalog advertises an
        // atomic RefineRayTracks transform, so both num_azim and azim_spacing_cm
        // must change in each phase. A previous catalog/runtime mismatch only
        // scaled num_azim, leaving spacing fixed at 0.05 and making the canonical
        // OpenMOC run fail ErrorMonotonic at coarse->medium.
        var result = await _launcher.RunAsync(MrId);

        Assert.True(result.Passed, result.FailureReason);
        var exec = Assert.Single(_execs.Data);

        AssertTracking(exec.ArtifactsDirectory, "coarse", expectedNumAzim: 16, expectedSpacing: 0.05);
        AssertTracking(exec.ArtifactsDirectory, "medium", expectedNumAzim: 48, expectedSpacing: 0.05 / 3.0);
        AssertTracking(exec.ArtifactsDirectory, "reference", expectedNumAzim: 128, expectedSpacing: 0.00625);
    }

    private static void AssertTracking(string artifactsDirectory, string role, int expectedNumAzim, double expectedSpacing)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(artifactsDirectory, $"phase.in.{role}.json")));
        var tracking = doc.RootElement.GetProperty("tracking");
        Assert.Equal(expectedNumAzim, tracking.GetProperty("num_azim").GetInt32());
        Assert.Equal(expectedSpacing, tracking.GetProperty("azim_spacing_cm").GetDouble(), 12);
    }
}
