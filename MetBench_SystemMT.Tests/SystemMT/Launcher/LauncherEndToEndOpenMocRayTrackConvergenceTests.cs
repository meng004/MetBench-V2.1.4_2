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

        // Three phases: coarse (num_azim=16, spacing=0.05) → medium (32, 0.025)
        // → reference (64, 0.0125). ErrorMonotonicKernel (NormKind.Relative) passes
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
    public async Task RunAsync_ray_track_convergence_reference_k_eff_strictly_greater_than_coarse()
    {
        Skip.IfNot(OpenMocTestPaths.OpenMocImportable(), SkipReason);

        // Sanity guard alongside the kernel verdict: OpenMOC under-resolves at the
        // coarse phase (num_azim=16, azim_spacing=0.05) and the reference phase
        // (num_azim=64, azim_spacing=0.0125) should report a strictly larger k_eff
        // on this pincell geometry. PR-Bol-2A maps first-phase → SourceValue and
        // last-phase → FollowUpValue for display compatibility, so we read from
        // those fields. Same-or-smaller k_eff at reference would point to a SUT
        // regression (flat convergence or polarity flip), not a tolerance issue.
        var result = await _launcher.RunAsync(MrId);

        Assert.True(result.Passed, result.FailureReason);
        Assert.True(
            result.FollowUpValue > result.SourceValue,
            $"Expected k_eff(reference={result.FollowUpValue}) > k_eff(coarse={result.SourceValue}) " +
            "under three-phase angular refinement. Flat or inverted convergence suggests an OpenMOC regression.");
    }
}
