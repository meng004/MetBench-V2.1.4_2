using System.Text.Json;

namespace MetBench_BLL.Discovery;

/// <summary>
/// 借助 LLM 自由提议 MR 候选 —— 把 SUT 描述 + 既有 MR 列表当 prompt，
/// 让模型反向问 "还有哪些可能的 invariant / monotonicity / convergence"。
/// </summary>
/// <remarks>
/// Prompt 期望 LLM 返回严格 JSON：
/// <code>
/// {"candidates": [
///   {"code":"MR-T-auto-001","name":"...","assertion":"approx","value":"k_eff",
///    "rationale":"...","confidence":0.7}, ...
/// ]}
/// </code>
/// 解析失败时回退到 status="error"，不抛异常 —— 让 <see cref="DiscoveryService"/>
/// 仍能把这次 run 落库便于审计。
/// </remarks>
public sealed class LlmNativeDiscoverer : IMRDiscoverer
{
    public string Name => "llm-native";

    private readonly ILlmGateway _gateway;
    private readonly string _systemPrompt;

    public LlmNativeDiscoverer(ILlmGateway gateway, string? systemPrompt = null)
    {
        _gateway = gateway;
        _systemPrompt = systemPrompt ?? DefaultPrompt;
    }

    public async Task<DiscoveryRunOutcome> DiscoverAsync(int? targetApplicationId, CancellationToken ct = default)
    {
        var prompt = _systemPrompt + $"\n\nTarget application id: {targetApplicationId?.ToString() ?? "(any)"}.";
        string reply;
        try
        {
            reply = await _gateway.CompleteAsync(prompt, ct);
        }
        catch (Exception ex)
        {
            return new DiscoveryRunOutcome("error", $"prompt-sha:{prompt.GetHashCode():X}",
                Array.Empty<CandidateMrProposal>(), ex.Message);
        }

        if (string.IsNullOrWhiteSpace(reply))
            return new DiscoveryRunOutcome("ok", $"prompt-sha:{prompt.GetHashCode():X}",
                Array.Empty<CandidateMrProposal>());

        try
        {
            var proposals = ParseLlmReply(reply);
            return new DiscoveryRunOutcome("ok", $"prompt-sha:{prompt.GetHashCode():X}", proposals);
        }
        catch (JsonException ex)
        {
            return new DiscoveryRunOutcome("error", $"prompt-sha:{prompt.GetHashCode():X}",
                Array.Empty<CandidateMrProposal>(), $"LLM JSON parse: {ex.Message}");
        }
    }

    internal static IReadOnlyList<CandidateMrProposal> ParseLlmReply(string reply)
    {
        var list = new List<CandidateMrProposal>();
        using var doc = JsonDocument.Parse(reply);
        if (!doc.RootElement.TryGetProperty("candidates", out var arr))
            return list;
        foreach (var c in arr.EnumerateArray())
        {
            var code = c.GetProperty("code").GetString() ?? "(missing)";
            var name = c.TryGetProperty("name", out var n) ? (n.GetString() ?? "") : "";
            var assertion = c.TryGetProperty("assertion", out var a) ? a.GetString() : null;
            var value = c.TryGetProperty("value", out var v) ? (v.GetString() ?? "k_eff") : "k_eff";
            var rationale = c.TryGetProperty("rationale", out var r) ? (r.GetString() ?? "") : "";
            var confidence = c.TryGetProperty("confidence", out var conf) && conf.ValueKind == JsonValueKind.Number
                ? conf.GetDouble() : 0.5;

            list.Add(new CandidateMrProposal(
                ProposedCode: code,
                ProposedName: name,
                SuggestedTransformationName: null,
                SuggestedAssertionTypeCode: assertion,
                ProposedValueName: value,
                Rationale: rationale,
                Confidence: confidence));
        }
        return list;
    }

    private const string DefaultPrompt = @"You are an expert in metamorphic testing for nuclear-reactor neutronics solvers.
List up to 5 plausible new metamorphic relations on k_eff that the user hasn't yet covered.
Reply ONLY in JSON of shape:
{""candidates"": [{""code"":""...""," + @"""name"":""..."",""assertion"":""approx|less|greater|bound"",""value"":""k_eff"",""rationale"":""..."",""confidence"":0.0}]}";
}
