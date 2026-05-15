using System.Text.Json;
using MetBench_Domain;

namespace MetBench_BLL.Discovery.Validators;

/// <summary>
/// TheoreticalLlmValidator —— 拿候选 MR 的 rationale 问 LLM "物理/数学上合理吗"。
/// </summary>
/// <remarks>
/// 期望 LLM 返回严格 JSON：<c>{"plausible": true|false, "reason": "..."}</c>。
/// 解析失败默认 Passed=false 并把 raw 回复塞进 DetailsJson。
/// </remarks>
public sealed class TheoreticalLlmValidator : IMRValidator
{
    public string Name => "theoretical-llm";

    private readonly ILlmGateway _gateway;

    public TheoreticalLlmValidator(ILlmGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<ValidationOutcome> ValidateAsync(CandidateMR candidate, CancellationToken ct = default)
    {
        var prompt = $@"Given the metamorphic relation candidate:
code: {candidate.ProposedCode}
name: {candidate.ProposedName}
value-under-test: {candidate.ProposedValueName}
assertion-type: {candidate.SuggestedAssertionTypeCode}
rationale: {candidate.Rationale}

Is this relation physically/mathematically plausible for a 2D pin-cell neutronics solver?
Reply ONLY JSON: {{""plausible"": true|false, ""reason"": ""<one sentence>""}}";

        string reply;
        try
        {
            reply = await _gateway.CompleteAsync(prompt, ct);
        }
        catch (Exception ex)
        {
            return new ValidationOutcome(false, $"{{\"reason\":\"gateway-error\",\"error\":\"{Escape(ex.Message)}\"}}");
        }

        if (string.IsNullOrWhiteSpace(reply))
            return new ValidationOutcome(false, "{\"reason\":\"empty-reply\"}");

        try
        {
            using var doc = JsonDocument.Parse(reply);
            var plausible = doc.RootElement.TryGetProperty("plausible", out var p)
                            && p.ValueKind == JsonValueKind.True;
            var reason = doc.RootElement.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
            return new ValidationOutcome(plausible,
                $"{{\"plausible\":{plausible.ToString().ToLowerInvariant()},\"reason\":\"{Escape(reason)}\"}}");
        }
        catch (JsonException)
        {
            return new ValidationOutcome(false, $"{{\"reason\":\"unparseable\",\"raw\":\"{Escape(reply)}\"}}");
        }
    }

    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
