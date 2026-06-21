using System.Reflection;
using System.Runtime.CompilerServices;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class RuntimeProcessExecutorRegistryTests
{
    [Fact]
    public void SystemMtPipeline_source_does_not_expose_docker_mcp_process_executor()
    {
        var sourcePath = Path.Combine(
            SolutionRoot(),
            "MetBench_BLL.Core",
            "SystemMT",
            "Pipeline",
            "SystemMtPipeline.cs");

        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("DockerMcpProcessExecutor", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(RuntimeKind.LocalPython)]
    [InlineData(RuntimeKind.PythonVirtualEnvironment)]
    public async Task RunAsync_dispatches_local_runtime_profiles_to_process_executor(RuntimeKind kind)
    {
        var local = new RecordingProcessExecutor(new ProcessResult(0, "local", "", TimeSpan.FromMilliseconds(2), false));
        var docker = new RecordingDockerMcpRuntimeClient(new DockerMcpRunResult(0, "docker", "", TimedOut: false));
        IRuntimeProcessExecutor registry = new RuntimeProcessExecutorRegistry(
            new LocalRuntimeProcessExecutor(local),
            new DockerRuntimeProcessExecutor(new DockerMcpProcessExecutor(docker)));
        var profile = new RuntimeProfile("system", "System Python", kind, "python");

        var result = await registry.RunAsync(
            profile,
            new ProcessInvocation("python", new[] { "runner.py" }),
            "/tmp/work",
            timeoutSeconds: 12,
            CancellationToken.None);

        Assert.Equal("local", result.Stdout);
        var request = Assert.Single(local.RunRequests);
        Assert.Equal("python", request.Invocation.FileName);
        Assert.Equal(new[] { "runner.py" }, request.Invocation.Arguments);
        Assert.Equal("/tmp/work", request.WorkingDirectory);
        Assert.Equal(12, request.TimeoutSeconds);
        Assert.Empty(docker.RunRequests);
    }

    [Fact]
    public async Task RunAsync_dispatches_docker_runtime_profiles_to_docker_mcp_executor()
    {
        var local = new RecordingProcessExecutor(new ProcessResult(0, "local", "", TimeSpan.Zero, false));
        var docker = new RecordingDockerMcpRuntimeClient(new DockerMcpRunResult(0, "docker", "", TimedOut: false));
        IRuntimeProcessExecutor registry = new RuntimeProcessExecutorRegistry(
            new LocalRuntimeProcessExecutor(local),
            new DockerRuntimeProcessExecutor(new DockerMcpProcessExecutor(docker)));
        var options = new DockerMcpRuntimeOptions(
            "http://127.0.0.1:8765",
            "metbench-sut:latest",
            "/opt/venv/bin/python",
            ToolName: "openmoc-runner",
            LocalExecutable: "/host/openmoc-runner");
        var profile = new RuntimeProfile(
            "openmoc-docker",
            "OpenMOC Docker",
            RuntimeKind.Docker,
            "/host/openmoc-runner",
            dockerMcp: options);

        var result = await registry.RunAsync(
            profile,
            new ProcessInvocation("/host/openmoc-runner", new[] { "--input", "source.json" }),
            "/tmp/work",
            timeoutSeconds: 30,
            CancellationToken.None);

        Assert.Equal("docker", result.Stdout);
        Assert.Empty(local.RunRequests);
        var request = Assert.Single(docker.RunRequests);
        Assert.Same(options, request.Options);
        Assert.Equal("metbench-sut:latest", request.Request.Image);
        Assert.Equal("openmoc-runner", request.Request.Tool);
        Assert.Equal(new[] { "--input", "source.json" }, request.Request.Args);
        Assert.Equal(30, request.Request.TimeoutSeconds);
    }

    [Theory]
    [InlineData(RuntimeKind.DockerPlaceholder)]
    [InlineData(RuntimeKind.RemotePlaceholder)]
    [InlineData(RuntimeKind.HpcPlaceholder)]
    public async Task RunAsync_fails_closed_for_unsupported_runtime_kinds(RuntimeKind kind)
    {
        IRuntimeProcessExecutor registry = new RuntimeProcessExecutorRegistry(
            new LocalRuntimeProcessExecutor(new RecordingProcessExecutor()),
            new DockerRuntimeProcessExecutor(new DockerMcpProcessExecutor(new RecordingDockerMcpRuntimeClient())));
        var profile = RuntimeProfile.Placeholder("placeholder", "Placeholder", kind);

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() =>
            registry.RunAsync(
                profile,
                new ProcessInvocation("python", Array.Empty<string>()),
                "/tmp/work",
                timeoutSeconds: 5,
                CancellationToken.None));

        Assert.Contains(kind.ToString(), ex.Message);
    }

    [Fact]
    public async Task RunAsync_fails_closed_for_docker_profile_without_options()
    {
        IRuntimeProcessExecutor registry = new RuntimeProcessExecutorRegistry(
            new LocalRuntimeProcessExecutor(new RecordingProcessExecutor()),
            new DockerRuntimeProcessExecutor(new DockerMcpProcessExecutor(new RecordingDockerMcpRuntimeClient())));
        var profile = CreateInvalidDockerProfileWithoutOptions();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            registry.RunAsync(
                profile,
                new ProcessInvocation("python", Array.Empty<string>()),
                "/tmp/work",
                timeoutSeconds: 5,
                CancellationToken.None));

        Assert.Contains("Docker MCP options", ex.Message);
    }

    private static RuntimeProfile CreateInvalidDockerProfileWithoutOptions()
    {
        var profile = (RuntimeProfile)RuntimeHelpers.GetUninitializedObject(typeof(RuntimeProfile));
        SetBackingField(profile, nameof(RuntimeProfile.RuntimeKey), "broken-docker");
        SetBackingField(profile, nameof(RuntimeProfile.DisplayName), "Broken Docker");
        SetBackingField(profile, nameof(RuntimeProfile.Kind), RuntimeKind.Docker);
        SetBackingField(profile, nameof(RuntimeProfile.ExecutablePath), "python");
        SetBackingField<DockerMcpRuntimeOptions?>(profile, nameof(RuntimeProfile.DockerMcp), null);
        return profile;
    }

    private static void SetBackingField<T>(RuntimeProfile profile, string propertyName, T value)
    {
        var field = typeof(RuntimeProfile).GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(profile, value);
    }

    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate solution root from {AppContext.BaseDirectory}.");
    }

    private sealed class RecordingProcessExecutor : IProcessExecutor
    {
        private readonly ProcessResult _result;

        public RecordingProcessExecutor()
            : this(new ProcessResult(0, "", "", TimeSpan.Zero, false))
        {
        }

        public RecordingProcessExecutor(ProcessResult result)
        {
            _result = result;
        }

        public List<RunRequest> RunRequests { get; } = new();

        public Task<ProcessResult> RunAsync(
            ProcessInvocation invocation,
            string workingDirectory,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            RunRequests.Add(new RunRequest(invocation, workingDirectory, timeoutSeconds));
            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingDockerMcpRuntimeClient : IDockerMcpRuntimeClient
    {
        private readonly DockerMcpRunResult _runResult;

        public RecordingDockerMcpRuntimeClient()
            : this(new DockerMcpRunResult(0, "", "", TimedOut: false))
        {
        }

        public RecordingDockerMcpRuntimeClient(DockerMcpRunResult runResult)
        {
            _runResult = runResult;
        }

        public List<DockerRunRequest> RunRequests { get; } = new();

        public Task<DockerMcpHealthResult> HealthAsync(
            DockerMcpRuntimeOptions options,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DockerMcpHealthResult(true, "ok", "ok"));

        public Task<DockerMcpRunResult> RunSutCommandAsync(
            DockerMcpRuntimeOptions options,
            DockerMcpRunRequest request,
            CancellationToken cancellationToken = default)
        {
            RunRequests.Add(new DockerRunRequest(options, request));
            return Task.FromResult(_runResult);
        }
    }

    private sealed record RunRequest(
        ProcessInvocation Invocation,
        string WorkingDirectory,
        int TimeoutSeconds);

    private sealed record DockerRunRequest(
        DockerMcpRuntimeOptions Options,
        DockerMcpRunRequest Request);
}
