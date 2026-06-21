using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Runtime;
using MetBench_SystemMT.Tests.SystemMT;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

/// <summary>
/// G1 fact test: parser/output-parser commands run locally via DefaultProcessExecutor
/// but were built on the container python path. After the fix, an explicit
/// <c>localPython</c> URI param overrides the parser executable while the
    /// runner still routes through the structured Docker MCP tool.
///
/// Test design: in-process fake MCP client. CI-safe, no docker or loopback listener.
    /// The fake client maps the structured tool call back to the real local Python + runner
    /// script, so the runner succeeds even with bogus container paths. The parser commands run
    /// locally — before the fix they use the bogus path and fail; after the fix they use
/// <c>localPython</c> and succeed.
    /// Routing fact: exactly 2 <c>run_sut_command</c> calls (source + follow-up), each using
    /// the allowlisted structured MCP tool/local executable instead of a Python script argv.
/// </summary>
public sealed class LauncherDockerMcpLocalParserTests
{
    [Fact]
    public async Task Parser_commands_run_locally_while_runner_routes_through_mcp()
    {
        var localPython = TestAssetPaths.PythonExecutable();
        var runnerScriptPath = Path.Combine(
            TestAssetPaths.AssetRoot(),
            "minimum_mr_subset_p3",
            "minimum_mr_subset_p3.py");
        var dockerClient = new FakeMcpRuntimeClient(localPython, runnerScriptPath);

        var uri = "docker-mcp://system?image=test-image"
            + "&tool=test-runner"
            + "&local=test-runner"
            + "&python=/nonexistent/container-python"
            + $"&endpoint={Uri.EscapeDataString("http://127.0.0.1:1")}"
            + $"&localPython={Uri.EscapeDataString(localPython)}";

        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: localPython,
            OpenMocPython: localPython,
            RuntimePythons: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["system"] = uri,
            });

        var execs = new FakeExecRepo();
        var results = new FakeResultRepo();
        var recorder = new SystemMtExecutionRecorder(execs, results);
        var anomaly = new RecordingAnomalyService();
        var runtimeExecutor = new RuntimeProcessExecutorRegistry(
            new LocalRuntimeProcessExecutor(),
            new DockerRuntimeProcessExecutor(new DockerMcpProcessExecutor(dockerClient)));
        var launcher = new SystemMtLauncher(
            options,
            new SystemMtPipeline(runtimeProcessExecutor: runtimeExecutor),
            recorder,
            anomaly,
            new ManifestMrCatalogProvider(options),
            severityThresholds: null,
            runtimeProfileProvider: null,
            runtimePreflightService: new RuntimePreflightService(
                new DefaultProcessExecutor(),
                dockerClient));

        var result = await launcher.RunAsync("p3-trajectory-sensitivity");

        Assert.True(result.Passed, "FailureReason: " + result.FailureReason);
        Assert.Equal(2, dockerClient.RunSutCommandCalls.Count);
        Assert.All(dockerClient.RunSutCommandCalls, argv =>
        {
            Assert.Equal("test-runner", argv[0]);
            Assert.DoesNotContain(argv, arg => arg.EndsWith(".py", StringComparison.OrdinalIgnoreCase));
        });
    }

    // ---- in-process fake MCP client ----

    /// <summary>
    /// In-process fake for the minimal MCP surface consumed by the launcher:
    /// <list type="bullet">
    ///   <item><c>runtime_health</c> → 200 with "ok" status.</item>
    ///   <item><c>run_sut_command</c> → records the structured tool invocation,
    ///     maps it to the real local runner script, and returns the captured output.</item>
    /// </list>
    /// </summary>
    private sealed class FakeMcpRuntimeClient : IDockerMcpRuntimeClient
    {
        private readonly string _realLocalPython;
        private readonly string _runnerScriptPath;

        public List<IReadOnlyList<string>> RunSutCommandCalls { get; } = new();

        public FakeMcpRuntimeClient(string realLocalPython, string runnerScriptPath)
        {
            _realLocalPython = realLocalPython;
            _runnerScriptPath = runnerScriptPath;
        }

        public Task<DockerMcpHealthResult> HealthAsync(
            DockerMcpRuntimeOptions options,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DockerMcpHealthResult(
                Available: true,
                Status: "ok",
                Detail: "fake docker mcp ok",
                BindHost: "127.0.0.1",
                BindPort: 1,
                RepoRoot: "/"));

        public async Task<DockerMcpRunResult> RunSutCommandAsync(
            DockerMcpRuntimeOptions options,
            DockerMcpRunRequest request,
            CancellationToken cancellationToken = default)
        {
            var argv = new[] { options.LocalExecutable }.Concat(request.Args).ToArray();
            lock (RunSutCommandCalls)
            {
                RunSutCommandCalls.Add(argv.ToArray());
            }

            if (argv.Length == 0)
            {
                return new DockerMcpRunResult(-1, string.Empty, "empty argv", TimedOut: false);
            }

            var realArgv = new List<string> { _realLocalPython, _runnerScriptPath };
            realArgv.AddRange(request.Args);

            // Run locally to simulate what the container would do
            var psi = new ProcessStartInfo
            {
                FileName = realArgv[0],
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            for (var i = 1; i < realArgv.Count; i++)
            {
                psi.ArgumentList.Add(realArgv[i]);
            }

            var stdoutSb = new StringBuilder();
            var stderrSb = new StringBuilder();
            int exitCode;
            try
            {
                using var proc = new Process { StartInfo = psi };
                proc.Start();
                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();
                // Fixed 60 s ceiling so the fake server cannot hang CI indefinitely.
                var delay = Task.Delay(TimeSpan.FromSeconds(60));
                var finished = await Task.WhenAny(
                    Task.WhenAll(stdoutTask, stderrTask).ContinueWith(_ => proc.WaitForExit()),
                    delay).ConfigureAwait(false);

                if (finished == delay)
                {
                    try { proc.Kill(); } catch { }
                    return new DockerMcpRunResult(-1, string.Empty, "timed out", TimedOut: true);
                }

                stdoutSb.Append(await stdoutTask.ConfigureAwait(false));
                stderrSb.Append(await stderrTask.ConfigureAwait(false));
                exitCode = proc.ExitCode;
            }
            catch (Exception ex)
            {
                return new DockerMcpRunResult(-1, string.Empty, ex.Message, TimedOut: false);
            }

            return new DockerMcpRunResult(
                exitCode,
                stdoutSb.ToString(),
                stderrSb.ToString(),
                TimedOut: false);
        }
    }
}
