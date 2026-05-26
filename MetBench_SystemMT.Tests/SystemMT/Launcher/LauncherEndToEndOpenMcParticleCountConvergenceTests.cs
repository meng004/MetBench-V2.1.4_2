using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// PR-N2 / Bol-Alg-02: end-to-end exercise of the new
/// <c>openmc-pincell-particle-count-convergence</c> MR through the unified
/// MT flow <c>SystemMtLauncher → SystemMtPipeline → SystemMtExecutionRecorder</c>.
/// This is the first catalog consumer of the variance-ratio launcher pipeline
/// wired by PR-VR (#168). The test skips cleanly when OpenMC is not importable
/// from the resolved Python (the CI Ubuntu image does not ship OpenMC); it
/// runs end-to-end on hosts that set <c>METBENCH_OPENMC_PYTHON</c> or that
/// have <c>/opt/openmc-venv/</c>.
/// </summary>
public sealed class LauncherEndToEndOpenMcParticleCountConvergenceTests
{
    private const string MrId = "openmc-pincell-particle-count-convergence";
    private const string SkipReason =
        "OpenMC is not importable from the resolved Python. " +
        "Set METBENCH_OPENMC_PYTHON to a Python where `import openmc` succeeds.";

    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndOpenMcParticleCountConvergenceTests()
    {
        _recorder = new SystemMtExecutionRecorder(_execs, _results);
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable(),
            OpenMcPython: OpenMcTestPaths.OpenMcPython());
        _launcher = new SystemMtLauncher(
            options,
            _pipeline,
            _recorder,
            _anomalyService,
            new ManifestMrCatalogProvider(options));
    }

    [SkippableFact]
    public async Task RunAsync_particle_count_convergence_passes_end_to_end_with_default_factor()
    {
        Skip.IfNot(OpenMcTestPaths.OpenMcImportable(), SkipReason);

        // DefaultParameters: factor=4 → particles 5000 → 20000.
        // 1/√4 = 0.5 expected stderr ratio; ToleranceRel=0.30 →
        // SigmaMultiplier = 1.30; pass iff high.StdError ≤ low.StdError × 0.65.
        var result = await _launcher.RunAsync(MrId);

        Assert.True(result.Passed,
            "particle-count-convergence MR must pass on the canonical pincell baseline. "
            + "FailureReason: " + result.FailureReason);
        Assert.Equal(MrId, result.MrId);
        Assert.Equal("k_eff_std", result.ValueName);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);
        Assert.Empty(_anomalyService.Recorded);
    }

    [SkippableFact]
    public async Task RunAsync_particle_count_convergence_observed_followup_stderr_is_smaller_than_source()
    {
        Skip.IfNot(OpenMcTestPaths.OpenMcImportable(), SkipReason);

        // Sanity assertion alongside the kernel verdict: the *raw* observed
        // k_eff_std on the follow-up (20000 particles) must be strictly smaller
        // than the source (5000 particles) regardless of the kernel's tolerance
        // bookkeeping. If this ever fires it indicates OpenMC is reporting the
        // same stderr on both sides — a real Monte-Carlo regression worth
        // investigating, not a tolerance-tuning problem.
        var result = await _launcher.RunAsync(MrId);

        Assert.True(result.Passed, result.FailureReason);
        Assert.True(
            result.FollowUpValue < result.SourceValue,
            $"Expected k_eff_std(followup={result.FollowUpValue}) < k_eff_std(source={result.SourceValue}) " +
            "under particles ×4. Same-or-larger stderr suggests a sampling-side bug.");
    }
}
