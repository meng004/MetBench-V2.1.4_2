using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class DockerMcpProcessExecutorTests
{
    [Fact]
    public async Task Run_async_converts_shell_command_to_mcp_run_sut_command()
    {
        var client = new RecordingDockerMcpRuntimeClient(new DockerMcpRunResult(
            ExitCode: 0,
            Stdout: "ok",
            Stderr: "",
            TimedOut: false));
        var options = new DockerMcpRuntimeOptions(
            Endpoint: "http://192.168.1.20:8765",
            Image: "metbench-sut:latest",
            PythonExecutable: "/opt/openmoc-venv/bin/python");
        var executor = new DockerMcpProcessExecutor(client);

        var result = await executor.RunAsync(
            options,
            "\"/opt/openmoc-venv/bin/python\" \"SUT/openmoc/runner.py\" --input \"/tmp/source case.json\" --output \"/tmp/source.out.json\"",
            timeoutSeconds: 60,
            CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("ok", result.Stdout);
        Assert.False(result.TimedOut);
        var request = Assert.Single(client.RunRequests);
        Assert.Same(options, request.Options);
        Assert.Equal(60, request.TimeoutSeconds);
        Assert.Equal(new[]
        {
            "/opt/openmoc-venv/bin/python",
            "SUT/openmoc/runner.py",
            "--input",
            "/tmp/source case.json",
            "--output",
            "/tmp/source.out.json",
        }, request.Argv);
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
            PythonExecutable: "python");

        var result = await executor.RunAsync(options, "python runner.py", 1, CancellationToken.None);

        Assert.True(result.TimedOut);
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("timed out", result.Stderr);
    }

    [Fact]
    public void SplitCommand_preserves_backslashes_in_quoted_windows_paths()
    {
        var argv = DockerMcpProcessExecutor.SplitCommand(
            "\"python\" \"D:\\repo\\runner.py\" --input \"C:\\Temp\\in.json\"");

        Assert.Equal(
            new[] { "python", @"D:\repo\runner.py", "--input", @"C:\Temp\in.json" },
            argv);
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
            "http://127.0.0.1:8765", "img", "/opt/venv/bin/python",
            PathStyle: DockerMcpPathStyle.Wsl);

        await executor.RunAsync(
            options,
            "\"/opt/venv/bin/python\" \"D:\\repo\\SUT\\runner.py\" --input \"C:\\Temp\\in.json\"",
            30,
            CancellationToken.None);

        Assert.Equal(
            new[]
            {
                "/opt/venv/bin/python",
                "/mnt/d/repo/SUT/runner.py",
                "--input",
                "/mnt/c/Temp/in.json",
            },
            client.LastArgv);
    }

    [Fact]
    public async Task RunAsync_keeps_argv_untranslated_when_path_style_is_none()
    {
        var client = new RecordingClient();
        var executor = new DockerMcpProcessExecutor(client);
        var options = new DockerMcpRuntimeOptions(
            "http://127.0.0.1:8765", "img", "python");

        await executor.RunAsync(options, "python \"D:\\repo\\runner.py\"", 30, CancellationToken.None);

        Assert.Equal(new[] { "python", @"D:\repo\runner.py" }, client.LastArgv);
    }

    private sealed class RecordingClient : IDockerMcpRuntimeClient
    {
        public IReadOnlyList<string>? LastArgv;

        public Task<DockerMcpHealthResult> HealthAsync(
            DockerMcpRuntimeOptions options, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DockerMcpRunResult> RunSutCommandAsync(
            DockerMcpRuntimeOptions options,
            IReadOnlyList<string> argv,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            LastArgv = argv;
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
            IReadOnlyList<string> argv,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            RunRequests.Add(new RunRequest(options, argv.ToArray(), timeoutSeconds));
            return Task.FromResult(_runResult);
        }
    }

    private sealed record RunRequest(
        DockerMcpRuntimeOptions Options,
        IReadOnlyList<string> Argv,
        int TimeoutSeconds);
}
