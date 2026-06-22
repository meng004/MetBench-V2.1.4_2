using MetBench_BLL.SystemMT.Jobs;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Acceptance;

public sealed class SystemMtApiBusinessRuntimeMcpEndToEndTests
{
    private const string BusinessMcpUrlEnv = "METBENCH_E2E_BUSINESS_MCP_URL";
    private const string BusinessMcpTokenEnv = "METBENCH_E2E_BUSINESS_MCP_TOKEN";
    private const string ApiUrlEnv = "METBENCH_E2E_API_URL";
    private const string MrIdEnv = "METBENCH_E2E_MR_ID";
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private const string SkipReason =
        "API + Business MCP + Runtime MCP E2E env is not configured. Set "
        + BusinessMcpUrlEnv + ", "
        + BusinessMcpTokenEnv + ", "
        + ApiUrlEnv + " and "
        + MrIdEnv + ".";

    private static string? Env(string name) => Environment.GetEnvironmentVariable(name);

    private static bool Configured =>
        !string.IsNullOrWhiteSpace(Env(BusinessMcpUrlEnv))
        && !string.IsNullOrWhiteSpace(Env(BusinessMcpTokenEnv))
        && !string.IsNullOrWhiteSpace(Env(ApiUrlEnv))
        && !string.IsNullOrWhiteSpace(Env(MrIdEnv));

    [SkippableFact]
    public async Task Business_mcp_submit_run_reaches_api_result_and_runtime_evidence()
    {
        Skip.IfNot(Configured, SkipReason);

        var businessUrl = Env(BusinessMcpUrlEnv)!;
        var businessToken = Env(BusinessMcpTokenEnv)!;
        var apiUrl = Env(ApiUrlEnv)!;
        var mrId = Env(MrIdEnv)!;

        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        using var submitTimeout = new CancellationTokenSource(RequestTimeout);
        var jobId = await SubmitRunAsync(http, businessUrl, businessToken, mrId, submitTimeout.Token);

        var succeeded = false;
        try
        {
            using var pollTimeout = new CancellationTokenSource(PollTimeout);
            var finalJob = await PollJobUntilTerminalAsync(http, apiUrl, jobId, pollTimeout.Token);

            Assert.True(
                finalJob.State == SystemMtJobState.Succeeded,
                "Final job payload: " + finalJob.Body);

            using var resultTimeout = new CancellationTokenSource(RequestTimeout);
            var result = await GetAsync(
                http,
                BuildUri(apiUrl, $"api/v1/systemmt/jobs/{jobId}/result"),
                resultTimeout.Token);
            using (var resultJson = ParseJsonPayload(result))
            {
                Assert.True(ReadRequiredBool(resultJson.RootElement, "passed", result), "Result payload: " + result);
            }

            using var evidenceTimeout = new CancellationTokenSource(RequestTimeout);
            var evidence = await GetAsync(
                http,
                BuildUri(apiUrl, $"api/v1/systemmt/jobs/{jobId}/evidence"),
                evidenceTimeout.Token);
            using var evidenceJson = ParseJsonPayload(evidence);
            var sourceRunId = ReadRequiredString(evidenceJson.RootElement, "sourceRunId", evidence);
            var followupRunId = ReadRequiredString(evidenceJson.RootElement, "followupRunId", evidence);

            Assert.False(string.IsNullOrWhiteSpace(sourceRunId), "Evidence payload: " + evidence);
            Assert.False(string.IsNullOrWhiteSpace(followupRunId), "Evidence payload: " + evidence);
            succeeded = true;
        }
        catch (Exception ex) when (!succeeded)
        {
            var cancelFailure = await TryCancelJobAsync(http, apiUrl, jobId);
            if (cancelFailure is not null)
            {
                ex.Data["BestEffortCancelFailure"] =
                    $"Best-effort cancel for job '{jobId}' failed: {cancelFailure}";
            }

            throw;
        }
    }

    private static async Task<string> SubmitRunAsync(
        HttpClient http,
        string businessUrl,
        string businessToken,
        string mrId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(businessUrl, "tool"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", businessToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                tool = "submit_run",
                arguments = new
                {
                    mr_id = mrId,
                },
            }),
            Encoding.UTF8,
            "application/json");

        var body = await SendAsync(http, request, cancellationToken);
        using var document = ParseJsonPayload(body);
        return ReadRequiredString(document.RootElement, "jobId", body);
    }

    private static async Task<(SystemMtJobState State, string Body)> PollJobUntilTerminalAsync(
        HttpClient http,
        string apiUrl,
        string jobId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.Add(PollTimeout);
        var lastBody = string.Empty;
        SystemMtJobState? lastState = null;

        while (DateTime.UtcNow < deadline)
        {
            lastBody = await GetAsync(http, BuildUri(apiUrl, $"api/v1/systemmt/jobs/{jobId}"), cancellationToken);
            using var document = ParseJsonPayload(lastBody);
            lastState = ReadState(document.RootElement, lastBody);

            if (lastState.Value.IsTerminal())
                return (lastState.Value, lastBody);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException(
            $"Job {jobId} did not reach a terminal state within {PollTimeout.TotalSeconds:0} seconds. "
            + $"Last state='{lastState}', payload='{lastBody}'.");
    }

    private static async Task<string> GetAsync(HttpClient http, Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        return await SendAsync(http, request, cancellationToken);
    }

    private static async Task<string?> TryCancelJobAsync(HttpClient http, string apiUrl, string jobId)
    {
        try
        {
            using var timeout = new CancellationTokenSource(RequestTimeout);
            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                BuildUri(apiUrl, $"api/v1/systemmt/jobs/{jobId}"));
            using var response = await http.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            if (response.IsSuccessStatusCode)
                return null;

            return $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {body}";
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static async Task<string> SendAsync(
        HttpClient http,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.True(
            response.IsSuccessStatusCode,
            $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        return body;
    }

    private static JsonDocument ParseJsonPayload(string payload)
    {
        try
        {
            return JsonDocument.Parse(payload);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Expected response body to be valid JSON. Payload: " + payload, ex);
        }
    }

    private static Uri BuildUri(string baseUrl, string relativePath) =>
        new($"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}", UriKind.Absolute);

    private static SystemMtJobState ReadState(JsonElement root, string payload)
    {
        var value = RequiredProperty(root, "state", payload);
        if (value.ValueKind == JsonValueKind.String)
        {
            var state = value.GetString();
            if (Enum.TryParse<SystemMtJobState>(state, ignoreCase: true, out var parsed))
                return parsed;

            throw new InvalidOperationException(
                $"Expected job state to match {nameof(SystemMtJobState)} but got '{state}'. Payload: {payload}");
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            if (Enum.IsDefined(typeof(SystemMtJobState), number))
                return (SystemMtJobState)number;

            throw new InvalidOperationException(
                $"Expected numeric job state to match {nameof(SystemMtJobState)} but got '{number}'. Payload: {payload}");
        }

        throw new InvalidOperationException(
            $"Expected job state to be a string or integer. Payload: {payload}");
    }

    private static string ReadRequiredString(JsonElement root, string propertyName, string payload)
    {
        var value = RequiredProperty(root, propertyName, payload);
        if (value.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"Expected '{propertyName}' to be a string. Payload: {payload}");
        return value.GetString() ?? string.Empty;
    }

    private static bool ReadRequiredBool(JsonElement root, string propertyName, string payload)
    {
        var value = RequiredProperty(root, propertyName, payload);
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            throw new InvalidOperationException($"Expected '{propertyName}' to be a boolean. Payload: {payload}");
        return value.GetBoolean();
    }

    private static JsonElement RequiredProperty(JsonElement root, string propertyName, string payload)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        throw new InvalidOperationException($"Missing required JSON property '{propertyName}'. Payload: {payload}");
    }
}
