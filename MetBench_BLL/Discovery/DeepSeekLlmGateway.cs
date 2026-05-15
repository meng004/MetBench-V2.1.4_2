using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MetBench_BLL.Discovery;

/// <summary>
/// Anthropic-compatible gateway backed by the DeepSeek API.
/// Reads credentials from IConfiguration ("DeepSeek:BaseUrl", "DeepSeek:ApiKey", "DeepSeek:Model").
/// </summary>
public sealed class DeepSeekLlmGateway : ILlmGateway
{
    private readonly HttpClient _http;
    private readonly string _model;

    public DeepSeekLlmGateway(string baseUrl, string apiKey, string model)
    {
        _model = model;

        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + '/') };
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var body = new
        {
            model = _model,
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(ct);
        var node = JsonNode.Parse(raw);
        return node?["content"]?[0]?["text"]?.GetValue<string>()
               ?? throw new InvalidOperationException($"Unexpected DeepSeek response: {raw}");
    }
}
