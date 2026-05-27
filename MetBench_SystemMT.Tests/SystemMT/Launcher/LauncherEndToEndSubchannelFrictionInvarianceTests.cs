using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// PR-T3-8 (Phase 5): end-to-end runs of <c>subchannel-friction-invariance</c>,
/// the new MR closing the (navier-stokes, Inv) gap surfaced by
/// <c>docs/superpowers/specs/2026-05-27-meta-pattern-coverage-audit.md</c>.
/// Pure-stdlib Python solver (analytical 0D-axial energy + momentum balance);
/// runs on Linux CI without any venv.
/// </summary>
public sealed class LauncherEndToEndSubchannelFrictionInvarianceTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndSubchannelFrictionInvarianceTests()
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
    public async Task RunAsync_subchannel_friction_invariance_passes_end_to_end()
    {
        // friction_factor 0.02 → 0.04 (factor=2). The 0D-axial subchannel
        // energy balance ΔT = q''·P_h·L/(G·A_xs·c_p) is friction-free; only
        // the momentum equation Δp = f·L·G²/(2·ρ·D_h) sees f. delta_T must
        // be bit-identical across the run. ToleranceRel=1e-12 pins this.
        var result = await _launcher.RunAsync("subchannel-friction-invariance");

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("delta_T", result.ValueName);
        // PR-T2-T3 review-fix T3: friction factor does NOT enter the energy
        // equation ΔT = q''·P_h·L/(G·A_xs·c_p), so doubling it must produce
        // delta_T BIT-IDENTICAL to the source. This is an INTENTIONAL strict
        // regression guard — do NOT loosen to ApproxEqual: any FP perturbation
        // here would indicate the runner's float ordering changed and silently
        // broke the analytical decoupling. If subchannel_1d.py is rewritten
        // and this fact starts failing on a microscopic FP wobble (e.g. 1e-15
        // residual), the right fix is to re-derive WHY a structural change
        // moved bits, not to relax this assertion.
        Assert.Equal(result.SourceValue, result.FollowUpValue);

        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Empty(_anomalyService.Recorded);
    }

    [Fact]
    public async Task RunAsync_subchannel_friction_invariance_persists_result_record()
    {
        // Belt-and-braces: confirm the launcher facade fully persists the
        // result + execution rows via SystemMtExecutionRecorder, matching the
        // shape of every other PassThrough MR.
        var result = await _launcher.RunAsync("subchannel-friction-invariance");

        Assert.True(result.Passed);
        var res = Assert.Single(_results.Data);
        Assert.Equal(result.SourceValue, res.SourceValue);
        Assert.Equal(result.FollowUpValue, res.FollowupValue);
        Assert.True(res.AssertionPassed);
        Assert.True(string.IsNullOrEmpty(res.FailureReason));
    }
}
