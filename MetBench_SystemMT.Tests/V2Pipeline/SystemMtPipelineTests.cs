using System.Text.Json;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pipeline;

/// <summary>
/// TDD 验证 P4.3 SystemMtPipeline —— 9 状态机；用 FakeProcessExecutor 模拟
/// Python subprocess 调用，避免依赖 Python 环境。
/// </summary>
public sealed class SystemMtPipelineTests : IDisposable
{
    private readonly string _workDir;

    public SystemMtPipelineTests()
    {
        _workDir = Path.Combine(Path.GetTempPath(), "MetBenchV2PipelineTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);

        var sourcePath = Path.Combine(_workDir, "source.in.json");
        var sourceDict = new Dictionary<string, object?>
        {
            ["materials"] = new Dictionary<string, object?>
            {
                ["fuel"] = new Dictionary<string, object?>
                {
                    ["temperature_kelvin"] = 600.0,
                }
            }
        };
        File.WriteAllText(sourcePath, JsonSerializer.Serialize(sourceDict));
    }

    public void Dispose()
    {
        try { Directory.Delete(_workDir, recursive: true); } catch { /* swallow */ }
    }

    private PipelineContext MakeContext(string assertionTypeCode = "less")
    {
        return new PipelineContext(
            MrCode: "MR-T-test",
            TransformationName: "ScaleField",
            AssertionTypeCode: assertionTypeCode,
            ValueName: "k_eff",
            TargetFieldPath: "materials.fuel.temperature_kelvin",
            PathSyntax: "json-pointer",
            Parameters: new Dictionary<string, string> { ["factor"] = "1.5" },
            Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: "test-sut",
            SourceCasePath: Path.Combine(_workDir, "source.in.json"),
            WorkingDirectory: _workDir,
            InputParserInvocation: new ProcessInvocation("fake-input-parser", Array.Empty<string>()),
            OutputParserInvocation: new ProcessInvocation("fake-output-parser", Array.Empty<string>()),
            RunnerInvocation: new ProcessInvocation("fake-runner", Array.Empty<string>()),
            TimeoutSeconds: 30,
            CatalogVersionSha: "test-sha",
            SutVersionSnapshot: "test-sut-v1",
            MetbenchVersion: "v2.0",
            TriggeredBy: "test");
    }

    [Fact]
    public async Task Pipeline_happy_path_ends_in_ok()
    {
        // Fake executor:
        //   parse → 返回 source dict JSON
        //   write → 返回 OK
        //   runner source → 写 source.out.json
        //   runner followup → 写 followup.out.json
        //   parse-output × 2 → 返回 {values, metadata}
        var sourceOut = new
        {
            values = new Dictionary<string, double>
            {
                ["k_eff"] = 1.13,
            },
            metadata = new Dictionary<string, string> { ["adapter"] = "test" }
        };
        var followupOut = new
        {
            values = new Dictionary<string, double>
            {
                ["k_eff"] = 0.51,
            },
            metadata = new Dictionary<string, string> { ["adapter"] = "test" }
        };

        var fake = new FakeProcessExecutor(cmd =>
        {
            if (cmd.Contains("fake-input-parser parse"))
            {
                var data = new Dictionary<string, object?>
                {
                    ["materials"] = new Dictionary<string, object?>
                    {
                        ["fuel"] = new Dictionary<string, object?>
                        {
                            ["temperature_kelvin"] = 600.0
                        }
                    }
                };
                return new ProcessResult(0, JsonSerializer.Serialize(data), "", TimeSpan.FromMilliseconds(50), false);
            }
            if (cmd.Contains("fake-input-parser write"))
                return new ProcessResult(0, "{}", "", TimeSpan.FromMilliseconds(50), false);
            if (cmd.Contains("fake-runner"))
            {
                // 寫 output file
                var outPath = ExtractOutputArg(cmd);
                var which = cmd.Contains("source.in.json") ? sourceOut : (object)followupOut;
                File.WriteAllText(outPath, JsonSerializer.Serialize(which));
                return new ProcessResult(0, "", "", TimeSpan.FromMilliseconds(100), false);
            }
            if (cmd.Contains("fake-output-parser"))
            {
                var outPath = ExtractOutputFileArg(cmd);
                var content = File.ReadAllText(outPath);
                return new ProcessResult(0, content, "", TimeSpan.FromMilliseconds(20), false);
            }
            return new ProcessResult(1, "", "Unknown command", TimeSpan.Zero, false);
        });

        var pipeline = new SystemMtPipeline(fake);
        var outcome = await pipeline.ExecuteAsync(MakeContext("less"));

        Assert.Equal(PipelineStatus.Ok, outcome.FinalStatus);
        Assert.Null(outcome.ErrorMessage);
        Assert.NotNull(outcome.AssertionResult);
        Assert.True(outcome.AssertionResult!.Passed);
        Assert.Equal(1.13, outcome.SourceMetrics?["k_eff"]);
        Assert.Equal(0.51, outcome.FollowupMetrics?["k_eff"]);
    }

    [Fact]
    public async Task Pipeline_input_parser_failure_ends_in_error()
    {
        var fake = new FakeProcessExecutor(_ =>
            new ProcessResult(1, "", "parse error", TimeSpan.FromMilliseconds(10), false));
        var pipeline = new SystemMtPipeline(fake);
        var outcome = await pipeline.ExecuteAsync(MakeContext());
        Assert.Equal(PipelineStatus.Error, outcome.FinalStatus);
        Assert.NotNull(outcome.ErrorMessage);
        Assert.Contains("ParsingSource", outcome.ErrorMessage!);
    }

    [Fact]
    public async Task Pipeline_sut_timeout_ends_in_timeout()
    {
        var fake = new FakeProcessExecutor(cmd =>
        {
            if (cmd.Contains("fake-input-parser parse"))
            {
                var data = new Dictionary<string, object?>
                {
                    ["materials"] = new Dictionary<string, object?>
                    {
                        ["fuel"] = new Dictionary<string, object?>
                        {
                            ["temperature_kelvin"] = 600.0
                        }
                    }
                };
                return new ProcessResult(0, JsonSerializer.Serialize(data), "", TimeSpan.FromMilliseconds(10), false);
            }
            if (cmd.Contains("fake-input-parser write"))
                return new ProcessResult(0, "{}", "", TimeSpan.FromMilliseconds(10), false);
            if (cmd.Contains("fake-runner"))
                return new ProcessResult(-1, "", "Killed", TimeSpan.FromMilliseconds(30000), true);  // timed out
            return new ProcessResult(1, "", "", TimeSpan.Zero, false);
        });
        var pipeline = new SystemMtPipeline(fake);
        var outcome = await pipeline.ExecuteAsync(MakeContext());
        Assert.Equal(PipelineStatus.Timeout, outcome.FinalStatus);
    }

    [Fact]
    public async Task Docker_runtime_profile_routes_only_sut_runner_through_mcp()
    {
        var sourceOut = new
        {
            values = new Dictionary<string, double> { ["k_eff"] = 1.13 },
            metadata = new Dictionary<string, string> { ["adapter"] = "docker-test" }
        };
        var followupOut = new
        {
            values = new Dictionary<string, double> { ["k_eff"] = 0.51 },
            metadata = new Dictionary<string, string> { ["adapter"] = "docker-test" }
        };
        var local = new FakeProcessExecutor(cmd =>
        {
            if (cmd.Contains("fake-input-parser parse"))
            {
                var data = new Dictionary<string, object?>
                {
                    ["materials"] = new Dictionary<string, object?>
                    {
                        ["fuel"] = new Dictionary<string, object?> { ["temperature_kelvin"] = 600.0 }
                    }
                };
                return new ProcessResult(0, JsonSerializer.Serialize(data), "", TimeSpan.FromMilliseconds(10), false);
            }
            if (cmd.Contains("fake-input-parser write"))
                return new ProcessResult(0, "{}", "", TimeSpan.FromMilliseconds(10), false);
            if (cmd.Contains("fake-output-parser"))
            {
                var outPath = ExtractOutputFileArg(cmd);
                return new ProcessResult(0, File.ReadAllText(outPath), "", TimeSpan.FromMilliseconds(10), false);
            }
            return new ProcessResult(1, "", "Unexpected local command: " + cmd, TimeSpan.Zero, false);
        });
        var dockerClient = new DockerWritingClient(argv =>
        {
            var output = ArgAfter(argv, "--output");
            var data = ArgAfter(argv, "--input").Contains("source.in.json", StringComparison.Ordinal)
                ? sourceOut
                : (object)followupOut;
            File.WriteAllText(output, JsonSerializer.Serialize(data));
            return new DockerMcpRunResult(0, "", "", TimedOut: false);
        });
        var runtimeExecutor = new RuntimeProcessExecutorRegistry(
            new LocalRuntimeProcessExecutor(local),
            new DockerRuntimeProcessExecutor(new DockerMcpProcessExecutor(dockerClient)));
        var pipeline = new SystemMtPipeline(local, runtimeExecutor);
        var context = MakeContext("less") with
        {
            RuntimeProfile = new RuntimeProfile(
                "openmoc-docker",
                "openmoc-docker Docker MCP",
                RuntimeKind.Docker,
                "fake-runner",
                dockerMcp: new DockerMcpRuntimeOptions(
                    "http://192.168.1.20:8765",
                    "metbench-sut:latest",
                    "/opt/openmoc-venv/bin/python",
                    ToolName: "fake-runner",
                    LocalExecutable: "fake-runner")),
        };

        var outcome = await pipeline.ExecuteAsync(context);

        Assert.Equal(PipelineStatus.Ok, outcome.FinalStatus);
        Assert.Equal(2, dockerClient.RunRequests.Count);
        Assert.All(dockerClient.RunRequests, request =>
        {
            Assert.Equal("metbench-sut:latest", request.Options.Image);
            Assert.Equal("fake-runner", request.Request.Tool);
        });
    }

    [Fact]
    public async Task Remote_tool_docker_profile_routes_parsers_writer_and_runner_through_mcp()
    {
        var sourceOut = new
        {
            values = new Dictionary<string, double> { ["k_eff"] = 1.13 },
            metadata = new Dictionary<string, string> { ["adapter"] = "remote-tool-test" }
        };
        var followupOut = new
        {
            values = new Dictionary<string, double> { ["k_eff"] = 0.51 },
            metadata = new Dictionary<string, string> { ["adapter"] = "remote-tool-test" }
        };
        var local = new FakeProcessExecutor(cmd =>
            new ProcessResult(1, "", "Unexpected local command: " + cmd, TimeSpan.Zero, false));
        var dockerClient = new DockerWritingClient(argv =>
        {
            if (argv.Contains("parse") && argv.Contains("--input"))
            {
                var data = new Dictionary<string, object?>
                {
                    ["materials"] = new Dictionary<string, object?>
                    {
                        ["fuel"] = new Dictionary<string, object?> { ["temperature_kelvin"] = 600.0 }
                    }
                };
                return new DockerMcpRunResult(0, JsonSerializer.Serialize(data), "", TimedOut: false);
            }
            if (argv.Contains("write"))
                return new DockerMcpRunResult(0, "{}", "", TimedOut: false);
            if (argv.Contains("--input") && argv.Contains("--output"))
            {
                var output = ArgAfter(argv, "--output");
                var data = ArgAfter(argv, "--input").Contains("source.in.json", StringComparison.Ordinal)
                    ? sourceOut
                    : (object)followupOut;
                File.WriteAllText(output, JsonSerializer.Serialize(data));
                return new DockerMcpRunResult(0, "", "", TimedOut: false);
            }
            if (argv.Contains("parse") && argv.Contains("--output-file"))
            {
                var outPath = ArgAfter(argv, "--output-file");
                return new DockerMcpRunResult(0, File.ReadAllText(outPath), "", TimedOut: false);
            }

            return new DockerMcpRunResult(1, "", "Unexpected remote argv: " + string.Join(" ", argv), TimedOut: false);
        });
        var runtimeExecutor = new RuntimeProcessExecutorRegistry(
            new LocalRuntimeProcessExecutor(local),
            new DockerRuntimeProcessExecutor(new DockerMcpProcessExecutor(dockerClient)));
        var pipeline = new SystemMtPipeline(local, runtimeExecutor);
        var context = MakeContext("less") with
        {
            InputParserInvocation = new ProcessInvocation("input-parser", Array.Empty<string>()),
            OutputParserInvocation = new ProcessInvocation("output-parser", Array.Empty<string>()),
            RunnerInvocation = new ProcessInvocation("sut-runner", Array.Empty<string>()),
            RuntimeProfile = new RuntimeProfile(
                "system",
                "system Docker MCP",
                RuntimeKind.Docker,
                executablePath: null,
                dockerMcp: new DockerMcpRuntimeOptions(
                    "http://192.168.1.20:8765",
                    "metbench-sut:latest",
                    PythonExecutable: "")),
        };

        var outcome = await pipeline.ExecuteAsync(context);

        Assert.Equal(PipelineStatus.Ok, outcome.FinalStatus);
        Assert.Equal(
            new[]
            {
                "input-parser",
                "input-parser",
                "sut-runner",
                "sut-runner",
                "output-parser",
                "output-parser",
            },
            dockerClient.RunRequests.Select(request => request.Request.Tool).ToArray());
    }

    [Fact]
    public async Task Pipeline_progress_callback_receives_state_transitions()
    {
        var fake = new FakeProcessExecutor(_ =>
            new ProcessResult(1, "", "stop early", TimeSpan.Zero, false));
        var pipeline = new SystemMtPipeline(fake);

        var states = new List<string>();
        // synchronous IProgress<T> impl —— Progress<T> 的回调走 SyncContext 异步派发，
        // 在 xUnit 并行 runner 下断言可能先于回调跑到，导致 race
        var progress = new SyncProgress(s => states.Add(s));
        await pipeline.ExecuteAsync(MakeContext(), progress);

        // 至少 parsing-source 触发了；后续因 fake 失败而中断
        Assert.Contains(PipelineStatus.ParsingSource, states);
    }

    private static string ExtractOutputArg(string cmd)
    {
        var marker = "--output \"";
        var i = cmd.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) throw new InvalidOperationException("No --output arg in " + cmd);
        var start = i + marker.Length;
        var end = cmd.IndexOf('"', start);
        return cmd.Substring(start, end - start);
    }

    private static string ExtractOutputFileArg(string cmd)
    {
        var marker = "--output-file \"";
        var i = cmd.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) throw new InvalidOperationException("No --output-file arg in " + cmd);
        var start = i + marker.Length;
        var end = cmd.IndexOf('"', start);
        return cmd.Substring(start, end - start);
    }

    private static string ArgAfter(IReadOnlyList<string> argv, string flag)
    {
        var index = argv.ToList().IndexOf(flag);
        if (index < 0 || index == argv.Count - 1)
            throw new InvalidOperationException($"No {flag} arg in " + string.Join(" ", argv));
        return argv[index + 1];
    }
}

internal sealed class SyncProgress : IProgress<string>
{
    private readonly Action<string> _action;
    public SyncProgress(Action<string> action) { _action = action; }
    public void Report(string value) => _action(value);
}

internal sealed class FakeProcessExecutor : IProcessExecutor
{
    private readonly Func<string, ProcessResult> _handler;

    public FakeProcessExecutor(Func<string, ProcessResult> handler)
    {
        _handler = handler;
    }

    public Task<ProcessResult> RunAsync(
        ProcessInvocation invocation, string workingDirectory, int timeoutSeconds, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(ToDisplayString(invocation)));
    }

    private static string ToDisplayString(ProcessInvocation invocation) =>
        string.Join(" ", new[] { invocation.FileName }.Concat(invocation.Arguments.Select(FormatArgument)));

    private static string FormatArgument(string argument)
    {
        if (argument.StartsWith("--", StringComparison.Ordinal)
            || argument.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_'))
        {
            return argument;
        }

        return "\"" + argument.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}

internal sealed class DockerWritingClient : IDockerMcpRuntimeClient
{
    private readonly Func<IReadOnlyList<string>, DockerMcpRunResult> _handler;

    public DockerWritingClient(Func<IReadOnlyList<string>, DockerMcpRunResult> handler)
    {
        _handler = handler;
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
        return Task.FromResult(_handler(request.Args));
    }

    public sealed record RunRequest(
        DockerMcpRuntimeOptions Options,
        DockerMcpRunRequest Request);
}
