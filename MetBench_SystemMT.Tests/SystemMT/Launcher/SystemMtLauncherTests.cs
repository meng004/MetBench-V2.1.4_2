using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Catalog;
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
    private static IMrCatalogProvider TestCatalogProvider() => new ManifestMrCatalogProvider(
        new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable()));

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
            _anomalyService,
            TestCatalogProvider());
    }

    [Fact]
    public void Constructor_rejects_null_options()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtLauncher(null!, _pipeline, _recorder, _anomalyService, TestCatalogProvider()));
    }

    [Fact]
    public void Constructor_rejects_null_pipeline()
    {
        var options = new LauncherOptions("/tmp", "python3", "python3");
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtLauncher(options, null!, _recorder, _anomalyService, TestCatalogProvider()));
    }

    [Fact]
    public void Constructor_rejects_null_recorder()
    {
        var options = new LauncherOptions("/tmp", "python3", "python3");
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtLauncher(options, _pipeline, null!, _anomalyService, TestCatalogProvider()));
    }

    [Fact]
    public void Constructor_rejects_null_anomaly_service()
    {
        var options = new LauncherOptions("/tmp", "python3", "python3");
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtLauncher(options, _pipeline, _recorder, null!, TestCatalogProvider()));
    }

    [Fact]
    public async Task ListAvailableAsync_returns_known_scenarios_in_id_order()
    {
        var descriptors = await _launcher.ListAvailableAsync();

        Assert.Equal(30, descriptors.Count);
        Assert.Equal("advection-amplitude-linearity", descriptors[0].Id);
        Assert.Equal("advection-mesh-conservation", descriptors[1].Id);
        Assert.Equal("bateman-mass-conservation", descriptors[2].Id);
        Assert.Equal("bateman-timestep-cauchy", descriptors[3].Id);
        Assert.Equal("burgers-amplitude-peak-monotone", descriptors[4].Id);
        Assert.Equal("burgers-mesh-conservation", descriptors[5].Id);
        Assert.Equal("csv-roundtrip-identity", descriptors[6].Id);
        Assert.Equal("damped-oscillator-scale-state", descriptors[7].Id);
        Assert.Equal("decay-chain-scale-initial", descriptors[8].Id);
        Assert.Equal("diffusion-mesh-richardson", descriptors[9].Id);
        Assert.Equal("diffusion-source-linearity", descriptors[10].Id);
        Assert.Equal("fourier-alpha-monotonic", descriptors[11].Id);
        Assert.Equal("fourier-timestep-convergence", descriptors[12].Id);
        Assert.Equal("heat-equation-amplitude", descriptors[13].Id);
        Assert.Equal("lotka-volterra-scale-gamma", descriptors[14].Id);
        Assert.Equal("openmc-pincell-nu-sigma-f", descriptors[15].Id);
        Assert.Equal("openmc-pincell-sigma-a", descriptors[16].Id);
        Assert.Equal("openmoc-pincell-nu-sigma-f", descriptors[17].Id);
        Assert.Equal("openmoc-pincell-sigma-a", descriptors[18].Id);
        Assert.Equal("poisson-mesh-richardson", descriptors[19].Id);
        Assert.Equal("poisson-source-superposition", descriptors[20].Id);
        Assert.Equal("projectile-scale-v0", descriptors[21].Id);
        Assert.Equal("scipy-bvp-poisson-seed-mesh-insensitivity", descriptors[22].Id);
        Assert.Equal("scipy-bvp-poisson-source-superposition", descriptors[23].Id);
        Assert.Equal("scipy-ivp-lv-prey-growth-monotone", descriptors[24].Id);
        Assert.Equal("scipy-ivp-lv-step-convergence", descriptors[25].Id);
        Assert.Equal("subchannel-flow-temperature-monotone", descriptors[26].Id);
        Assert.Equal("subchannel-heat-flux-linearity", descriptors[27].Id);
        Assert.Equal("wave-amplitude-linearity", descriptors[28].Id);
        Assert.Equal("wave-mesh-energy-convergence", descriptors[29].Id);
    }

    [Fact]
    public async Task ListAvailableAsync_advection_amplitude_linearity_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var lin = descriptors.Single(d => d.Id == "advection-amplitude-linearity");

        Assert.Equal("advection-1d", lin.SutName);
        Assert.Equal("ScaleField", lin.TransformationName);
        Assert.Equal("GreaterThan", lin.AssertionName);
        Assert.Equal("peak_amplitude", lin.ValueName);
        Assert.Equal("2", lin.DefaultParameters["factor"]);
        Assert.Contains("amplitude", lin.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Advection.Scaling.Amplitude", lin.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_advection_mesh_conservation_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var cons = descriptors.Single(d => d.Id == "advection-mesh-conservation");

        Assert.Equal("advection-1d", cons.SutName);
        Assert.Equal("ScaleField", cons.TransformationName);
        Assert.Equal("ApproxEqual", cons.AssertionName);
        Assert.Equal("mass_integral", cons.ValueName);
        Assert.Equal("2", cons.DefaultParameters["factor"]);
        Assert.Contains("conservation", cons.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Advection.Invariance.Mass", cons.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_wave_amplitude_linearity_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var lin = descriptors.Single(d => d.Id == "wave-amplitude-linearity");

        Assert.Equal("wave-1d", lin.SutName);
        Assert.Equal("ScaleField", lin.TransformationName);
        Assert.Equal("GreaterThan", lin.AssertionName);
        Assert.Equal("peak_amplitude", lin.ValueName);
        Assert.Equal("2", lin.DefaultParameters["factor"]);
        Assert.Contains("amplitude", lin.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Wave.Scaling.Amplitude", lin.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_wave_mesh_energy_convergence_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var conv = descriptors.Single(d => d.Id == "wave-mesh-energy-convergence");

        Assert.Equal("wave-1d", conv.SutName);
        Assert.Equal("ScaleField", conv.TransformationName);
        Assert.Equal("ApproxEqual", conv.AssertionName);
        Assert.Equal("energy_proxy", conv.ValueName);
        Assert.Equal("2", conv.DefaultParameters["factor"]);
        Assert.Contains("convergence", conv.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Wave.Convergence.Energy", conv.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_burgers_amplitude_peak_monotone_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var mono = descriptors.Single(d => d.Id == "burgers-amplitude-peak-monotone");

        Assert.Equal("burgers-1d", mono.SutName);
        Assert.Equal("ScaleField", mono.TransformationName);
        Assert.Equal("GreaterThan", mono.AssertionName);
        Assert.Equal("peak_amplitude", mono.ValueName);
        Assert.Equal("2", mono.DefaultParameters["factor"]);
        Assert.Contains("amplitude", mono.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Burgers.Scaling.Amplitude", mono.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_burgers_mesh_conservation_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var cons = descriptors.Single(d => d.Id == "burgers-mesh-conservation");

        Assert.Equal("burgers-1d", cons.SutName);
        Assert.Equal("ScaleField", cons.TransformationName);
        Assert.Equal("ApproxEqual", cons.AssertionName);
        Assert.Equal("mass_integral", cons.ValueName);
        Assert.Equal("2", cons.DefaultParameters["factor"]);
        Assert.Contains("conservation", cons.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Burgers.Invariance.Mass", cons.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_poisson_source_superposition_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var lin = descriptors.Single(d => d.Id == "poisson-source-superposition");

        Assert.Equal("poisson-1d", lin.SutName);
        Assert.Equal("ScaleField", lin.TransformationName);
        Assert.Equal("GreaterThan", lin.AssertionName);
        Assert.Equal("u_max", lin.ValueName);
        Assert.Equal("2", lin.DefaultParameters["factor"]);
        Assert.Contains("source", lin.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Poisson.Scaling.Source", lin.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_poisson_mesh_richardson_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var rich = descriptors.Single(d => d.Id == "poisson-mesh-richardson");

        Assert.Equal("poisson-1d", rich.SutName);
        Assert.Equal("ScaleField", rich.TransformationName);
        Assert.Equal("ApproxEqual", rich.AssertionName);
        Assert.Equal("u_max", rich.ValueName);
        Assert.Equal("2", rich.DefaultParameters["factor"]);
        Assert.Contains("mesh", rich.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Poisson.Convergence.Mesh", rich.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_diffusion_source_linearity_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var lin = descriptors.Single(d => d.Id == "diffusion-source-linearity");

        Assert.Equal("diffusion-1d", lin.SutName);
        Assert.Equal("ScaleField", lin.TransformationName);
        Assert.Equal("GreaterThan", lin.AssertionName);
        Assert.Equal("phi_max", lin.ValueName);
        Assert.Equal("2", lin.DefaultParameters["factor"]);
        Assert.Contains("source", lin.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Diffusion.Scaling.Source", lin.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_diffusion_mesh_richardson_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var rich = descriptors.Single(d => d.Id == "diffusion-mesh-richardson");

        Assert.Equal("diffusion-1d", rich.SutName);
        Assert.Equal("ScaleField", rich.TransformationName);
        Assert.Equal("ApproxEqual", rich.AssertionName);
        Assert.Equal("phi_max", rich.ValueName);
        Assert.Equal("2", rich.DefaultParameters["factor"]);
        Assert.Contains("mesh", rich.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Diffusion.Convergence.Mesh", rich.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_subchannel_flow_temperature_monotone_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var mono = descriptors.Single(d => d.Id == "subchannel-flow-temperature-monotone");

        Assert.Equal("subchannel-1d", mono.SutName);
        Assert.Equal("ScaleField", mono.TransformationName);
        Assert.Equal("LessThan", mono.AssertionName);
        Assert.Equal("delta_T", mono.ValueName);
        Assert.Equal("2", mono.DefaultParameters["factor"]);
        Assert.Contains("flow", mono.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Subchannel.Scaling.MassFlux", mono.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_subchannel_heat_flux_linearity_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var lin = descriptors.Single(d => d.Id == "subchannel-heat-flux-linearity");

        Assert.Equal("subchannel-1d", lin.SutName);
        Assert.Equal("ScaleField", lin.TransformationName);
        Assert.Equal("GreaterThan", lin.AssertionName);
        Assert.Equal("delta_T", lin.ValueName);
        Assert.Equal("2", lin.DefaultParameters["factor"]);
        Assert.Contains("heat", lin.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Subchannel.Scaling.HeatFlux", lin.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_fourier_timestep_convergence_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var conv = descriptors.Single(d => d.Id == "fourier-timestep-convergence");

        Assert.Equal("heat-equation", conv.SutName);
        Assert.Equal("ScaleField", conv.TransformationName);
        Assert.Equal("ApproxEqual", conv.AssertionName);
        Assert.Equal("max_u", conv.ValueName);
        Assert.Equal("2", conv.DefaultParameters["factor"]);
        Assert.Contains("step", conv.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Fourier.Convergence.Timestep", conv.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_fourier_alpha_monotonic_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var mono = descriptors.Single(d => d.Id == "fourier-alpha-monotonic");

        Assert.Equal("heat-equation", mono.SutName);
        Assert.Equal("ScaleField", mono.TransformationName);
        Assert.Equal("LessThan", mono.AssertionName);
        Assert.Equal("max_u", mono.ValueName);
        Assert.Equal("2", mono.DefaultParameters["factor"]);
        Assert.Contains("diffus", mono.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Fourier.Scaling.Alpha", mono.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_bateman_mass_conservation_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var massConservation = descriptors.Single(d => d.Id == "bateman-mass-conservation");

        Assert.Equal("decay-chain", massConservation.SutName);
        Assert.Equal("ScaleField", massConservation.TransformationName);
        Assert.Equal("ApproxEqual", massConservation.AssertionName);
        Assert.Equal("total", massConservation.ValueName);
        Assert.Equal("2", massConservation.DefaultParameters["factor"]);
        Assert.Contains("conservation", massConservation.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bateman.Invariance.MassConservation", massConservation.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_bateman_timestep_cauchy_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var cauchy = descriptors.Single(d => d.Id == "bateman-timestep-cauchy");

        Assert.Equal("decay-chain", cauchy.SutName);
        Assert.Equal("ScaleField", cauchy.TransformationName);
        Assert.Equal("ApproxEqual", cauchy.AssertionName);
        Assert.Equal("N_C_final", cauchy.ValueName);
        Assert.Equal("2", cauchy.DefaultParameters["factor"]);
        Assert.Contains("step", cauchy.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bateman.Convergence.Timestep", cauchy.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_projectile_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var projectile = descriptors.Single(d => d.Id == "projectile-scale-v0");

        Assert.Equal("projectile", projectile.SutName);
        Assert.Equal("ScaleField", projectile.TransformationName);
        Assert.Equal("GreaterThan", projectile.AssertionName);
        Assert.Equal("range", projectile.ValueName);
        Assert.Equal("2", projectile.DefaultParameters["factor"]);
        Assert.Contains("v0", projectile.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Projectile.Scaling.V0", projectile.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_scipy_ivp_lv_prey_growth_monotone_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var mono = descriptors.Single(d => d.Id == "scipy-ivp-lv-prey-growth-monotone");

        Assert.Equal("scipy-ivp-lotka-volterra", mono.SutName);
        Assert.Equal("ScaleGamma", mono.TransformationName);
        Assert.Equal("GreaterThan", mono.AssertionName);
        Assert.Equal("mean_prey", mono.ValueName);
        Assert.Equal("2", mono.DefaultParameters["factor"]);
        Assert.Contains("gamma", mono.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ScipyIvp.LotkaVolterra.Scaling.Gamma", mono.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_scipy_ivp_lv_step_convergence_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var conv = descriptors.Single(d => d.Id == "scipy-ivp-lv-step-convergence");

        Assert.Equal("scipy-ivp-lotka-volterra", conv.SutName);
        Assert.Equal("ScaleField", conv.TransformationName);
        Assert.Equal("ApproxEqual", conv.AssertionName);
        Assert.Equal("mean_prey", conv.ValueName);
        Assert.Equal("2", conv.DefaultParameters["factor"]);
        Assert.Contains("eval", conv.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ScipyIvp.LotkaVolterra.Convergence.EvalGrid", conv.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_scipy_bvp_poisson_source_superposition_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var lin = descriptors.Single(d => d.Id == "scipy-bvp-poisson-source-superposition");

        Assert.Equal("scipy-bvp-poisson-1d", lin.SutName);
        Assert.Equal("ScaleField", lin.TransformationName);
        Assert.Equal("GreaterThan", lin.AssertionName);
        Assert.Equal("u_max", lin.ValueName);
        Assert.Equal("2", lin.DefaultParameters["factor"]);
        Assert.Contains("source", lin.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ScipyBvp.Poisson.Scaling.Source", lin.MrFamily);
    }

    [Fact]
    public async Task ListAvailableAsync_scipy_bvp_poisson_mesh_richardson_descriptor_has_expected_metadata()
    {
        var descriptors = await _launcher.ListAvailableAsync();
        var conv = descriptors.Single(d => d.Id == "scipy-bvp-poisson-seed-mesh-insensitivity");

        Assert.Equal("scipy-bvp-poisson-1d", conv.SutName);
        Assert.Equal("ScaleField", conv.TransformationName);
        Assert.Equal("ApproxEqual", conv.AssertionName);
        Assert.Equal("u_max", conv.ValueName);
        Assert.Equal("2", conv.DefaultParameters["factor"]);
        Assert.Contains("mesh", conv.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ScipyBvp.Poisson.Convergence.SeedMesh", conv.MrFamily);
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
    public async Task ListAvailableAsync_exposes_four_single_program_boltzmann_mrs_with_expected_program_types()
    {
        // Categorical (set-based) assertion that complements the positional-index test
        // `ListAvailableAsync_returns_known_scenarios_in_id_order`. The positional test pins
        // ordering and breaks loudly when SUTs are added/removed; this one survives ordering
        // changes but guards that the 4 single-program Boltzmann MR ids remain discoverable.
        var descriptors = await _launcher.ListAvailableAsync();

        var boltzmannIds = new[]
        {
            "openmc-pincell-nu-sigma-f",
            "openmc-pincell-sigma-a",
            "openmoc-pincell-nu-sigma-f",
            "openmoc-pincell-sigma-a",
        };

        var bySut = descriptors
            .Where(d => boltzmannIds.Contains(d.Id, StringComparer.Ordinal))
            .ToDictionary(d => d.Id, d => d.SutName);

        Assert.Equal(4, bySut.Count);
        Assert.Equal("openmc", bySut["openmc-pincell-nu-sigma-f"]);
        Assert.Equal("openmc", bySut["openmc-pincell-sigma-a"]);
        Assert.Equal("openmoc", bySut["openmoc-pincell-nu-sigma-f"]);
        Assert.Equal("openmoc", bySut["openmoc-pincell-sigma-a"]);
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
