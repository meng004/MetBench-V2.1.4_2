namespace MetBench_BLL.Discovery;

/// <summary>
/// F12 — Multi-LLM provider 互验：把多家 LLM 的 plausibility 判断收集起来，算 consensus + Cohen's κ。
/// </summary>
/// <remarks>
/// 用途：把候选 MR 的 rationale 同时发给 DeepSeek / Anthropic / OpenAI（或本地模型），
/// 看 ≥2 家是否一致认为"物理合理"。任一家单独 say-true 不足以 promote；多家一致才是强信号。
///
/// 设计：
/// <list type="bullet">
///   <item>构造期注入 N 个命名的 ILlmGateway，运行时并发 fan-out</item>
///   <item>响应解析失败 / gateway 异常 → 该 provider 贡献 null（不参与 consensus）</item>
///   <item>consensus = majority vote (严格 &gt; 50%) over non-null responses</item>
///   <item>Cohen's κ = pair-wise inter-rater agreement (n≥2)</item>
/// </list>
/// </remarks>
public sealed class MultiLlmConsensusValidator
{
    private readonly IReadOnlyList<NamedGateway> _gateways;

    public MultiLlmConsensusValidator(IEnumerable<NamedGateway> gateways)
    {
        _gateways = gateways.ToList();
        if (_gateways.Count == 0)
            throw new ArgumentException("At least one LLM gateway required.", nameof(gateways));
    }

    /// <summary>
    /// 把同一 prompt 并发发给所有 gateway，收集 + 算 consensus。
    /// </summary>
    public async Task<LlmConsensusReport> AssessAsync(string prompt, CancellationToken ct = default)
    {
        var tasks = _gateways.Select(async ng =>
        {
            try
            {
                var raw = await ng.Gateway.CompleteAsync(prompt, ct).ConfigureAwait(false);
                var plausible = TryParsePlausible(raw);
                return new LlmProviderResponse(ng.Name, raw, plausible, ErrorMessage: null);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new LlmProviderResponse(ng.Name, RawText: "", Plausible: null, ErrorMessage: ex.Message);
            }
        });

        var responses = await Task.WhenAll(tasks).ConfigureAwait(false);

        var votes = responses.Where(r => r.Plausible.HasValue).ToList();
        bool? consensus = null;
        if (votes.Count > 0)
        {
            var trueVotes = votes.Count(r => r.Plausible == true);
            // strict majority: > 50% (ties → null = inconclusive)
            if (trueVotes * 2 > votes.Count) consensus = true;
            else if (trueVotes * 2 < votes.Count) consensus = false;
            else consensus = null;
        }

        var unanimous = votes.Count > 0
                        && votes.All(r => r.Plausible == votes[0].Plausible);

        double? kappa = votes.Count >= 2 ? ComputeCohensKappaPairwise(votes) : null;

        return new LlmConsensusReport(
            ProviderCount: _gateways.Count,
            ValidResponses: votes.Count,
            Responses: responses,
            Consensus: consensus,
            CohenKappa: kappa,
            UnanimousAgreement: unanimous);
    }

    /// <summary>
    /// 解析 <c>{"plausible": true|false, "reason": "..."}</c>。
    /// 解析失败或字段缺失 → null（不参与投票）。
    /// </summary>
    internal static bool? TryParsePlausible(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("plausible", out var p)) return null;
            return p.ValueKind switch
            {
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 对 n ≥ 2 个非 null votes 算平均 pair-wise Cohen's κ。
    /// κ ∈ [-1, 1]，1 = 完全一致，0 = 随机水平，&lt; 0 = 比随机更差。
    /// </summary>
    internal static double ComputeCohensKappaPairwise(IReadOnlyList<LlmProviderResponse> votes)
    {
        if (votes.Count < 2) return 0.0;

        double totalKappa = 0;
        int pairs = 0;
        for (int i = 0; i < votes.Count; i++)
        {
            for (int j = i + 1; j < votes.Count; j++)
            {
                totalKappa += PairwiseKappa(votes[i].Plausible!.Value, votes[j].Plausible!.Value, votes);
                pairs++;
            }
        }
        return pairs == 0 ? 0.0 : totalKappa / pairs;
    }

    /// <summary>
    /// 二元 rater 的 Cohen's κ（pair）：基于 marginal proba 算 expected agreement。
    /// 这里只有两个 rater 一个样本，所以 κ ∈ {-1, 1}。
    /// </summary>
    private static double PairwiseKappa(bool a, bool b, IReadOnlyList<LlmProviderResponse> allVotes)
    {
        // For two raters single binary item：
        //   po (observed agreement) = 1 if a==b else 0
        //   pe (expected agreement) 依赖 marginal rate
        //   κ = (po - pe) / (1 - pe)
        var trueRate = allVotes.Count(v => v.Plausible == true) / (double)allVotes.Count;
        var falseRate = 1.0 - trueRate;
        var pe = trueRate * trueRate + falseRate * falseRate;
        var po = a == b ? 1.0 : 0.0;
        return Math.Abs(1.0 - pe) < 1e-12 ? po : (po - pe) / (1.0 - pe);
    }
}

/// <summary>命名的 LLM gateway —— 在 consensus 报告中显示 provider 名字。</summary>
public sealed record NamedGateway(string Name, ILlmGateway Gateway);

/// <summary>单个 provider 对一次 prompt 的回应。</summary>
public sealed record LlmProviderResponse(
    string ProviderName,
    string RawText,
    bool? Plausible,
    string? ErrorMessage);

/// <summary>multi-LLM 互验报告。</summary>
public sealed record LlmConsensusReport(
    int ProviderCount,
    int ValidResponses,
    IReadOnlyList<LlmProviderResponse> Responses,
    bool? Consensus,
    double? CohenKappa,
    bool UnanimousAgreement);
