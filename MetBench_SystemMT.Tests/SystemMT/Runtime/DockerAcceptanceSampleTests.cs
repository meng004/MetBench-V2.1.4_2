using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class DockerAcceptanceSampleTests
{
    [Fact]
    public async Task Docker_acceptance_sample_validates_configuration_queue_record_and_fail_closed_execution_boundary()
    {
        var provider = new InMemoryRuntimeBackendConfigurationProvider(new[] { CreatePythonStdlibSample() });
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue, backendConfigurations: provider);

        var handle = await service.SubmitAsync(
            new SystemMtJobRequest(
                "docker-acceptance-python-identity",
                RuntimeBackendKey: "DOCKER-ACCEPTANCE-PYTHON-STDLIB"),
            default);

        var queued = await store.GetAsync(handle.JobId, default);
        Assert.Equal("docker", queued!.BackendKind);
        Assert.Equal("docker-acceptance-python-stdlib", queued.BackendExternalId);
        Assert.Equal(handle.JobId, await queue.DequeueAsync(default));

        var diagnostic = provider.Resolve("docker-acceptance-python-stdlib").ToSanitizedDiagnostic();
        Assert.Equal("python:3.12-slim", diagnostic["docker_image"]);
        Assert.Equal("configured-by-operator", diagnostic["secret_ref:METBENCH_DOCKER_SAMPLE_TOKEN"]);
        Assert.DoesNotContain(diagnostic, pair => pair.Value == "raw-secret-value");

        var launcher = new CapturingLauncher();
        var pipeline = new SystemMtAsyncPipeline(launcher);
        var outcome = await pipeline.ExecuteJobAsync(
            handle.JobId,
            new SystemMtJobRequest(
                "docker-acceptance-python-identity",
                RuntimeBackendKey: "docker-acceptance-python-stdlib"),
            null,
            default);

        Assert.Equal(SystemMtJobState.Failed, outcome.FinalState);
        Assert.Equal("MiddlewareUnavailable", outcome.FailureKind);
        Assert.Contains("backend executor", outcome.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.Null(launcher.LastMrId);
    }

    private static RuntimeBackendConfiguration CreatePythonStdlibSample() =>
        RuntimeBackendConfiguration.Docker(
            "docker-acceptance-python-stdlib",
            new DockerBackendConfiguration(
                image: "python:3.12-slim",
                commandTemplate: "python /workspace/run_identity.py --input /workspace/in/source.json --output /workspace/out/followup.json",
                workDirectory: "/workspace",
                environment: new Dictionary<string, string> { ["METBENCH_DOCKER_SAMPLE_TOKEN"] = "raw-secret-value" },
                secretReferences: new Dictionary<string, RuntimeSecretReference>
                {
                    ["METBENCH_DOCKER_SAMPLE_TOKEN"] = new("configured-by-operator")
                },
                inputMounts: new[] { RuntimePathMapping.Create("docker-acceptance/in", "/workspace/in") },
                outputMounts: new[] { RuntimePathMapping.Create("docker-acceptance/out", "/workspace/out") },
                timeout: TimeSpan.FromMinutes(5),
                killTimeout: TimeSpan.FromSeconds(10)));

    private sealed class CapturingLauncher : ISystemMtLauncher
    {
        public string? LastMrId { get; private set; }

        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrSummary>>(Array.Empty<MrSummary>());

        public Task<MrRunResult> RunAsync(
            string mrId,
            IReadOnlyDictionary<string, string>? ov = null,
            CancellationToken ct = default)
        {
            LastMrId = mrId;
            return Task.FromResult(new MrRunResult(
                RecordId: Guid.NewGuid().ToString(),
                MrId: mrId,
                Passed: true,
                FailureReason: string.Empty,
                ValueName: "identity",
                SourceValue: 1,
                FollowUpValue: 1,
                SourceElapsed: TimeSpan.Zero,
                FollowUpElapsed: TimeSpan.Zero));
        }

        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(
            IReadOnlyList<BatchMrRunRequest> r,
            IProgress<BatchProgress>? p = null,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MrRunResult>>(Array.Empty<MrRunResult>());
    }
}
