using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MetBench_BLL.SystemMT.Runtime;

public interface IDockerMcpRuntimeClient
{
    Task<DockerMcpHealthResult> HealthAsync(
        DockerMcpRuntimeOptions options,
        CancellationToken cancellationToken = default);

    Task<DockerMcpRunResult> RunSutCommandAsync(
        DockerMcpRuntimeOptions options,
        DockerMcpRunRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record DockerMcpHealthResult(
    bool Available,
    string Status,
    string Detail,
    string BindHost = "",
    int? BindPort = null,
    string RepoRoot = "");

public sealed record DockerMcpRunResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    bool TimedOut,
    string RunId = "");

public sealed class DockerMcpRuntimeClient : IDockerMcpRuntimeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public DockerMcpRuntimeClient()
        : this(new HttpClient())
    {
    }

    internal DockerMcpRuntimeClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<DockerMcpHealthResult> HealthAsync(
        DockerMcpRuntimeOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var token = ResolveAuthToken(options);
        if (options.AuthTokenEnvironmentVariable is not null && string.IsNullOrWhiteSpace(token))
        {
            return new DockerMcpHealthResult(
                false,
                "auth_token_missing",
                $"Docker MCP auth token environment variable '{options.AuthTokenEnvironmentVariable}' is missing.");
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ToolUri(options.Endpoint));
            if (!string.IsNullOrWhiteSpace(token))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(new { tool = "runtime_health", arguments = new { } }, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new DockerMcpHealthResult(
                    false,
                    "http_error",
                    $"Docker MCP health request failed with status {(int)response.StatusCode}. {Trim(body)}");
            }

            return ParseHealth(body);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DockerMcpHealthResult(false, "request_error", ex.Message);
        }
    }

    public async Task<DockerMcpRunResult> RunSutCommandAsync(
        DockerMcpRuntimeOptions options,
        DockerMcpRunRequest request,
        CancellationToken cancellationToken = default)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Image))
            throw new ArgumentException("Docker MCP run requires an image.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Tool))
            throw new ArgumentException("Docker MCP run requires a tool.", nameof(request));
        if (request.Args is null)
            throw new ArgumentException("Docker MCP run requires args.", nameof(request));
        if (request.TimeoutSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Timeout must be positive.");
        if (!string.Equals(request.Image, options.Image, StringComparison.Ordinal))
            throw new ArgumentException("Docker MCP run request image must match configured image.", nameof(request));
        if (!string.Equals(request.Tool, options.ToolName, StringComparison.Ordinal))
            throw new ArgumentException("Docker MCP run request tool must match configured tool.", nameof(request));

        var token = ResolveAuthToken(options);
        if (options.AuthTokenEnvironmentVariable is not null && string.IsNullOrWhiteSpace(token))
        {
            return new DockerMcpRunResult(
                -1,
                "",
                $"Docker MCP auth token environment variable '{options.AuthTokenEnvironmentVariable}' is missing.",
                TimedOut: false);
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ToolUri(options.Endpoint));
            if (!string.IsNullOrWhiteSpace(token))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    tool = "run_sut_command",
                    arguments = new
                    {
                        image = request.Image,
                        tool = request.Tool,
                        args = request.Args,
                        timeout_seconds = request.TimeoutSeconds,
                    },
                }, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new DockerMcpRunResult(
                    -1,
                    "",
                    $"Docker MCP run_sut_command failed with status {(int)response.StatusCode}. {Trim(body)}",
                    TimedOut: false);
            }

            return ParseRunResult(body);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DockerMcpRunResult(-1, "", ex.Message, TimedOut: false);
        }
    }

    private static Uri ToolUri(string endpoint)
    {
        var trimmed = endpoint.TrimEnd('/');
        return new Uri($"{trimmed}/tool", UriKind.Absolute);
    }

    private static string? ResolveAuthToken(DockerMcpRuntimeOptions options) =>
        string.IsNullOrWhiteSpace(options.AuthTokenEnvironmentVariable)
            ? null
            : Environment.GetEnvironmentVariable(options.AuthTokenEnvironmentVariable);

    private static DockerMcpHealthResult ParseHealth(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var status = GetString(root, "status");
            var bindHost = GetString(root, "bind_host");
            var bindPort = GetInt(root, "bind_port");
            var repoRoot = GetString(root, "repo_root");
            var detail = $"Docker MCP runtime_health returned status '{status}' at {bindHost}:{bindPort}.";

            return string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase)
                ? new DockerMcpHealthResult(true, status, detail, bindHost, bindPort, repoRoot)
                : new DockerMcpHealthResult(false, status, detail, bindHost, bindPort, repoRoot);
        }
        catch (JsonException ex)
        {
            return new DockerMcpHealthResult(false, "malformed_response", ex.Message);
        }
    }

    private static DockerMcpRunResult ParseRunResult(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var returnCode = root.TryGetProperty("returncode", out var returnCodeValue)
                && returnCodeValue.TryGetInt32(out var parsedReturnCode)
                    ? parsedReturnCode
                    : -1;
            var status = GetString(root, "status");
            var stderr = GetString(root, "stderr");
            var timedOut = string.Equals(status, "timeout", StringComparison.OrdinalIgnoreCase)
                || stderr.Contains("timed out", StringComparison.OrdinalIgnoreCase);
            return new DockerMcpRunResult(
                returnCode,
                GetString(root, "stdout"),
                stderr,
                timedOut,
                GetString(root, "run_id"));
        }
        catch (JsonException ex)
        {
            return new DockerMcpRunResult(-1, "", ex.Message, TimedOut: false);
        }
    }

    private static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static int? GetInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static string Trim(string value)
    {
        var text = value.Trim();
        return text.Length <= 500 ? text : text[..500];
    }
}
