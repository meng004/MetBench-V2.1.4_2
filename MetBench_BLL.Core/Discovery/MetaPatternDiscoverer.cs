using System.Diagnostics;
using System.Text.Json;

namespace MetBench_BLL.Discovery;

/// <summary>
/// 调既有 Python <c>tools/noether_candidates.py</c> → 解析 JSON → CandidateMrProposal。
/// </summary>
/// <remarks>
/// 之所以走 sidecar 而非 C# 重写：noether_candidates.py 已有审稿过的 candidate 列表 +
/// 物理 rationale，未来扩 MetaPattern 表只动 Python 即可，C# 不重复维护。
///
/// JSON 契约（<c>python noether_candidates.py</c> 的 stdout）：
/// <code>
/// {
///   "metapatterns": { ... },
///   "candidates": [
///     {"id": "MR01-...", "meta_pattern": "m_inv", "block": "G", "parameter": "...",
///      "description": "...", "hypothesis": "...", "assertion": "approx",
///      "physical_rationale": "...", "realizable_with_current_sut": true},
///     ...
///   ]
/// }
/// </code>
/// 只取 <c>realizable_with_current_sut == true</c> 的条目当 proposal。
/// </remarks>
public sealed class MetaPatternDiscoverer : IMRDiscoverer
{
    public string Name => "metapattern-noether";

    private readonly string _pythonExe;
    private readonly string _scriptPath;

    public MetaPatternDiscoverer(string pythonExe, string scriptPath)
    {
        _pythonExe = pythonExe;
        _scriptPath = scriptPath;
    }

    public async Task<DiscoveryRunOutcome> DiscoverAsync(int? targetApplicationId, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(_pythonExe, $"\"{_scriptPath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start {_pythonExe} {_scriptPath}");

        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
        var stderr = await proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0)
        {
            return new DiscoveryRunOutcome(
                Status: "error",
                InvocationDetails: $"{_pythonExe} {_scriptPath}",
                Proposals: Array.Empty<CandidateMrProposal>(),
                ErrorMessage: $"python exited {proc.ExitCode}: {stderr}");
        }

        var proposals = ParseProposals(stdout);
        return new DiscoveryRunOutcome(
            Status: "ok",
            InvocationDetails: $"{_pythonExe} {_scriptPath}",
            Proposals: proposals);
    }

    internal static IReadOnlyList<CandidateMrProposal> ParseProposals(string json)
    {
        var list = new List<CandidateMrProposal>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
            return list;

        foreach (var c in candidates.EnumerateArray())
        {
            var realizable = c.TryGetProperty("realizable_with_current_sut", out var rprop)
                             && rprop.ValueKind == JsonValueKind.True;
            if (!realizable) continue;

            var id = c.GetProperty("id").GetString() ?? "(missing-id)";
            var description = c.TryGetProperty("description", out var d) ? (d.GetString() ?? "") : "";
            var hypothesis = c.TryGetProperty("hypothesis", out var h) ? (h.GetString() ?? "") : "";
            var rationale = c.TryGetProperty("physical_rationale", out var pr) ? (pr.GetString() ?? "") : "";
            var assertion = c.TryGetProperty("assertion", out var a) ? (a.GetString() ?? "") : "";

            list.Add(new CandidateMrProposal(
                ProposedCode: id,
                ProposedName: description,
                SuggestedTransformationName: null,
                SuggestedAssertionTypeCode: assertion,
                ProposedValueName: "k_eff",
                Rationale: $"{hypothesis}\n\n{rationale}",
                Confidence: 0.8));
        }
        return list;
    }
}
