using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

public sealed class SystemMtScenarioLauncherTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDbSystemMtResultRepository _repository;
    private readonly SystemMtScenarioLauncher _launcher;

    public SystemMtScenarioLauncherTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchLauncherTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _repository = new LiteDbSystemMtResultRepository(_dbPath);
        _launcher = new SystemMtScenarioLauncher(
            new LauncherOptions(
                SutRoot: TestAssetPaths.AssetRoot(),
                SystemPython: TestAssetPaths.PythonExecutable(),
                OpenMocPython: TestAssetPaths.PythonExecutable()),
            _repository);
    }

    public void Dispose()
    {
        _repository.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var logFile = _dbPath + "-log";
        if (File.Exists(logFile)) File.Delete(logFile);
    }

    [Fact]
    public void Constructor_rejects_null_options()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtScenarioLauncher(null!, _repository));
    }

    [Fact]
    public void Constructor_rejects_null_repository()
    {
        var options = new LauncherOptions("/tmp", "python3", "python3");
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtScenarioLauncher(options, null!));
    }

    [Fact]
    public async Task ListAvailableAsync_returns_known_scenarios_in_id_order()
    {
        var descriptors = await _launcher.ListAvailableAsync();

        Assert.Equal(3, descriptors.Count);
        Assert.Equal("heat-equation-amplitude", descriptors[0].Id);
        Assert.Equal("openmoc-pincell-nu-sigma-f", descriptors[1].Id);
        Assert.Equal("openmoc-pincell-sigma-a", descriptors[2].Id);
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
    public async Task RunAsync_heat_equation_with_default_factor_passes_and_persists()
    {
        var result = await _launcher.RunAsync("heat-equation-amplitude");

        Assert.True(result.Passed, result.FailureReason);
        Assert.False(string.IsNullOrEmpty(result.RecordId));
        Assert.Equal("heat-equation-amplitude", result.ScenarioId);
        Assert.Equal("max_u", result.ValueName);
        Assert.True(result.SourceValue > 0);
        Assert.True(result.FollowUpValue > result.SourceValue,
            $"follow-up max_u ({result.FollowUpValue}) should exceed source ({result.SourceValue}) with factor=2");

        var persisted = await _repository.GetAsync(result.RecordId);
        Assert.NotNull(persisted);
        Assert.Equal("1D heat equation — ScaleAmplitude (linearity)", persisted!.ScenarioName);
        Assert.Equal("ScaleAmplitude", persisted.TransformationName);
        Assert.Equal("2", persisted.TransformationParameters!["factor"]);
        Assert.True(persisted.Passed);
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

        var persisted = await _repository.GetAsync(result.RecordId);
        Assert.Equal("3", persisted!.TransformationParameters!["factor"]);
    }

    [Fact]
    public async Task RunAsync_persists_failure_when_assertion_fails()
    {
        // factor=0.5 halves the amplitude, so follow-up max_u < source max_u,
        // which violates the GreaterThan assertion.
        var result = await _launcher.RunAsync(
            "heat-equation-amplitude",
            new Dictionary<string, string> { ["factor"] = "0.5" });

        Assert.False(result.Passed);
        Assert.False(string.IsNullOrEmpty(result.FailureReason));
        Assert.True(result.FollowUpValue < result.SourceValue);

        var persisted = await _repository.GetAsync(result.RecordId);
        Assert.NotNull(persisted);
        Assert.False(persisted!.Passed);
        Assert.Equal(result.FailureReason, persisted.FailureReason);
    }

    [Fact]
    public async Task RunAsync_two_runs_create_two_persisted_records()
    {
        await _launcher.RunAsync("heat-equation-amplitude");
        await _launcher.RunAsync("heat-equation-amplitude");

        var recent = await _repository.ListByScenarioAsync(
            "1D heat equation — ScaleAmplitude (linearity)",
            limit: 10);
        Assert.Equal(2, recent.Count);
    }
}
