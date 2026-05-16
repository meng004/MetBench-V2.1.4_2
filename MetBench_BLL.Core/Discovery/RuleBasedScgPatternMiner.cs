namespace MetBench_BLL.Discovery;

/// <summary>
/// W11 — Day-1 SCG pattern miner：纯 C# in-memory，识别 3 类经典 do-calculus 模式。
/// </summary>
/// <remarks>
/// 模式：
/// <list type="bullet">
///   <item><b>direct-cause</b> — 一条 X → Y 边 → 单调性 MR 候选（若 X 上升 Y 上升 / 下降，跑实验确定方向）</item>
///   <item><b>mediator</b>     — X → M → Y → do-intervention MR：固定 M 时调 X，Y 应不变</item>
///   <item><b>confounder</b>   — Z → X 且 Z → Y → invariance MR：固定 Z 时 X 与 Y 应解耦</item>
/// </list>
/// 每个 pattern instance 经 hash 去重（防止同一三元组重复产 MR）。
///
/// Day-2 扩展点：
/// <list type="bullet">
///   <item>collider / chain / fork 的更细分类</item>
///   <item>引入边权重 → 排序 candidate</item>
///   <item>用 do-calculus 严格推导是否真正可识别（identifiability）</item>
/// </list>
/// </remarks>
public sealed class RuleBasedScgPatternMiner : IScgPatternMiner
{
    public IReadOnlyList<ScgPatternMatch> Mine(ScgGraph graph)
    {
        var matches = new List<ScgPatternMatch>();
        var seen = new HashSet<int>();

        // ===== Pattern 1: direct-cause =====
        foreach (var edge in graph.Edges)
        {
            if (edge.Relation != "causes") continue;
            var x = graph.FindNode(edge.FromId);
            var y = graph.FindNode(edge.ToId);
            if (x is null || y is null) continue;

            var hash = HashCode.Combine("direct-cause", x.Id, y.Id);
            if (!seen.Add(hash)) continue;

            matches.Add(new ScgPatternMatch(
                PatternKind: "direct-cause",
                HumanName: $"Monotonic effect: {x.Name} → {y.Name}",
                AffectedVariable: y.Id,
                TransformationHint: $"scale-{x.Id}",
                AssertionHint: "monotonic",
                CausalExplanation: $"SCG edge {x.Id} causes {y.Id}. Hypothesis: " +
                                   $"perturbing {x.Name} should produce a monotone change in {y.Name}, " +
                                   $"sign to be determined empirically.",
                Confidence: 0.7,
                Hash: hash));
        }

        // ===== Pattern 2: mediator =====
        //   X → M → Y where neither X→Y directly
        foreach (var xm in graph.Edges.Where(e => e.Relation == "causes"))
        {
            foreach (var my in graph.Edges.Where(e => e.Relation == "causes" && e.FromId == xm.ToId))
            {
                if (xm.FromId == my.ToId) continue; // skip self-cycle
                // skip if direct X→Y already exists
                if (graph.Edges.Any(e =>
                    e.Relation == "causes" && e.FromId == xm.FromId && e.ToId == my.ToId))
                    continue;

                var x = graph.FindNode(xm.FromId);
                var m = graph.FindNode(xm.ToId);
                var y = graph.FindNode(my.ToId);
                if (x is null || m is null || y is null) continue;

                var hash = HashCode.Combine("mediator", x.Id, m.Id, y.Id);
                if (!seen.Add(hash)) continue;

                matches.Add(new ScgPatternMatch(
                    PatternKind: "mediator",
                    HumanName: $"Mediator chain: {x.Name} → {m.Name} → {y.Name}",
                    AffectedVariable: y.Id,
                    TransformationHint: $"do-fix-{m.Id}",
                    AssertionHint: "approx-invariant",
                    CausalExplanation: $"SCG path {x.Id} → {m.Id} → {y.Id}. " +
                                       $"do({m.Name} = const) should make {y.Name} insensitive to {x.Name}.",
                    Confidence: 0.6,
                    Hash: hash));
            }
        }

        // ===== Pattern 3: confounder =====
        //   Z → X and Z → Y, no edge between X and Y
        var causesByFrom = graph.Edges
            .Where(e => e.Relation == "causes")
            .GroupBy(e => e.FromId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ToId).ToList());

        foreach (var (zId, children) in causesByFrom)
        {
            for (int i = 0; i < children.Count; i++)
            {
                for (int j = i + 1; j < children.Count; j++)
                {
                    var xId = children[i];
                    var yId = children[j];
                    // require no direct edge X→Y or Y→X
                    bool hasDirect = graph.Edges.Any(e =>
                        e.Relation == "causes" &&
                        ((e.FromId == xId && e.ToId == yId) || (e.FromId == yId && e.ToId == xId)));
                    if (hasDirect) continue;

                    var z = graph.FindNode(zId);
                    var x = graph.FindNode(xId);
                    var y = graph.FindNode(yId);
                    if (z is null || x is null || y is null) continue;

                    var hash = HashCode.Combine("confounder", z.Id, x.Id, y.Id);
                    if (!seen.Add(hash)) continue;

                    matches.Add(new ScgPatternMatch(
                        PatternKind: "confounder",
                        HumanName: $"Confounder: {z.Name} → ({x.Name}, {y.Name})",
                        AffectedVariable: y.Id,
                        TransformationHint: $"do-fix-{z.Id}",
                        AssertionHint: "approx-invariant",
                        CausalExplanation: $"SCG: {z.Id} confounds {x.Id} and {y.Id}. " +
                                           $"Holding {z.Name} constant should decouple {x.Name} and {y.Name}.",
                        Confidence: 0.55,
                        Hash: hash));
                }
            }
        }

        return matches;
    }
}
