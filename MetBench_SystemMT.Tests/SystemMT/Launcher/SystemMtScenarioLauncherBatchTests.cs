using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

public sealed class SystemMtScenarioLauncherBatchTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDbSystemMtResultRepository _repository;
    private readonly SystemMtScenarioLauncher _launcher;

    public SystemMtScenarioLauncherBatchTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchLauncherBatchTests",
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

    private static BatchScenarioRequest Req(string id, string? factor = null) =>
        new(id, factor is null ? null : new Dictionary<string, string> { ["factor"] = factor });

    [Fact]
    public async Task RunBatchAsync_null_requests_throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _launcher.RunBatchAsync(null!));
    }

    [Fact]
    public async Task RunBatchAsync_empty_requests_returns_empty()
    {
        var results = await _launcher.RunBatchAsync(Array.Empty<BatchScenarioRequest>());
        Assert.Empty(results);
    }

    [Fact]
    public async Task RunBatchAsync_pre_validates_blank_id_before_running_any()
    {
        var requests = new[]
        {
            Req("heat-equation-amplitude"),
            Req(""),                                  // ← invalid
            Req("heat-equation-amplitude"),
        };

        var error = await Assert.ThrowsAsync<ArgumentException>(() => _launcher.RunBatchAsync(requests));
        Assert.Contains("index 1", error.Message);
        Assert.Contains("blank", error.Message, StringComparison.OrdinalIgnoreCase);

        // Repository must be empty — no scenarios should have run.
        var recent = await _repository.ListRecentAsync(10);
        Assert.Empty(recent);
    }

    [Fact]
    public async Task RunBatchAsync_pre_validates_unknown_id_before_running_any()
    {
        var requests = new[]
        {
            Req("heat-equation-amplitude"),
            Req("not-a-real-scenario"),
        };

        var error = await Assert.ThrowsAsync<ArgumentException>(() => _launcher.RunBatchAsync(requests));
        Assert.Contains("index 1", error.Message);
        Assert.Contains("not-a-real-scenario", error.Message);

        var recent = await _repository.ListRecentAsync(10);
        Assert.Empty(recent);
    }

    [Fact]
    public async Task RunBatchAsync_single_request_runs_once()
    {
        var results = await _launcher.RunBatchAsync(new[] { Req("heat-equation-amplitude") });

        Assert.Single(results);
        Assert.Equal("heat-equation-amplitude", results[0].ScenarioId);
        Assert.True(results[0].Passed, results[0].FailureReason);
    }

    [Fact]
    public async Task RunBatchAsync_multiple_requests_run_in_order_and_all_persist()
    {
        var requests = new[]
        {
            Req("heat-equation-amplitude", "2"),
            Req("heat-equation-amplitude", "3"),
            Req("heat-equation-amplitude", "2"),
        };

        var results = await _launcher.RunBatchAsync(requests);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal("heat-equation-amplitude", r.ScenarioId));
        Assert.All(results, r => Assert.True(r.Passed));

        // Each persisted with its own RecordId
        Assert.Equal(3, results.Select(r => r.RecordId).Distinct().Count());

        var recent = await _repository.ListRecentAsync(10);
        Assert.Equal(3, recent.Count);
    }

    [Fact]
    public async Task RunBatchAsync_failure_in_one_scenario_does_not_stop_others()
    {
        // factor=0.5 fails GreaterThan assertion; factor=2 passes. Mix them.
        var requests = new[]
        {
            Req("heat-equation-amplitude", "2"),       // PASS
            Req("heat-equation-amplitude", "0.5"),     // FAIL (legit MR violation, not infra)
            Req("heat-equation-amplitude", "2"),       // PASS
        };

        var results = await _launcher.RunBatchAsync(requests);

        Assert.Equal(3, results.Count);
        Assert.True(results[0].Passed);
        Assert.False(results[1].Passed);
        Assert.True(results[2].Passed);
        Assert.False(string.IsNullOrEmpty(results[1].FailureReason));
    }

    [Fact]
    public async Task RunBatchAsync_progress_callbacks_fire_twice_per_scenario()
    {
        var events = new List<BatchProgress>();
        var progress = new Progress<BatchProgress>(events.Add);
        var requests = new[]
        {
            Req("heat-equation-amplitude"),
            Req("heat-equation-amplitude"),
        };

        await _launcher.RunBatchAsync(requests, progress);

        // Wait briefly for Progress<T> dispatch (it uses SynchronizationContext)
        await Task.Delay(50);

        // 2 scenarios × 2 events (start + end) = 4 events
        Assert.Equal(4, events.Count);

        Assert.Equal(0, events[0].Completed);
        Assert.Equal(2, events[0].Total);
        Assert.Null(events[0].LastResult);

        Assert.Equal(1, events[1].Completed);
        Assert.NotNull(events[1].LastResult);

        Assert.Equal(1, events[2].Completed);
        Assert.Null(events[2].LastResult);

        Assert.Equal(2, events[3].Completed);
        Assert.NotNull(events[3].LastResult);
    }

    [Fact]
    public async Task RunBatchAsync_cancellation_before_run_throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _launcher.RunBatchAsync(
                new[] { Req("heat-equation-amplitude") },
                progress: null,
                cancellationToken: cts.Token));

        var recent = await _repository.ListRecentAsync(10);
        Assert.Empty(recent);
    }
}
