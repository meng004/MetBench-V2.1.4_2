using MetBench_BLL.Discovery;
using MetBench_SystemMT.Tests.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

/// <summary>
/// F7 集成 smoke：MetaPatternDiscoverer 真实 spawn python3 noether_candidates.py，
/// 解析 stdout JSON → CandidateMrProposal。
///
/// CI Linux 环境备齐 python3 + 仓库内脚本，所以无 skip 机制 —— 缺则失败，提示环境问题。
/// </summary>
public sealed class MetaPatternDiscovererIntegrationTests
{
    private static readonly string RepoRoot = LocateRepoRoot();
    private static readonly string ScriptPath = Path.Combine(RepoRoot, "tools", "noether_candidates.py");
    private static readonly string PythonExe = TestAssetPaths.PythonExecutable();

    [Fact]
    public async Task DiscoverAsync_real_python_sidecar_returns_realizable_candidates()
    {
        Assert.True(File.Exists(ScriptPath),
            $"noether script must exist at {ScriptPath} (repo root inferred = {RepoRoot})");
        var discoverer = new MetaPatternDiscoverer(PythonExe, ScriptPath);

        var outcome = await discoverer.DiscoverAsync(targetApplicationId: null);

        Assert.Equal("ok", outcome.Status);
        Assert.True(outcome.Proposals.Count >= 3,
            $"expected ≥3 realizable proposals, got {outcome.Proposals.Count}");
        // 至少含一个 m_inv 和一个 m_mono（基础 4 + 5）
        var codes = outcome.Proposals.Select(p => p.ProposedCode).ToList();
        Assert.Contains(codes, c => c.Contains("inv-") || c.StartsWith("MR01") || c.StartsWith("MR02"));
        Assert.Contains(codes, c => c.Contains("mono-") || c.StartsWith("MR05") || c.StartsWith("MR06"));
    }

    [Fact]
    public async Task DiscoverAsync_invalid_python_path_throws()
    {
        var discoverer = new MetaPatternDiscoverer("nonexistent-python-12345", ScriptPath);

        // Process.Start fails to find binary → throws (cleanest contract for caller).
        await Assert.ThrowsAnyAsync<Exception>(() =>
            discoverer.DiscoverAsync(targetApplicationId: null));
    }

    [Fact]
    public async Task DiscoverAsync_invalid_script_path_returns_error_outcome()
    {
        var discoverer = new MetaPatternDiscoverer(PythonExe, "/nonexistent/script.py");

        var outcome = await discoverer.DiscoverAsync(targetApplicationId: null);

        Assert.Equal("error", outcome.Status);
        Assert.NotNull(outcome.ErrorMessage);
        Assert.Empty(outcome.Proposals);
    }

    [Fact]
    public async Task DiscoverAsync_proposals_have_required_fields()
    {
        Assert.True(File.Exists(ScriptPath), "noether script must exist");
        var discoverer = new MetaPatternDiscoverer(PythonExe, ScriptPath);

        var outcome = await discoverer.DiscoverAsync(targetApplicationId: null);

        Assert.NotEmpty(outcome.Proposals);
        foreach (var p in outcome.Proposals)
        {
            Assert.False(string.IsNullOrEmpty(p.ProposedCode), $"missing code in {p}");
            Assert.False(string.IsNullOrEmpty(p.ProposedName), $"missing name in {p.ProposedCode}");
            Assert.Equal("k_eff", p.ProposedValueName);  // 当前 SUT 主断言量
            Assert.True(p.Confidence > 0, $"confidence must be > 0, got {p.Confidence} for {p.ProposedCode}");
        }
    }

    private static string LocateRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "MetBench.sln")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        return AppContext.BaseDirectory;
    }
}
