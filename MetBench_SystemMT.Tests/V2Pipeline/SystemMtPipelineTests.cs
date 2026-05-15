using System.Text.Json;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
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
            InputParserCommand: "fake-input-parser",
            OutputParserCommand: "fake-output-parser",
            RunnerCommand: "fake-runner",
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
        string command, string workingDirectory, int timeoutSeconds, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(command));
    }
}
