using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class DockerMcpProcessExecutorTests
{
    [Fact]
    public async Task Run_async_converts_invocation_to_mcp_run_sut_command()
    {
        var client = new RecordingDockerMcpRuntimeClient(new DockerMcpRunResult(
            ExitCode: 0,
            Stdout: "ok",
            Stderr: "",
            TimedOut: false));
        var options = new DockerMcpRuntimeOptions(
            Endpoint: "http://192.168.1.20:8765",
            Image: "metbench-sut:latest",
            PythonExecutable: "/opt/metbench-tools/openmoc-runner",
            ToolName: "openmoc-runner",
            LocalExecutable: "/host/openmoc-runner");
        var executor = new DockerMcpProcessExecutor(client);

        var result = await executor.RunAsync(
            options,
            new ProcessInvocation(
                "/host/openmoc-runner",
                new[]
                {
                    "--input",
                    "source-case.json",
                    "--output",
                    "source.out.json",
                }),
            timeoutSeconds: 60,
            CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("ok", result.Stdout);
        Assert.False(result.TimedOut);
        var request = Assert.Single(client.RunRequests);
        Assert.Same(options, request.Options);
        Assert.Equal(60, request.Request.TimeoutSeconds);
        Assert.Equal("metbench-sut:latest", request.Request.Image);
        Assert.Equal("openmoc-runner", request.Request.Tool);
        Assert.Equal(new[]
        {
            "--input",
            "source-case.json",
            "--output",
            "source.out.json",
        }, request.Request.Args);
    }

    [Fact]
    public async Task Run_async_maps_timeout_to_process_result_timeout()
    {
        var client = new RecordingDockerMcpRuntimeClient(new DockerMcpRunResult(
            ExitCode: -1,
            Stdout: "",
            Stderr: "timed out",
            TimedOut: true));
        var executor = new DockerMcpProcessExecutor(client);
        var options = new DockerMcpRuntimeOptions(
            Endpoint: "http://127.0.0.1:8765",
            Image: "metbench-sut:latest",
            PythonExecutable: "/opt/metbench-tools/openmoc-runner",
            ToolName: "openmoc-runner",
            LocalExecutable: "openmoc-runner");

        var result = await executor.RunAsync(
            options,
            new ProcessInvocation("openmoc-runner", Array.Empty<string>()),
            1,
            CancellationToken.None);

        Assert.True(result.TimedOut);
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("timed out", result.Stderr);
    }

    [Fact]
    public async Task RunAsync_rejects_empty_executable_before_calling_mcp_client()
    {
        var client = new RecordingClient();
        var executor = new DockerMcpProcessExecutor(client);
        var options = new DockerMcpRuntimeOptions(
            "http://127.0.0.1:8765",
            "img",
            "/opt/metbench-tools/openmoc-runner",
            ToolName: "openmoc-runner",
            LocalExecutable: "openmoc-runner");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            executor.RunAsync(
                options,
                new ProcessInvocation("", Array.Empty<string>()),
                30,
                CancellationToken.None));

        Assert.Null(client.LastRequest);
    }

    [Fact]
    public async Task RunAsync_rejects_commands_that_do_not_match_configured_local_executable()
    {
        var client = new RecordingClient();
        var executor = new DockerMcpProcessExecutor(client);
        var options = new DockerMcpRuntimeOptions(
            "http://127.0.0.1:8765",
            "metbench-sut:latest",
            "/opt/metbench-tools/openmoc-runner",
            ToolName: "openmoc-runner",
            LocalExecutable: "/host/openmoc-runner");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            executor.RunAsync(
                options,
                new ProcessInvocation("/bin/sh", new[] { "-c", "id" }),
                timeoutSeconds: 60,
                CancellationToken.None));

        Assert.Null(client.LastRequest);
    }

    [Theory]
    [InlineData("-c")]
    [InlineData("/c")]
    [InlineData("-m")]
    [InlineData("/m")]
    [InlineData("runner.py")]
    [InlineData("../secret.json")]
    [InlineData("&&")]
    [InlineData("|")]
    [InlineData("$(id)")]
    [InlineData("`id`")]
    public async Task RunAsync_rejects_unsafe_args_before_calling_mcp_client(string unsafeArg)
    {
        var client = new RecordingClient();
        var executor = new DockerMcpProcessExecutor(client);
        var options = new DockerMcpRuntimeOptions(
            "http://127.0.0.1:8765",
            "metbench-sut:latest",
            "/opt/metbench-tools/openmoc-runner",
            ToolName: "openmoc-runner",
            LocalExecutable: "/host/openmoc-runner",
            PathStyle: DockerMcpPathStyle.Wsl);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            executor.RunAsync(
                options,
                new ProcessInvocation("/host/openmoc-runner", new[] { unsafeArg }),
                timeoutSeconds: 60,
                CancellationToken.None));

        Assert.Null(client.LastRequest);
    }

    [Theory]
    [InlineData(@"D:\Codes\MetBench\SUT\openmc\openmc_runner.py", "/mnt/d/Codes/MetBench/SUT/openmc/openmc_runner.py")]
    [InlineData("c:/Users/lemon/AppData/Local/Temp/x.json", "/mnt/c/Users/lemon/AppData/Local/Temp/x.json")]
    [InlineData("--input", "--input")]
    [InlineData("/opt/openmc-venv/bin/python", "/opt/openmc-venv/bin/python")]
    [InlineData("5000", "5000")]
    public void TranslateWindowsPathToWsl_translates_only_windows_absolute_paths(
        string token, string expected)
    {
        Assert.Equal(expected, DockerMcpProcessExecutor.TranslateWindowsPathToWsl(token));
    }

    [Fact]
    public async Task RunAsync_translates_argv_when_path_style_is_wsl()
    {
        var client = new RecordingClient();
        var executor = new DockerMcpProcessExecutor(client);
        var options = new DockerMcpRuntimeOptions(
            "http://127.0.0.1:8765",
            "img",
            "/opt/metbench-tools/openmc-runner",
            ToolName: "openmc-runner",
            LocalExecutable: "openmc-runner",
            PathStyle: DockerMcpPathStyle.Wsl);

        await executor.RunAsync(
            options,
                new ProcessInvocation(
                    "openmc-runner",
                    new[] { "--input", @"C:\Users\lemon\AppData\Local\Temp\source.json" }),
            30,
            CancellationToken.None);

        Assert.Equal(
            new[]
                {
                    "--input",
                    "/mnt/c/Users/lemon/AppData/Local/Temp/source.json",
                },
            client.LastRequest!.Args);
    }

    [Fact]
    public async Task RunAsync_keeps_argv_untranslated_when_path_style_is_none()
    {
        var client = new RecordingClient();
        var executor = new DockerMcpProcessExecutor(client);
        var options = new DockerMcpRuntimeOptions(
            "http://127.0.0.1:8765",
            "img",
            "/opt/metbench-tools/openmc-runner",
            ToolName: "openmc-runner",
            LocalExecutable: "openmc-runner");

        await executor.RunAsync(
            options,
            new ProcessInvocation("openmc-runner", new[] { "--output", "out.json" }),
            30,
            CancellationToken.None);

        Assert.Equal(new[] { "--output", "out.json" }, client.LastRequest!.Args);
    }

    private sealed class RecordingClient : IDockerMcpRuntimeClient
    {
        public DockerMcpRunRequest? LastRequest;

        public Task<DockerMcpHealthResult> HealthAsync(
            DockerMcpRuntimeOptions options, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DockerMcpRunResult> RunSutCommandAsync(
            DockerMcpRuntimeOptions options,
            DockerMcpRunRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new DockerMcpRunResult(0, string.Empty, string.Empty, false));
        }
    }

    private sealed class RecordingDockerMcpRuntimeClient : IDockerMcpRuntimeClient
    {
        private readonly DockerMcpRunResult _runResult;

        public RecordingDockerMcpRuntimeClient(DockerMcpRunResult runResult)
        {
            _runResult = runResult;
        }

        public List<RunRequest> RunRequests { get; } = new();

        public Task<DockerMcpHealthResult> HealthAsync(
            DockerMcpRuntimeOptions options,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DockerMcpHealthResult(true, "ok", "ok"));

        public Task<DockerMcpRunResult> RunSutCommandAsync(
            DockerMcpRuntimeOptions options,
            DockerMcpRunRequest request,
            CancellationToken cancellationToken = default)
        {
            RunRequests.Add(new RunRequest(options, request));
            return Task.FromResult(_runResult);
        }
    }

    private sealed record RunRequest(
        DockerMcpRuntimeOptions Options,
        DockerMcpRunRequest Request);
}
