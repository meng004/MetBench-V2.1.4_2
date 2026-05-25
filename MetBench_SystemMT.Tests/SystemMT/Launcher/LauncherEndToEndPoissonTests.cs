using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// T3 expansion: 1D Poisson SUT (broader elliptic-PDE coverage beyond the 5
/// reactor anchors) end-to-end via the unified MT flow:
/// <c>SystemMtLauncher → SystemMtPipeline → SystemMtExecutionRecorder</c>.
///
/// Pure stdlib Python solver (Thomas tridiagonal, no numpy / scipy) → CI-runnable
/// on the cloud Linux image, no venv gate. Validates that T1's CLI runner /
/// Python adapter / catalog provider / launcher facade scaffolding handles a
/// brand-new SUT without changes outside the SUT files + catalog wiring.
/// </summary>
public sealed class LauncherEndToEndPoissonTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndPoissonTests()
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
    public async Task RunAsync_poisson_source_superposition_passes_end_to_end()
    {
        // -u'' = f on [0,1], factor=2: u_max(src)=0.125, u_max(flw)=0.25.
        // GreaterThan assertion → followup (0.25) > source (0.125) → pass.
        var result = await _launcher.RunAsync("poisson-source-superposition");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("u_max", result.ValueName);
        Assert.True(result.FollowUpValue > result.SourceValue,
            $"factor=2(默认),followup ({result.FollowUpValue}) 应严格大于 source ({result.SourceValue})");

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }

    [Fact]
    public async Task RunAsync_poisson_mesh_richardson_passes_end_to_end()
    {
        // Doubling num_points (101 → 202) must leave u_max within O(dx²)
        // truncation. The analytic solution is a quadratic; the 2nd-order
        // central FD scheme resolves it to machine epsilon, so the residual
        // is far below the configured ToleranceRel=1e-3 / ToleranceAbs=1e-6.
        var result = await _launcher.RunAsync("poisson-mesh-richardson");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("u_max", result.ValueName);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Empty(_anomalyService.Recorded);
    }
}
