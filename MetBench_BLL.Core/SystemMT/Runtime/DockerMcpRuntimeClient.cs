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
        IReadOnlyList<string> argv,
        int timeoutSeconds,
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
    bool TimedOut);

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
            using var request = new HttpRequestMessage(HttpMethod.Post, ToolUri(options.Endpoint));
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { tool = "runtime_health", arguments = new { } }, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
        IReadOnlyList<string> argv,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        if (argv is null || argv.Count == 0)
            throw new ArgumentException("Docker MCP run requires a non-empty argv.", nameof(argv));
        if (timeoutSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Timeout must be positive.");

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
            using var request = new HttpRequestMessage(HttpMethod.Post, ToolUri(options.Endpoint));
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    tool = "run_sut_command",
                    arguments = new
                    {
                        image = options.Image,
                        argv,
                        timeout_seconds = timeoutSeconds,
                    },
                }, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
                timedOut);
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
