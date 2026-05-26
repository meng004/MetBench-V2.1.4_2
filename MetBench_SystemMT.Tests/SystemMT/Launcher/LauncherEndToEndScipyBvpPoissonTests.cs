using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// T3C-BVP: SciPy <c>solve_bvp</c>-backed 1D Poisson SUT (second external-library-dependent
/// SUT in the catalog, following the T3C-IVP scipy-ivp-lotka-volterra pilot). End-to-end via
/// the unified MT flow: <c>SystemMtLauncher → SystemMtPipeline → SystemMtExecutionRecorder</c>.
///
/// SciPy is NOT installed on the cloud CI image; these tests skip cleanly with the verbatim
/// skip reason "SciPy runtime not configured for scipy-bvp-poisson-1d." when
/// <c>scipy.integrate</c> is not importable from the resolved Python.
/// </summary>
public sealed class LauncherEndToEndScipyBvpPoissonTests
{
    private const string SkipReason = "SciPy runtime not configured for scipy-bvp-poisson-1d.";

    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndScipyBvpPoissonTests()
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
    public async Task RunAsync_scipy_bvp_poisson_source_superposition_passes_end_to_end()
    {
        Skip.IfNot(ScipyTestPaths.ScipyImportable(), SkipReason);

        // -u'' = f on [0,1], factor=2: u_max(src)=0.125, u_max(flw)=0.25.
        // GreaterThan → followup (0.25) > source (0.125) → pass.
        var result = await _launcher.RunAsync("scipy-bvp-poisson-source-superposition");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("u_max", result.ValueName);
        Assert.True(result.FollowUpValue > result.SourceValue,
            $"factor=2 (f doubled), followup ({result.FollowUpValue}) 应严格大于 source ({result.SourceValue})");

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }

    [SkippableFact]
    public async Task RunAsync_scipy_bvp_poisson_mesh_richardson_passes_end_to_end()
    {
        Skip.IfNot(ScipyTestPaths.ScipyImportable(), SkipReason);

        // SciPy solve_bvp adaptively refines its own mesh until residual is below tolerance;
        // the user-supplied num_points (101 → 202) only seeds the initial mesh. Both runs
        // converge to the same continuous solution u(x)=f·x(L−x)/2, whose peak value
        // u_max = f·L²/8 = 0.125 occurs at x = L/2, within ToleranceRel=1e-3 / ToleranceAbs=1e-6.
        var result = await _launcher.RunAsync("scipy-bvp-poisson-seed-mesh-insensitivity");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("u_max", result.ValueName);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Empty(_anomalyService.Recorded);
    }
}
