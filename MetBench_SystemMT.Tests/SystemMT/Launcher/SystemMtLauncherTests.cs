using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

public sealed class SystemMtLauncherTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public SystemMtLauncherTests()
    {
        _recorder = new SystemMtExecutionRecorder(_execs, _results);
        _launcher = new SystemMtLauncher(
            new LauncherOptions(
                SutRoot: TestAssetPaths.AssetRoot(),
                SystemPython: TestAssetPaths.PythonExecutable(),
                OpenMocPython: TestAssetPaths.PythonExecutable()),
            _pipeline,
            _recorder,
            _anomalyService);
    }

    [Fact]
    public void Constructor_rejects_null_options()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtLauncher(null!, _pipeline, _recorder, _anomalyService));
    }

    [Fact]
    public void Constructor_rejects_null_pipeline()
    {
        var options = new LauncherOptions("/tmp", "python3", "python3");
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtLauncher(options, null!, _recorder, _anomalyService));
    }

    [Fact]
    public void Constructor_rejects_null_recorder()
    {
        var options = new LauncherOptions("/tmp", "python3", "python3");
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtLauncher(options, _pipeline, null!, _anomalyService));
    }

    [Fact]
    public void Constructor_rejects_null_anomaly_service()
    {
        var options = new LauncherOptions("/tmp", "python3", "python3");
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtLauncher(options, _pipeline, _recorder, null!));
    }

    [Fact]
    public async Task ListAvailableAsync_returns_known_scenarios_in_id_order()
    {
        var descriptors = await _launcher.ListAvailableAsync();

        Assert.Equal(8, descriptors.Count);
        Assert.Equal("damped-oscillator-scale-state", descriptors[0].Id);
        Assert.Equal("decay-chain-scale-initial", descriptors[1].Id);
        Assert.Equal("heat-equation-amplitude", descriptors[2].Id);
        Assert.Equal("lotka-volterra-scale-gamma", descriptors[3].Id);
        Assert.Equal("openmc-pincell-nu-sigma-f", descriptors[4].Id);
        Assert.Equal("openmc-pincell-sigma-a", descriptors[5].Id);
        Assert.Equal("openmoc-pincell-nu-sigma-f", descriptors[6].Id);
        Assert.Equal("openmoc-pincell-sigma-a", descriptors[7].Id);
    }

    [Fact]
    public async Task ListAvailableAsync_heat_equation_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var heatEq = descriptors.Single(d => d.Id == "heat-equation-amplitude");

        Assert.Equal("heat-equation", heatEq.SutName);
        Assert.Equal("ScaleAmplitude", heatEq.TransformationName);
        Assert.Equal("GreaterThan", heatEq.AssertionName);
        Assert.Equal("max_u", heatEq.ValueName);
        Assert.Equal("2", heatEq.DefaultParameters["factor"]);
        Assert.Contains("linear", heatEq.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Diffusion.Scaling.Amplitude", heatEq.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_groups_cross_program_scenarios_by_MrFamily()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var byFamily = descriptors
            .Where(d => !string.IsNullOrEmpty(d.MrFamily))
            .GroupBy(d => d.MrFamily)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Id).OrderBy(s => s, StringComparer.Ordinal).ToList());

        Assert.Equal(
            new[] { "openmc-pincell-nu-sigma-f", "openmoc-pincell-nu-sigma-f" },
            byFamily["NeutronTransport.Scaling.NuSigmaF"]);

        Assert.Equal(
            new[] { "openmc-pincell-sigma-a", "openmoc-pincell-sigma-a" },
            byFamily["NeutronTransport.Scaling.SigmaA"]);

        Assert.Equal(
            new[] { "heat-equation-amplitude" },
            byFamily["Diffusion.Scaling.Amplitude"]);
    }

    [Fact]
    public async Task ListAvailableAsync_openmoc_sigma_a_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var sigmaA = descriptors.Single(d => d.Id == "openmoc-pincell-sigma-a");

        Assert.Equal("openmoc", sigmaA.SutName);
        Assert.Equal("ScaleFuelSigmaA", sigmaA.TransformationName);
        Assert.Equal("LessThan", sigmaA.AssertionName);
        Assert.Equal("k_eff", sigmaA.ValueName);
    }

    [Fact]
    public async Task RunAsync_rejects_blank_scenario_id()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _launcher.RunAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _launcher.RunAsync("   "));
    }

    [Fact]
    public async Task RunAsync_rejects_unknown_scenario_id()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            _launcher.RunAsync("nonsense-scenario"));
        Assert.Contains("nonsense-scenario", error.Message);
    }

    [Fact]
    public async Task RunAsync_throws_OperationCanceled_when_cancelled_before_run()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _launcher.RunAsync("heat-equation-amplitude", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RunAsync_heat_equation_with_default_factor_passes_and_persists_execution_result()
    {
        var result = await _launcher.RunAsync("heat-equation-amplitude");

        Assert.True(result.Passed, result.FailureReason);
        Assert.True(Guid.TryParse(result.RecordId, out var execId));
        Assert.Equal("heat-equation-amplitude", result.MrId);
        Assert.Equal("max_u", result.ValueName);
        Assert.True(result.SourceValue > 0);
        Assert.True(result.FollowUpValue > result.SourceValue,
            $"follow-up max_u ({result.FollowUpValue}) should exceed source ({result.SourceValue}) with factor=2");

        // v2 schema:Execution + Result 各落 1 行,FK 一致
        var exec = Assert.Single(_execs.Data);
        var res = Assert.Single(_results.Data);
        Assert.Equal(execId, exec.IdExecution);
        Assert.Equal("ok", exec.Status);
        Assert.Equal("launcher", exec.TriggeredBy);
        Assert.False(string.IsNullOrEmpty(exec.ArtifactsDirectory));
        Assert.Equal(execId, res.ExecutionId);
        Assert.True(res.AssertionPassed);
        Assert.Equal(result.SourceValue, res.SourceValue);
        Assert.Equal(result.FollowUpValue, res.FollowupValue);
        // 通过 run 不写 anomaly
        Assert.Empty(_anomalyService.Recorded);
    }

    [Fact]
    public async Task RunAsync_with_parameter_override_uses_overridden_factor()
    {
        var result = await _launcher.RunAsync(
            "heat-equation-amplitude",
            new Dictionary<string, string> { ["factor"] = "3" });

        Assert.True(result.Passed, result.FailureReason);
        var ratio = result.FollowUpValue / result.SourceValue;
        Assert.True(ratio is > 2.9 and < 3.1,
            $"factor=3 should yield ratio ~3.0, got {ratio:F4}");

        // factor=3 → followup max_u ≈ 3 × source max_u
        var res = Assert.Single(_results.Data);
        var resultRatio = res.FollowupValue!.Value / res.SourceValue!.Value;
        Assert.True(resultRatio is > 2.9 and < 3.1);
    }

    [Fact]
    public async Task RunAsync_persists_failure_when_assertion_fails()
    {
        // factor=0.5 halves the amplitude, so follow-up max_u < source max_u,
        // which violates the "greater" assertion.
        var result = await _launcher.RunAsync(
            "heat-equation-amplitude",
            new Dictionary<string, string> { ["factor"] = "0.5" });

        Assert.False(result.Passed);
        Assert.False(string.IsNullOrEmpty(result.FailureReason));
        Assert.True(result.FollowUpValue < result.SourceValue);

        var exec = Assert.Single(_execs.Data);
        Assert.Equal("anomaly", exec.Status);
        var res = Assert.Single(_results.Data);
        Assert.False(res.AssertionPassed);
        Assert.False(string.IsNullOrEmpty(res.FailureReason));
        // 失败 → AnomalyService 应记录一条
        Assert.Single(_anomalyService.Recorded);
        Assert.Equal(res.IdResult.ToString(), _anomalyService.Recorded[0].ResultId);
    }

    [Fact]
    public async Task RunAsync_two_runs_create_two_persisted_executions()
    {
        await _launcher.RunAsync("heat-equation-amplitude");
        await _launcher.RunAsync("heat-equation-amplitude");

        Assert.Equal(2, _execs.Data.Count);
        Assert.Equal(2, _results.Data.Count);
        Assert.All(_execs.Data, e => Assert.Equal("ok", e.Status));
        // 两次 Execution.Id 不同
        Assert.NotEqual(_execs.Data[0].IdExecution, _execs.Data[1].IdExecution);
    }
}
