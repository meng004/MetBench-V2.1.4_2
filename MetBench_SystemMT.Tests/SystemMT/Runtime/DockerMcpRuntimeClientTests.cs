using System.Net;
using System.Net.Http;
using System.Text;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Runtime;

public sealed class DockerMcpRuntimeClientTests
{
    [Fact]
    public async Task Health_async_posts_runtime_health_tool_with_bearer_token()
    {
        var handler = new CapturingHandler(
            HttpStatusCode.OK,
            """
            {
              "status": "ok",
              "bind_host": "192.168.1.20",
              "bind_port": 8765,
              "repo_root": "/repo"
            }
            """);
        var client = new DockerMcpRuntimeClient(new HttpClient(handler));
        var options = new DockerMcpRuntimeOptions(
            Endpoint: "http://192.168.1.20:8765",
            Image: "metbench-sut:latest",
            PythonExecutable: "/opt/openmoc-venv/bin/python",
            AuthTokenEnvironmentVariable: "METBENCH_DOCKER_MCP_TOKEN");
        Environment.SetEnvironmentVariable("METBENCH_DOCKER_MCP_TOKEN", "secret-token");

        try
        {
            var result = await client.HealthAsync(options);

            Assert.True(result.Available);
            Assert.Equal("ok", result.Status);
            Assert.Equal("192.168.1.20", result.BindHost);
            Assert.Equal(8765, result.BindPort);
            Assert.Equal("/repo", result.RepoRoot);
            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("http://192.168.1.20:8765/tool", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
            Assert.Equal("secret-token", handler.LastRequest.Headers.Authorization.Parameter);
            Assert.Contains("\"tool\":\"runtime_health\"", handler.LastRequestBody);
        }
        finally
        {
            Environment.SetEnvironmentVariable("METBENCH_DOCKER_MCP_TOKEN", null);
        }
    }

    [Fact]
    public async Task Health_async_reports_http_failure_as_unavailable()
    {
        var handler = new CapturingHandler(HttpStatusCode.Unauthorized, """{"error":"Unauthorized"}""");
        var client = new DockerMcpRuntimeClient(new HttpClient(handler));
        var options = new DockerMcpRuntimeOptions(
            Endpoint: "http://127.0.0.1:8765",
            Image: "metbench-sut:latest",
            PythonExecutable: "/opt/openmoc-venv/bin/python");

        var result = await client.HealthAsync(options);

        Assert.False(result.Available);
        Assert.Equal("http_error", result.Status);
        Assert.Contains("401", result.Detail);
        Assert.Contains("Unauthorized", result.Detail);
    }

    [Fact]
    public async Task RunSutCommandAsync_posts_structured_tool_request_without_raw_argv()
    {
        var handler = new CapturingHandler(
            HttpStatusCode.OK,
            """
            {
              "run_id": "run-1",
              "status": "completed",
              "returncode": 0,
              "stdout": "ok",
              "stderr": ""
            }
            """);
        var client = new DockerMcpRuntimeClient(new HttpClient(handler));
        var options = new DockerMcpRuntimeOptions(
            Endpoint: "http://127.0.0.1:8765",
            Image: "metbench-sut:latest",
            PythonExecutable: "/opt/metbench-tools/openmoc-runner",
            ToolName: "openmoc-runner",
            LocalExecutable: "/host/openmoc-runner");

        var result = await client.RunSutCommandAsync(
            options,
            new DockerMcpRunRequest(
                Image: "metbench-sut:latest",
                Tool: "openmoc-runner",
                Args: new[] { "--input", "source.json" },
                WorkingDirectory: string.Empty,
                TimeoutSeconds: 60));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("\"tool\":\"run_sut_command\"", handler.LastRequestBody);
        Assert.Contains("\"arguments\":", handler.LastRequestBody);
        Assert.Contains("\"image\":\"metbench-sut:latest\"", handler.LastRequestBody);
        Assert.Contains("\"tool\":\"openmoc-runner\"", handler.LastRequestBody);
        Assert.Contains("\"args\":[\"--input\",\"source.json\"]", handler.LastRequestBody);
        Assert.Contains("\"timeout_seconds\":60", handler.LastRequestBody);
        Assert.DoesNotContain("argv", handler.LastRequestBody);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public CapturingHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }
    }
}
