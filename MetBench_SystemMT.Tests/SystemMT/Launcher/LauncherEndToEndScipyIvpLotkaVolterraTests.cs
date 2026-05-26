using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// T3C-IVP: SciPy <c>solve_ivp</c>-backed Lotka-Volterra SUT (first external-library-dependent
/// SUT in the catalog). End-to-end via the unified MT flow:
/// <c>SystemMtLauncher → SystemMtPipeline → SystemMtExecutionRecorder</c>.
///
/// SciPy is NOT installed on the cloud CI image; these tests skip cleanly with the verbatim
/// skip reason "SciPy runtime not configured for scipy-ivp-lotka-volterra." when
/// <c>scipy.integrate</c> is not importable from the resolved Python.
/// </summary>
public sealed class LauncherEndToEndScipyIvpLotkaVolterraTests
{
    private const string SkipReason = "SciPy runtime not configured for scipy-ivp-lotka-volterra.";

    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndScipyIvpLotkaVolterraTests()
    {
        _recorder = new SystemMtExecutionRecorder(_execs, _results);
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable(),
            ScipyPython: ScipyTestPaths.ScipyPython());
        _launcher = new SystemMtLauncher(
            options,
            _pipeline,
            _recorder,
            _anomalyService,
            new ManifestMrCatalogProvider(options));
    }

    [SkippableFact]
    public async Task RunAsync_scipy_ivp_lv_prey_growth_monotone_passes_end_to_end()
    {
        Skip.IfNot(ScipyTestPaths.ScipyImportable(), SkipReason);

        // ⟨prey⟩ = γ/δ. factor=2 doubles γ → time-averaged prey strictly increases.
        var result = await _launcher.RunAsync("scipy-ivp-lv-prey-growth-monotone");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("mean_prey", result.ValueName);
        Assert.True(result.FollowUpValue > result.SourceValue,
            $"factor=2 (γ doubled), followup ({result.FollowUpValue}) 应严格大于 source ({result.SourceValue})");

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }

    [SkippableFact]
    public async Task RunAsync_scipy_ivp_lv_step_convergence_passes_end_to_end()
    {
        Skip.IfNot(ScipyTestPaths.ScipyImportable(), SkipReason);

        // SciPy's adaptive RK45 decouples integration accuracy from num_eval_points (which only
        // controls the output sampling grid). Doubling num_eval_points refines the trapezoidal
        // average over the same continuous trajectory; mean_prey converges to the same value
        // within the ToleranceRel=1e-3 / ToleranceAbs=1e-6 tolerance.
        var result = await _launcher.RunAsync("scipy-ivp-lv-step-convergence");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("mean_prey", result.ValueName);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Empty(_anomalyService.Recorded);
    }
}
