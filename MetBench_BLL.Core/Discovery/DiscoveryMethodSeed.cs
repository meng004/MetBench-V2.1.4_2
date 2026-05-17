using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Discovery;

/// <summary>
/// 把 v2.1 / W11 三类 MR 识别方法的"权威定义"灌入 LiteDB。
/// </summary>
/// <remarks>
/// 启动时调用一次 <see cref="EnsureSeeded"/>：每个 (Name, Version) 不存在则 Add。
/// 已存在的不动 —— 用户可能从 UI 调过 ConfigJson / Enabled，不要覆盖。
///
/// 修改方式：
///   • 加新方法（如 W12 加 4th discoverer）: 在 <see cref="CanonicalSet"/> 加一条
///   • 改既有: 从 WPF / Repository.Modify 改 —— 这里只负责初始化
/// </remarks>
public static class DiscoveryMethodSeed
{
    /// <summary>三类 MR 识别方法的权威定义。</summary>
    public static readonly IReadOnlyList<DiscoveryMethod> CanonicalSet = new[]
    {
        new DiscoveryMethod
        {
            Name = "MetaPattern-Structural",
            Version = "NOETHER-v1",
            ConfigJson = """{"script":"tools/noether_candidates.py"}""",
            Enabled = true,
            Description = "基于 8 个 NOETHER MetaPattern 模板 × SUT 参数空间枚举式实例化。",
        },
        new DiscoveryMethod
        {
            Name = "LLM-Native",
            Version = "v1",
            ConfigJson = """{"min_confidence":0.0,"max_candidates":5}""",
            Enabled = true,
            Description = "把 SUT 文档 + 既有 MR 当 prompt，让 LLM 自由提议新 MR。",
        },
        new DiscoveryMethod
        {
            Name = "SCG-Heuristic",
            Version = "scg-v1",
            ConfigJson = """{"patterns":["direct-cause","mediator","confounder"],"min_confidence":0.5}""",
            Enabled = true,
            Description = "W11.1 — 基于 Semantic Causal Graph 的 do-calculus 模式挖掘：" +
                          "direct-cause / mediator / confounder 三类，给跨域 SUT 演示扩展性。",
        },
    };

    /// <summary>
    /// 若 DB 中没有某 (Name, Version) → Add。其余 no-op。
    /// </summary>
    /// <returns>本次新插入的行数。</returns>
    public static int EnsureSeeded(IDiscoveryMethodRepository repo)
    {
        var inserted = 0;
        foreach (var dm in CanonicalSet)
        {
            if (repo.GetByNameVersion(dm.Name, dm.Version) is null)
            {
                repo.Add(dm);
                inserted++;
            }
        }
        return inserted;
    }
}
