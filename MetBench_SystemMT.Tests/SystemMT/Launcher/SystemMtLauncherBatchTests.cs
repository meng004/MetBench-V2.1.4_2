using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

public sealed class SystemMtLauncherBatchTests
{
    private readonly FakeExecRepo _execs = new();
    private readonly FakeResultRepo _results = new();
    private readonly SystemMtExecutionRecorder _recorder;
    private readonly SystemMtPipeline _pipeline = new();
    private readonly RecordingAnomalyService _anomalyService = new();
    private readonly SystemMtLauncher _launcher;

    public SystemMtLauncherBatchTests()
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

    private static BatchMrRunRequest Req(string id, string? factor = null) =>
        new(id, factor is null ? null : new Dictionary<string, string> { ["factor"] = factor });

    [Fact]
    public async Task RunBatchAsync_null_requests_throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _launcher.RunBatchAsync(null!));
    }

    [Fact]
    public async Task RunBatchAsync_empty_requests_returns_empty()
    {
        var results = await _launcher.RunBatchAsync(Array.Empty<BatchMrRunRequest>());
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

        // 任何 scenario 都不应跑(pre-validation 在第一步)
        Assert.Empty(_execs.Data);
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

        Assert.Empty(_execs.Data);
    }

    [Fact]
    public async Task RunBatchAsync_single_request_runs_once()
    {
        var results = await _launcher.RunBatchAsync(new[] { Req("heat-equation-amplitude") });

        Assert.Single(results);
        Assert.Equal("heat-equation-amplitude", results[0].MrId);
        Assert.True(results[0].Passed, results[0].FailureReason);
        Assert.Single(_execs.Data);
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
        Assert.All(results, r => Assert.Equal("heat-equation-amplitude", r.MrId));
        Assert.All(results, r => Assert.True(r.Passed));

        // 每次 RecordId 唯一
        Assert.Equal(3, results.Select(r => r.RecordId).Distinct().Count());

        // v2 schema:3 Execution + 3 Result
        Assert.Equal(3, _execs.Data.Count);
        Assert.Equal(3, _results.Data.Count);
    }

    [Fact]
    public async Task RunBatchAsync_failure_in_one_scenario_does_not_stop_others()
    {
        // factor=0.5 fails "greater" assertion; factor=2 passes. Mix them.
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

        // 失败那次应建 1 个 Anomaly
        Assert.Single(_anomalyService.Recorded);
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

        Assert.Empty(_execs.Data);
    }
}
