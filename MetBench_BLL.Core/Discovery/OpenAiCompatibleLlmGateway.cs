using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MetBench_BLL.Discovery;

/// <summary>
/// W11.2 — OpenAI-compatible LLM gateway。
/// 适配端点 <c>POST {baseUrl}/v1/chat/completions</c>，Bearer 认证。
/// </summary>
/// <remarks>
/// 一个 gateway 类满足 3 家 provider：
/// <list type="bullet">
///   <item><b>OpenAI 原生</b>：<c>https://api.openai.com</c></item>
///   <item><b>DeepSeek 原生</b>：<c>https://api.deepseek.com</c></item>
///   <item><b>bltcy.ai 多模型网关</b>：<c>https://api.bltcy.ai</c>（同一 key 跑 gpt / claude / 其他）</item>
/// </list>
///
/// 与既有 <see cref="DeepSeekLlmGateway"/>（仅 BLL/，Anthropic v1/messages 协议）的区别：
/// 此 gateway 走 OpenAI 业界标准 chat/completions，是 W11.2 multi-LLM 实验
/// 三家 provider 的统一接入点。
///
/// 设计：注入 HttpClient (可测) + apiKey + model。CompleteAsync 包成 prompt → 取 first choice.message.content。
/// </remarks>
public sealed class OpenAiCompatibleLlmGateway : ILlmGateway
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly bool _ownsClient;

    /// <summary>生产构造：从 baseUrl + apiKey 自建 HttpClient。</summary>
    public OpenAiCompatibleLlmGateway(string baseUrl, string apiKey, string model)
        : this(BuildHttp(baseUrl, apiKey), model, ownsClient: true) { }

    /// <summary>测试构造：注入预制 HttpClient（含 mock handler），便于单测。</summary>
    internal OpenAiCompatibleLlmGateway(HttpClient http, string model, bool ownsClient = false)
    {
        _http = http;
        _model = model;
        _ownsClient = ownsClient;
    }

    private static HttpClient BuildHttp(string baseUrl, string apiKey)
    {
        var c = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + '/') };
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        c.Timeout = TimeSpan.FromSeconds(120);
        return c;
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
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"LLM API {(int)response.StatusCode} {response.StatusCode}: {Truncate(raw, 500)}");

        var node = JsonNode.Parse(raw)
            ?? throw new InvalidOperationException($"LLM API returned non-JSON: {Truncate(raw, 200)}");

        // OpenAI / DeepSeek / bltcy.ai 都用 choices[0].message.content
        var content = node["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
        if (content is not null) return content;

        // 部分 anthropic-compat gateway 用 content[0].text
        var anthropicText = node["content"]?[0]?["text"]?.GetValue<string>();
        if (anthropicText is not null) return anthropicText;

        throw new InvalidOperationException(
            $"Cannot extract text from LLM response. Raw (first 300 chars): {Truncate(raw, 300)}");
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max) + "...";
}
