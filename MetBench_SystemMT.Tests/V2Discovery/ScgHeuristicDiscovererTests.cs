using MetBench_BLL.Discovery;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

/// <summary>
/// W11.1 — SCG-Heuristic Discoverer 骨架测试。
/// </summary>
public sealed class ScgHeuristicDiscovererTests
{
    // ===== ScgHeuristicDiscoverer orchestration =====

    [Fact]
    public async Task Returns_proposal_per_pattern_match()
    {
        var graph = new ScgGraph(
            Nodes: new[]
            {
                new ScgNode("x", "input X", "input"),
                new ScgNode("y", "output Y", "output"),
            },
            Edges: new[] { new ScgEdge("x", "y", "causes") });
        var d = new ScgHeuristicDiscoverer(new FakeBuilder(graph), new RuleBasedScgPatternMiner());

        var outcome = await d.DiscoverAsync(targetApplicationId: 1);

        Assert.Equal("ok", outcome.Status);
        Assert.Single(outcome.Proposals);
        Assert.StartsWith("SCG-direct-cause-", outcome.Proposals[0].ProposedCode);
        Assert.Contains("scg-nodes:2", outcome.InvocationDetails);
    }

    [Fact]
    public async Task Builder_failure_yields_error_status()
    {
        var d = new ScgHeuristicDiscoverer(
            new ThrowingBuilder(new InvalidOperationException("boom")),
            new RuleBasedScgPatternMiner());

        var outcome = await d.DiscoverAsync(null);

        Assert.Equal("error", outcome.Status);
        Assert.Empty(outcome.Proposals);
        Assert.Contains("boom", outcome.ErrorMessage);
    }

    [Fact]
    public async Task Miner_failure_yields_error_status()
    {
        var graph = new ScgGraph(Array.Empty<ScgNode>(), Array.Empty<ScgEdge>());
        var d = new ScgHeuristicDiscoverer(
            new FakeBuilder(graph),
            new ThrowingMiner(new Exception("miner-broke")));

        var outcome = await d.DiscoverAsync(null);

        Assert.Equal("error", outcome.Status);
        Assert.Contains("miner-broke", outcome.ErrorMessage);
    }

    [Fact]
    public async Task Empty_graph_yields_zero_proposals_ok_status()
    {
        var d = new ScgHeuristicDiscoverer(
            new FakeBuilder(new ScgGraph(Array.Empty<ScgNode>(), Array.Empty<ScgEdge>())),
            new RuleBasedScgPatternMiner());

        var outcome = await d.DiscoverAsync(null);

        Assert.Equal("ok", outcome.Status);
        Assert.Empty(outcome.Proposals);
        Assert.Contains("nodes:0 edges:0", outcome.InvocationDetails);
    }

    [Fact]
    public async Task Cancellation_during_build_propagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var d = new ScgHeuristicDiscoverer(new CancellingBuilder(), new RuleBasedScgPatternMiner());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => d.DiscoverAsync(null, cts.Token));
    }

    [Fact]
    public void Discoverer_name_is_scg_heuristic()
    {
        var d = new ScgHeuristicDiscoverer(
            new FakeBuilder(new ScgGraph(Array.Empty<ScgNode>(), Array.Empty<ScgEdge>())),
            new RuleBasedScgPatternMiner());
        Assert.Equal("scg-heuristic", d.Name);
    }

    // ===== RuleBasedScgPatternMiner pattern detection =====

    [Fact]
    public void DirectCause_pattern_produces_monotonic_hint()
    {
        var graph = MakeGraph(
            nodes: new[] { ("x", "param X", "param"), ("y", "out Y", "output") },
            edges: new[] { ("x", "y", "causes") });

        var matches = new RuleBasedScgPatternMiner().Mine(graph);

        var m = Assert.Single(matches);
        Assert.Equal("direct-cause", m.PatternKind);
        Assert.Equal("monotonic", m.AssertionHint);
        Assert.Equal("y", m.AffectedVariable);
        Assert.Contains("scale-x", m.TransformationHint);
    }

    [Fact]
    public void Mediator_pattern_only_when_no_direct_edge()
    {
        // X → M → Y, no X → Y direct → mediator detected
        var graph = MakeGraph(
            nodes: new[]
            {
                ("x", "X", "input"), ("m", "M", "intermediate"), ("y", "Y", "output"),
            },
            edges: new[]
            {
                ("x", "m", "causes"),
                ("m", "y", "causes"),
            });

        var matches = new RuleBasedScgPatternMiner().Mine(graph);

        Assert.Contains(matches, m => m.PatternKind == "mediator"
            && m.AffectedVariable == "y"
            && m.TransformationHint == "do-fix-m");
    }

    [Fact]
    public void Mediator_pattern_skipped_when_direct_X_Y_edge_present()
    {
        var graph = MakeGraph(
            nodes: new[]
            {
                ("x", "X", "input"), ("m", "M", "intermediate"), ("y", "Y", "output"),
            },
            edges: new[]
            {
                ("x", "m", "causes"),
                ("m", "y", "causes"),
                ("x", "y", "causes"),  // direct shortcut → no mediator
            });

        var matches = new RuleBasedScgPatternMiner().Mine(graph);

        Assert.DoesNotContain(matches, m => m.PatternKind == "mediator");
        Assert.Contains(matches, m => m.PatternKind == "direct-cause");
    }

    [Fact]
    public void Confounder_pattern_detects_common_cause()
    {
        // Z → X and Z → Y, no X↔Y → confounder
        var graph = MakeGraph(
            nodes: new[]
            {
                ("z", "Z", "input"), ("x", "X", "intermediate"), ("y", "Y", "output"),
            },
            edges: new[]
            {
                ("z", "x", "causes"),
                ("z", "y", "causes"),
            });

        var matches = new RuleBasedScgPatternMiner().Mine(graph);

        Assert.Contains(matches, m => m.PatternKind == "confounder"
            && m.TransformationHint == "do-fix-z");
    }

    [Fact]
    public void Confounder_pattern_skipped_when_X_Y_directly_connected()
    {
        var graph = MakeGraph(
            nodes: new[]
            {
                ("z", "Z", "input"), ("x", "X", "intermediate"), ("y", "Y", "output"),
            },
            edges: new[]
            {
                ("z", "x", "causes"),
                ("z", "y", "causes"),
                ("x", "y", "causes"),  // X→Y direct → not pure confounder
            });

        var matches = new RuleBasedScgPatternMiner().Mine(graph);

        Assert.DoesNotContain(matches, m => m.PatternKind == "confounder");
    }

    [Fact]
    public void Pattern_match_hash_dedups_duplicate_edges()
    {
        // Same x→y edge listed twice: only one direct-cause match.
        var graph = MakeGraph(
            nodes: new[] { ("x", "X", "param"), ("y", "Y", "output") },
            edges: new[]
            {
                ("x", "y", "causes"),
                ("x", "y", "causes"),
            });

        var matches = new RuleBasedScgPatternMiner().Mine(graph);

        Assert.Single(matches, m => m.PatternKind == "direct-cause");
    }

    [Fact]
    public void ScgGraph_FindNode_returns_null_for_missing()
    {
        var g = new ScgGraph(
            new[] { new ScgNode("a", "A", "input") },
            Array.Empty<ScgEdge>());
        Assert.NotNull(g.FindNode("a"));
        Assert.Null(g.FindNode("missing"));
    }

    [Fact]
    public void ScgGraph_OutEdges_InEdges_enumerate_correctly()
    {
        var g = MakeGraph(
            nodes: new[] { ("a", "A", "input"), ("b", "B", "out"), ("c", "C", "out") },
            edges: new[] { ("a", "b", "causes"), ("a", "c", "causes") });
        Assert.Equal(2, g.OutEdges("a").Count());
        Assert.Single(g.InEdges("b"));
        Assert.Empty(g.OutEdges("b"));
    }

    // ===== helpers =====

    private static ScgGraph MakeGraph(
        (string id, string name, string type)[] nodes,
        (string from, string to, string rel)[] edges)
    {
        return new ScgGraph(
            nodes.Select(n => new ScgNode(n.id, n.name, n.type)).ToList(),
            edges.Select(e => new ScgEdge(e.from, e.to, e.rel)).ToList());
    }

    private sealed class FakeBuilder : IScgGraphBuilder
    {
        private readonly ScgGraph _graph;
        public FakeBuilder(ScgGraph g) { _graph = g; }
        public Task<ScgGraph> BuildAsync(int? targetApplicationId, CancellationToken ct = default)
            => Task.FromResult(_graph);
    }

    private sealed class ThrowingBuilder : IScgGraphBuilder
    {
        private readonly Exception _ex;
        public ThrowingBuilder(Exception ex) { _ex = ex; }
        public Task<ScgGraph> BuildAsync(int? targetApplicationId, CancellationToken ct = default)
            => throw _ex;
    }

    private sealed class CancellingBuilder : IScgGraphBuilder
    {
        public Task<ScgGraph> BuildAsync(int? targetApplicationId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new ScgGraph(Array.Empty<ScgNode>(), Array.Empty<ScgEdge>()));
        }
    }

    private sealed class ThrowingMiner : IScgPatternMiner
    {
        private readonly Exception _ex;
        public ThrowingMiner(Exception ex) { _ex = ex; }
        public IReadOnlyList<ScgPatternMatch> Mine(ScgGraph graph) => throw _ex;
    }
}
