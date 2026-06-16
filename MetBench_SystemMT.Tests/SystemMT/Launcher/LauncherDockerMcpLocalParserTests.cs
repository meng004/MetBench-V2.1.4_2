using System;
using System.Collections.Generic;
using System.Diagnostics;
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
/// runner still uses the profile (container) python.
///
/// Test design: in-process fake MCP client. CI-safe, no docker or loopback listener.
/// The fake client substitutes argv[0] with the real local python for the runner calls,
/// so the runner succeeds even with a bogus container python. The parser commands run
/// locally — before the fix they use the bogus path and fail; after the fix they use
/// <c>localPython</c> and succeed.
/// Routing fact: exactly 2 <c>run_sut_command</c> calls (source + follow-up), each with
/// the bogus container python as argv[0].
/// </summary>
public sealed class LauncherDockerMcpLocalParserTests
{
    [Fact]
    public async Task Parser_commands_run_locally_while_runner_routes_through_mcp()
    {
        var localPython = TestAssetPaths.PythonExecutable();
        var dockerClient = new FakeMcpRuntimeClient(localPython);

        var uri = "docker-mcp://system?image=test-image"
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
        var launcher = new SystemMtLauncher(
            options,
            new SystemMtPipeline(
                dockerMcpProcessExecutor: new DockerMcpProcessExecutor(dockerClient)),
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
            Assert.Equal("/nonexistent/container-python", argv[0]));
    }

    // ---- in-process fake MCP client ----

    /// <summary>
    /// In-process fake for the minimal MCP surface consumed by the launcher:
    /// <list type="bullet">
    ///   <item><c>runtime_health</c> → 200 with "ok" status.</item>
    ///   <item><c>run_sut_command</c> → records argv[0], replaces it with the real
    ///     local python, runs the process locally, and returns the captured output.</item>
    /// </list>
    /// </summary>
    private sealed class FakeMcpRuntimeClient : IDockerMcpRuntimeClient
    {
        private readonly string _realLocalPython;

        public List<IReadOnlyList<string>> RunSutCommandCalls { get; } = new();

        public FakeMcpRuntimeClient(string realLocalPython)
        {
            _realLocalPython = realLocalPython;
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
            IReadOnlyList<string> argv,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            // Record the call with the original argv[0] (the container python path)
            lock (RunSutCommandCalls)
            {
                RunSutCommandCalls.Add(argv.ToArray());
            }

            if (argv.Count == 0)
            {
                return new DockerMcpRunResult(-1, string.Empty, "empty argv", TimedOut: false);
            }

            // Replace argv[0] with the real local python so the process can actually run
            var realArgv = new List<string>(argv) { [0] = _realLocalPython };

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
