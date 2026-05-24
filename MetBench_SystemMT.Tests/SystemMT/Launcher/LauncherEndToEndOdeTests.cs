using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Transformations;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// 方向 1 — 3 个 ODE SUT 经 unified MT 流程端到端跑通的回归门:
/// <c>SystemMtLauncher → SystemMtPipeline → SystemMtExecutionRecorder</c>。
/// 此前 launcher 端到端在 CI 只有 heat-equation 一例覆盖,这里把 decay-chain /
/// damped-oscillator / lotka-volterra 全部纳入(均靠 system python + scipy,CI 可达;
/// openmoc / openmc 仍由 venv 门控,落在 VM UAT)。
/// </summary>
public sealed class LauncherEndToEndOdeTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public LauncherEndToEndOdeTests()
    {
        _recorder = new SystemMtExecutionRecorder(_execs, _results);
        _launcher = new SystemMtLauncher(
            new LauncherOptions(
                SutRoot: TestAssetPaths.AssetRoot(),
                SystemPython: TestAssetPaths.PythonExecutable(),
                OpenMocPython: TestAssetPaths.PythonExecutable()),
            _pipeline,
            _recorder,
            _anomalyService,
            new ManifestMrCatalogProvider(new LauncherOptions(
                SutRoot: TestAssetPaths.AssetRoot(),
                SystemPython: TestAssetPaths.PythonExecutable(),
                OpenMocPython: TestAssetPaths.PythonExecutable())));
    }

    [Theory]
    [InlineData("decay-chain-scale-initial",      "N_C_final")]
    [InlineData("damped-oscillator-scale-state",  "max_abs_displacement")]
    [InlineData("lotka-volterra-scale-gamma",     "mean_prey")]
    public async Task RunAsync_ode_mr_with_default_factor_passes_end_to_end(
        string mrId, string expectedValueName)
    {
        var result = await _launcher.RunAsync(mrId);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(expectedValueName, result.ValueName);
        Assert.True(result.FollowUpValue > result.SourceValue,
            $"factor=2(默认),followup ({result.FollowUpValue}) 应严格大于 source ({result.SourceValue})");

        // v2 schema:Execution + Result 各 1 行,FK 一致
        var exec = Assert.Single(_execs.Data);
        var res  = Assert.Single(_results.Data);
        Assert.Equal("ok", exec.Status);
        Assert.True(res.AssertionPassed);
        Assert.Equal(exec.IdExecution, res.ExecutionId);

        // 通过 run 不触发 anomaly 桥
        Assert.Empty(_anomalyService.Recorded);
    }

    [Fact]
    public void Damped_oscillator_still_registers_composite_transformation_in_registry()
    {
        // damped-oscillator 仍有多步 TransformSteps,走 CompositeTransform
        var t = TransformationRegistry.Get("Composite-damped-oscillator-scale-state");
        Assert.IsType<CompositeTransform>(t);
    }

    [Fact]
    public void Decay_chain_no_longer_registers_composite_uses_recipe()
    {
        // P4: decay-chain-scale-initial 改用 L1 Recipe(EquationKey="bateman"),
        // 不再注册 Composite-decay-chain-scale-initial
        Assert.Throws<KeyNotFoundException>(() =>
            TransformationRegistry.Get("Composite-decay-chain-scale-initial"));
    }

    [Fact]
    public void Single_step_mr_uses_registered_transformation_directly()
    {
        // 单步 MR 不注册 Composite-<id>;lotka-volterra 应直接走 ScaleField
        Assert.Throws<KeyNotFoundException>(() =>
            TransformationRegistry.Get("Composite-lotka-volterra-scale-gamma"));
        // 而 ScaleField 自身始终在 registry
        Assert.IsType<ScaleField>(TransformationRegistry.Get("ScaleField"));
    }
}
