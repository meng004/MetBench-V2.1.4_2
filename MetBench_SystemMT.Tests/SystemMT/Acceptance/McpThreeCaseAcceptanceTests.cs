using System.Collections.Generic;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using MetBench_SystemMT.Tests.SystemMT;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Acceptance;

/// <summary>
/// LAN acceptance for the three MCP server deployments (spec
/// docs/superpowers/specs/2026-06-12-mcp-three-case-acceptance-design.md §8/§9).
/// Skips unless METBENCH_MCP_ACCEPTANCE_* point at a live server started per
/// docs/uat/mcp-three-case-acceptance-runbook.md.
/// </summary>
public sealed class McpThreeCaseAcceptanceTests
{
    private const string SkipReason =
        "MCP acceptance env is not configured. Set METBENCH_MCP_ACCEPTANCE_URI, "
        + "METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY and METBENCH_MCP_ACCEPTANCE_MR.";

    private static string? Env(string name) => System.Environment.GetEnvironmentVariable(name);

    private static bool Configured =>
        !string.IsNullOrWhiteSpace(Env("METBENCH_MCP_ACCEPTANCE_URI"))
        && !string.IsNullOrWhiteSpace(Env("METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY"))
        && !string.IsNullOrWhiteSpace(Env("METBENCH_MCP_ACCEPTANCE_MR"));

    private static LauncherOptions AcceptanceOptions()
    {
        var uri = Env("METBENCH_MCP_ACCEPTANCE_URI")!;
        var key = Env("METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY")!;
        var sutRoot = Env("METBENCH_MCP_ACCEPTANCE_SUTROOT") ?? TestAssetPaths.AssetRoot();
        var python = TestAssetPaths.PythonExecutable();

        return new LauncherOptions(
            SutRoot: sutRoot,
            SystemPython: python,
            OpenMocPython: python,
            RuntimePythons: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = uri,
            });
    }

    private static SystemMtLauncher BuildAcceptanceLauncher()
    {
        var options = AcceptanceOptions();
        return new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            new RecordingAnomalyService(),
            new ManifestMrCatalogProvider(options));
    }

    [SkippableFact]
    public async Task Acceptance_1_preflight_health_reaches_live_server()
    {
        Skip.IfNot(Configured, SkipReason);

        var key = Env("METBENCH_MCP_ACCEPTANCE_RUNTIME_KEY")!;
        var options = AcceptanceOptions();
        var profile = new LauncherOptionsRuntimeProfileProvider(options).GetProfile(key);

        Assert.NotNull(profile.DockerMcp);

        var health = await new DockerMcpRuntimeClient().HealthAsync(profile.DockerMcp!);

        Assert.True(health.Available,
            $"Docker MCP health check failed. Status='{health.Status}', Detail='{health.Detail}'");
        Assert.Equal("ok", health.Status, ignoreCase: true);
    }

    [SkippableFact]
    public async Task Acceptance_2_launcher_runs_mr_end_to_end_through_mcp()
    {
        Skip.IfNot(Configured, SkipReason);

        var mrId = Env("METBENCH_MCP_ACCEPTANCE_MR")!;
        var results = new FakeResultRepo();
        var options = AcceptanceOptions();
        var launcher = new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(new FakeExecRepo(), results),
            new RecordingAnomalyService(),
            new ManifestMrCatalogProvider(options));

        var result = await launcher.RunAsync(mrId);

        Assert.True(result.Passed, "FailureReason: " + result.FailureReason);
        Assert.Single(results.Data);
    }

    [SkippableFact]
    public async Task Acceptance_3_async_job_reaches_succeeded_through_mcp()
    {
        Skip.IfNot(Configured, SkipReason);

        var mrId = Env("METBENCH_MCP_ACCEPTANCE_MR")!;

        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var worker = new SystemMtJobWorker(store, new SystemMtAsyncPipeline(BuildAcceptanceLauncher()));

        var handle = await service.SubmitAsync(new SystemMtJobRequest(mrId), default);
        var queuedId = await queue.DequeueAsync(default);

        Assert.Equal(handle.JobId, queuedId);

        await worker.RunJobAsync(queuedId, default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        var result = await service.GetResultAsync(handle.JobId, default);

        Assert.NotNull(status);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);
        Assert.NotNull(result);
        Assert.True(result!.Passed, result.FailureReason);
    }
}
