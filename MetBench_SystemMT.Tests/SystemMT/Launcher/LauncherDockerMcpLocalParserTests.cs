using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
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
/// Test design: in-process loopback fake MCP server (HttpListener). CI-safe, no docker.
/// The fake server substitutes argv[0] with the real local python for the runner calls,
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

        using var server = new FakeMcpServer(localPython);
        var uri = "docker-mcp://system?image=test-image"
            + "&python=/nonexistent/container-python"
            + $"&endpoint={Uri.EscapeDataString(server.Endpoint)}"
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
            new SystemMtPipeline(),
            recorder,
            anomaly,
            new ManifestMrCatalogProvider(options));

        var result = await launcher.RunAsync("p3-trajectory-sensitivity");

        Assert.True(result.Passed, "FailureReason: " + result.FailureReason);
        Assert.Equal(2, server.RunSutCommandCalls.Count);
        Assert.All(server.RunSutCommandCalls, argv =>
            Assert.Equal("/nonexistent/container-python", argv[0]));
    }

    // ---- in-process loopback fake MCP server ----

    /// <summary>
    /// An in-process HttpListener that mimics the minimal MCP server surface
    /// consumed by <see cref="MetBench_BLL.SystemMT.Runtime.DockerMcpRuntimeClient"/>:
    /// <list type="bullet">
    ///   <item><c>runtime_health</c> → 200 with "ok" status.</item>
    ///   <item><c>run_sut_command</c> → records argv[0], replaces it with the real
    ///     local python, runs the process locally, and returns the captured output.</item>
    /// </list>
    /// </summary>
    private sealed class FakeMcpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _realLocalPython;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _listenerTask;
        private readonly int _port;

        public List<IReadOnlyList<string>> RunSutCommandCalls { get; } = new();

        public string Endpoint => $"http://127.0.0.1:{_port}";

        public FakeMcpServer(string realLocalPython)
        {
            _realLocalPython = realLocalPython;
            _port = FindFreePort();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            _listener.Start();
            _listenerTask = Task.Run(() => ServeAsync(_cts.Token));
        }

        private static int FindFreePort()
        {
            using var tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        private async Task ServeAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext? ctx;
                try
                {
                    ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                _ = Task.Run(() => HandleRequestAsync(ctx), ct);
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext ctx)
        {
            using var response = ctx.Response;
            try
            {
                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                var body = await reader.ReadToEndAsync().ConfigureAwait(false);

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var tool = root.TryGetProperty("tool", out var toolProp)
                    ? toolProp.GetString() ?? string.Empty
                    : string.Empty;

                string responseJson;
                if (string.Equals(tool, "runtime_health", StringComparison.OrdinalIgnoreCase))
                {
                    // DockerMcpRuntimeClient.ParseHealth expects: status, bind_host, bind_port, repo_root
                    responseJson = JsonSerializer.Serialize(new
                    {
                        status = "ok",
                        bind_host = "127.0.0.1",
                        bind_port = _port,
                        repo_root = "/",
                    });
                }
                else if (string.Equals(tool, "run_sut_command", StringComparison.OrdinalIgnoreCase))
                {
                    responseJson = await HandleRunSutCommandAsync(root).ConfigureAwait(false);
                }
                else
                {
                    response.StatusCode = 400;
                    responseJson = JsonSerializer.Serialize(new { error = $"unknown tool: {tool}" });
                }

                var bytes = Encoding.UTF8.GetBytes(responseJson);
                response.ContentType = "application/json";
                response.ContentLength64 = bytes.Length;
                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    response.StatusCode = 500;
                    var errBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = ex.Message }));
                    response.ContentLength64 = errBytes.Length;
                    await response.OutputStream.WriteAsync(errBytes).ConfigureAwait(false);
                }
                catch
                {
                    // best effort
                }
            }
        }

        private async Task<string> HandleRunSutCommandAsync(JsonElement root)
        {
            // DockerMcpRuntimeClient sends: { tool, arguments: { image, argv, timeout_seconds } }
            var args = root.TryGetProperty("arguments", out var argsProp) ? argsProp : default;
            var argv = new List<string>();
            if (args.ValueKind == JsonValueKind.Object
                && args.TryGetProperty("argv", out var argvProp)
                && argvProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in argvProp.EnumerateArray())
                {
                    argv.Add(item.GetString() ?? string.Empty);
                }
            }

            // Record the call with the original argv[0] (the container python path)
            lock (RunSutCommandCalls)
            {
                RunSutCommandCalls.Add(argv.AsReadOnly());
            }

            if (argv.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    run_id = "t",
                    status = "completed",
                    returncode = -1,
                    stdout = string.Empty,
                    stderr = "empty argv",
                });
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
                    return JsonSerializer.Serialize(new
                    {
                        run_id = "t",
                        status = "timeout",
                        returncode = -1,
                        stdout = string.Empty,
                        stderr = "timed out",
                    });
                }

                stdoutSb.Append(await stdoutTask.ConfigureAwait(false));
                stderrSb.Append(await stderrTask.ConfigureAwait(false));
                exitCode = proc.ExitCode;
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    run_id = "t",
                    status = "error",
                    returncode = -1,
                    stdout = string.Empty,
                    stderr = ex.Message,
                });
            }

            return JsonSerializer.Serialize(new
            {
                run_id = "t",
                status = "completed",
                returncode = exitCode,
                stdout = stdoutSb.ToString(),
                stderr = stderrSb.ToString(),
            });
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener.Stop(); } catch { }
            try { _listenerTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
            _cts.Dispose();
        }
    }
}
