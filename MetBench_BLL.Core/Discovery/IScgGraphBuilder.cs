namespace MetBench_BLL.Discovery;

/// <summary>
/// W11 — Semantic Causal Graph (SCG) abstractions for <see cref="ScgHeuristicDiscoverer"/>.
/// </summary>
/// <remarks>
/// SCG = node-edge graph where:
/// <list type="bullet">
///   <item>nodes are SUT variables / parameters / outputs（如 "fuel.nu_sigma_f", "k_eff"）</item>
///   <item>edges encode causal relations: "causes" (X → Y means X directly affects Y),
///         "mediates" (X → M → Y), "confounds" (Z → X, Z → Y)</item>
/// </list>
///
/// Day-1 用例：把已知 SUT 关系手工拼成图 → miner 找经典 do-calculus 模式 →
/// 自动产 candidate MR。后续可让 sidecar 从源码 / spec 自动抽 SCG。
///
/// 设计契约：
/// <list type="bullet">
///   <item><see cref="IScgGraphBuilder"/> 负责拿到图（来源不限 —— 内存 / sidecar / DB）</item>
///   <item><see cref="IScgPatternMiner"/> 是纯函数 in-memory 算法，单测可 100% 覆盖</item>
///   <item>两者通过 <see cref="ScgHeuristicDiscoverer"/> 组合，符合 <see cref="IMRDiscoverer"/></item>
/// </list>
/// </remarks>
public interface IScgGraphBuilder
{
    /// <summary>构造 / 加载目标 SUT 的因果图。</summary>
    /// <param name="targetApplicationId">SUT 的 v2 Application Id；null = 全局通用图。</param>
    Task<ScgGraph> BuildAsync(int? targetApplicationId, CancellationToken ct = default);
}

/// <summary>在 SCG 上挖掘已知 do-calculus 模式 → candidate MR proposal。</summary>
public interface IScgPatternMiner
{
    /// <summary>对给定图返回所有匹配的 pattern instance。</summary>
    IReadOnlyList<ScgPatternMatch> Mine(ScgGraph graph);
}

/// <summary>因果图的不可变快照。</summary>
public sealed record ScgGraph(
    IReadOnlyList<ScgNode> Nodes,
    IReadOnlyList<ScgEdge> Edges)
{
    public int NodeCount => Nodes.Count;
    public int EdgeCount => Edges.Count;

    /// <summary>O(1) 按 Id 找节点；找不到返回 null。</summary>
    public ScgNode? FindNode(string id)
    {
        for (int i = 0; i < Nodes.Count; i++)
            if (Nodes[i].Id == id) return Nodes[i];
        return null;
    }

    /// <summary>所有 from→? 的出边。</summary>
    public IEnumerable<ScgEdge> OutEdges(string fromId)
    {
        foreach (var e in Edges)
            if (e.FromId == fromId) yield return e;
    }

    /// <summary>所有 ?→to 的入边。</summary>
    public IEnumerable<ScgEdge> InEdges(string toId)
    {
        foreach (var e in Edges)
            if (e.ToId == toId) yield return e;
    }
}

/// <summary>因果图节点。<see cref="Type"/> = "input" | "param" | "intermediate" | "output"。</summary>
public sealed record ScgNode(string Id, string Name, string Type);

/// <summary>
/// 因果图有向边。<see cref="Relation"/>:
/// <list type="bullet">
///   <item><c>"causes"</c> — direct causal effect</item>
///   <item><c>"mediates"</c> — intermediate node on a causal chain（注意 mediator 通常是 node 的 type，不是边的 relation；这里保留以便扩展）</item>
///   <item><c>"confounds"</c> — common cause 公共原因</item>
/// </list>
/// </summary>
public sealed record ScgEdge(string FromId, string ToId, string Relation);

/// <summary>
/// 一次 mine 命中的 pattern。<see cref="PatternKind"/>:
/// <list type="bullet">
///   <item><c>"direct-cause"</c> — X → Y，可推导 monotonicity MR</item>
///   <item><c>"mediator"</c> — X → M → Y，可推导 do-intervention MR</item>
///   <item><c>"confounder"</c> — Z 同时 cause X 和 Y，可推导 invariance under (Z held constant) MR</item>
/// </list>
/// </summary>
public sealed record ScgPatternMatch(
    string PatternKind,
    string HumanName,
    string AffectedVariable,
    string? TransformationHint,
    string? AssertionHint,
    string CausalExplanation,
    double Confidence,
    int Hash);
