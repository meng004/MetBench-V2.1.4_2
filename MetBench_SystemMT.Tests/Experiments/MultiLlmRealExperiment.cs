using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MetBench_BLL.Discovery;
using Xunit;
using Xunit.Abstractions;

namespace MetBench_SystemMT.Tests.Experiments;

/// <summary>
/// W11.2 — F12 Multi-LLM Consensus 真实实验。
/// </summary>
/// <remarks>
/// 与日常 CI 隔离：仅当 <c>METBENCH_LLM_EXPERIMENT=1</c> 时跑（默认 [Skip]）。
///
/// 加载顺序：
/// <list type="number">
///   <item>检查 <c>METBENCH_LLM_EXPERIMENT</c> 环境变量；未设 → skip</item>
///   <item>读 <c>.env</c> 文件（如果存在）→ 加载到 process env</item>
///   <item>读 <c>DEEPSEEK_*</c> / <c>OPENAI_*</c> / <c>CLAUDE_*</c> 三组 base/key/model</item>
///   <item>构 3 个 NamedGateway，跑 <c>MultiLlmConsensusValidator</c>over <c>prompts.json</c></item>
///   <item>结果 + 统计写到 <c>docs/experiments/2026-05-w11-llm-consensus/results.json</c></item>
/// </list>
///
/// 跑法：
/// <code>
/// METBENCH_LLM_EXPERIMENT=1 dotnet test --filter "FullyQualifiedName~MultiLlmRealExperiment"
/// </code>
///
/// 注：本类**不**入 CI；trx / 结果文件入 git 作论文附录数据。
/// </remarks>
public sealed class MultiLlmRealExperiment
{
    private readonly ITestOutputHelper _output;
    public MultiLlmRealExperiment(ITestOutputHelper output) { _output = output; }

    [Fact]
    public async Task Run_consensus_across_three_providers()
    {
        if (Environment.GetEnvironmentVariable("METBENCH_LLM_EXPERIMENT") != "1")
        {
            _output.WriteLine("Skipped: set METBENCH_LLM_EXPERIMENT=1 to run.");
            return;
        }
        var repoRoot = FindRepoRoot();
        LoadDotEnvIfPresent(repoRoot);

        var prompts = LoadPromptCorpus(Path.Combine(repoRoot, "docs", "experiments",
            "2026-05-w11-llm-consensus", "prompts.json"));

        var gateways = BuildGateways();
        Assert.True(gateways.Count >= 2,
            $"Need ≥ 2 LLM providers configured in .env; got {gateways.Count}. " +
            "Set DEEPSEEK_API_KEY / OPENAI_API_KEY / CLAUDE_API_KEY.");

        _output.WriteLine($"Running {prompts.Count} candidates × {gateways.Count} providers " +
                          $"= {prompts.Count * gateways.Count} LLM calls.");

        var validator = new MultiLlmConsensusValidator(gateways);
        var results = new List<ExperimentResult>(prompts.Count);
        var sw = Stopwatch.StartNew();

        foreach (var p in prompts)
        {
            var prompt = BuildPrompt(p);
            try
            {
                var report = await validator.AssessAsync(prompt);
                results.Add(new ExperimentResult(
                    Id: p.Id,
                    Expected: p.Expected,
                    Consensus: report.Consensus,
                    Unanimous: report.UnanimousAgreement,
                    Kappa: report.CohenKappa,
                    ProviderResponses: report.Responses
                        .Select(r => new ProviderResp(r.ProviderName, r.Plausible, Truncate(r.RawText, 200), r.ErrorMessage))
                        .ToList()));
                _output.WriteLine($"  [{p.Id}] expected={p.Expected} → consensus={report.Consensus} " +
                                  $"unanimous={report.UnanimousAgreement} κ={report.CohenKappa?.ToString("F3") ?? "n/a"}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"  [{p.Id}] EXCEPTION: {ex.Message}");
                results.Add(new ExperimentResult(p.Id, p.Expected, null, false, null,
                    new List<ProviderResp> { new("(orchestrator)", null, "", ex.Message) }));
            }
        }

        sw.Stop();

        var summary = ComputeSummary(results);
        var outDir = Path.Combine(repoRoot, "docs", "experiments", "2026-05-w11-llm-consensus");
        Directory.CreateDirectory(outDir);
        var resultsPath = Path.Combine(outDir, "results.json");
        var serialized = JsonSerializer.Serialize(new
        {
            ranAt = DateTimeOffset.UtcNow,
            providers = gateways.Select(g => g.Name).ToArray(),
            promptCount = prompts.Count,
            totalElapsed = sw.Elapsed.ToString(),
            summary,
            results,
        }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(resultsPath, serialized);

        _output.WriteLine($"\n===== Summary =====");
        _output.WriteLine($"  consensus accuracy: {summary.ConsensusAccuracy:P1}");
        _output.WriteLine($"  unanimous rate:     {summary.UnanimousRate:P1}");
        _output.WriteLine($"  mean κ:             {summary.MeanKappa:F3}");
        _output.WriteLine($"  results: {resultsPath}");
    }

    // ===== gateway construction =====

    private static List<NamedGateway> BuildGateways()
    {
        var list = new List<NamedGateway>();
        TryAdd(list, "deepseek", "DEEPSEEK_BASE_URL", "DEEPSEEK_API_KEY", "DEEPSEEK_MODEL");
        TryAdd(list, "openai",   "OPENAI_BASE_URL",   "OPENAI_API_KEY",   "OPENAI_MODEL");
        TryAdd(list, "claude",   "CLAUDE_BASE_URL",   "CLAUDE_API_KEY",   "CLAUDE_MODEL");
        return list;
    }

    private static void TryAdd(List<NamedGateway> list, string name, string baseVar, string keyVar, string modelVar)
    {
        var baseUrl = Environment.GetEnvironmentVariable(baseVar);
        var apiKey = Environment.GetEnvironmentVariable(keyVar);
        var model = Environment.GetEnvironmentVariable(modelVar);
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
            return;
        list.Add(new NamedGateway(name, new OpenAiCompatibleLlmGateway(baseUrl!, apiKey!, model!)));
    }

    // ===== prompt loading & rendering =====

    internal sealed record PromptCorpusEntry(
        string Id, string Program, string Transformation, string Assertion, bool? Expected, string Note);

    private static List<PromptCorpusEntry> LoadPromptCorpus(string path)
    {
        using var s = File.OpenRead(path);
        using var doc = JsonDocument.Parse(s);
        var list = new List<PromptCorpusEntry>();
        foreach (var c in doc.RootElement.GetProperty("candidates").EnumerateArray())
        {
            bool? expected = c.TryGetProperty("expected", out var e) && e.ValueKind == JsonValueKind.True ? true
                           : c.TryGetProperty("expected", out var e2) && e2.ValueKind == JsonValueKind.False ? false
                           : null;
            list.Add(new PromptCorpusEntry(
                c.GetProperty("id").GetString() ?? "(no-id)",
                c.TryGetProperty("program", out var pg) ? (pg.GetString() ?? "") : "",
                c.TryGetProperty("transformation", out var tf) ? (tf.GetString() ?? "") : "",
                c.TryGetProperty("assertion", out var asn) ? (asn.GetString() ?? "") : "",
                expected,
                c.TryGetProperty("note", out var nt) ? (nt.GetString() ?? "") : ""));
        }
        return list;
    }

    private static string BuildPrompt(PromptCorpusEntry p)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert in metamorphic testing for scientific computing programs.");
        sb.AppendLine("Reply ONLY with a single JSON object on one line of the form:");
        sb.AppendLine("{\"plausible\": true|false, \"reason\": \"...\"}");
        sb.AppendLine();
        sb.AppendLine($"Program: {p.Program}");
        sb.AppendLine($"Transformation: {p.Transformation}");
        sb.AppendLine($"Candidate metamorphic assertion: {p.Assertion}");
        sb.AppendLine();
        sb.AppendLine("Question: Is this metamorphic relation physically / mathematically plausible? Reply with JSON only.");
        return sb.ToString();
    }

    // ===== aggregation =====

    internal sealed record ExperimentResult(
        string Id, bool? Expected, bool? Consensus, bool Unanimous, double? Kappa,
        IReadOnlyList<ProviderResp> ProviderResponses);

    internal sealed record ProviderResp(string Provider, bool? Plausible, string RawText, string? Error);

    internal sealed record Summary(
        int Total, int ConsensusMatched, double ConsensusAccuracy,
        int UnanimousCount, double UnanimousRate, double MeanKappa);

    private static Summary ComputeSummary(IReadOnlyList<ExperimentResult> results)
    {
        var labeled = results.Where(r => r.Expected.HasValue).ToList();
        var matched = labeled.Count(r => r.Consensus.HasValue && r.Consensus == r.Expected);
        var acc = labeled.Count > 0 ? matched / (double)labeled.Count : 0.0;
        var unan = results.Count(r => r.Unanimous);
        var unanRate = results.Count > 0 ? unan / (double)results.Count : 0.0;
        var kappas = results.Where(r => r.Kappa.HasValue).Select(r => r.Kappa!.Value).ToList();
        var meanKappa = kappas.Count > 0 ? kappas.Average() : 0.0;
        return new Summary(results.Count, matched, acc, unan, unanRate, meanKappa);
    }

    // ===== utilities =====

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "MetBench.sln"))) return dir;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        throw new InvalidOperationException("Could not locate repo root (MetBench.sln).");
    }

    private static void LoadDotEnvIfPresent(string repoRoot)
    {
        var envPath = Path.Combine(repoRoot, ".env");
        if (!File.Exists(envPath)) return;
        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;
            var idx = trimmed.IndexOf('=');
            if (idx <= 0) continue;
            var key = trimmed.Substring(0, idx).Trim();
            var val = trimmed.Substring(idx + 1).Trim().Trim('"').Trim('\'');
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, val);
        }
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max) + "...";
}
