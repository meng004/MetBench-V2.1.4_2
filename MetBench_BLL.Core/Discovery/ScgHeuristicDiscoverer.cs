namespace MetBench_BLL.Discovery;

/// <summary>
/// W11 — 第 3 类 MR 识别方法：基于 Semantic Causal Graph (SCG) 的启发式。
/// </summary>
/// <remarks>
/// 与既有两类的对比：
/// <list type="table">
///   <listheader><term>类</term><description>工作方式</description></listheader>
///   <item><term><see cref="MetaPatternDiscoverer"/></term><description>枚举式：8 NOETHER MetaPattern × SUT 参数空间</description></item>
///   <item><term><see cref="LlmNativeDiscoverer"/></term><description>自由提议：LLM prompt 直接产 candidate</description></item>
///   <item><term><see cref="ScgHeuristicDiscoverer"/></term><description>结构式：因果图模式匹配（do-calculus / mediator / confounder）</description></item>
/// </list>
///
/// 流程：
/// <list type="number">
///   <item><see cref="IScgGraphBuilder"/> 给出目标 SUT 的因果图</item>
///   <item><see cref="IScgPatternMiner"/> 找经典 pattern</item>
///   <item>每个 pattern 实例化为 <see cref="CandidateMrProposal"/></item>
/// </list>
///
/// 论文价值：给 reviewer 演示 MR 识别的扩展机制 —— 同一 <c>IMRDiscoverer</c> 接口下，
/// MetaPattern + LLM + SCG 三类方法互不依赖、可独立验证、可并行 fan-out 投票。
/// </remarks>
public sealed class ScgHeuristicDiscoverer : IMRDiscoverer
{
    public string Name => "scg-heuristic";

    private readonly IScgGraphBuilder _builder;
    private readonly IScgPatternMiner _miner;

    public ScgHeuristicDiscoverer(IScgGraphBuilder builder, IScgPatternMiner miner)
    {
        _builder = builder;
        _miner = miner;
    }

    public async Task<DiscoveryRunOutcome> DiscoverAsync(
        int? targetApplicationId, CancellationToken ct = default)
    {
        ScgGraph graph;
        try
        {
            graph = await _builder.BuildAsync(targetApplicationId, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DiscoveryRunOutcome(
                Status: "error",
                InvocationDetails: $"scg-builder-failed (app={targetApplicationId})",
                Proposals: Array.Empty<CandidateMrProposal>(),
                ErrorMessage: $"SCG build failed: {ex.Message}");
        }

        IReadOnlyList<ScgPatternMatch> matches;
        try
        {
            matches = _miner.Mine(graph);
        }
        catch (Exception ex)
        {
            return new DiscoveryRunOutcome(
                Status: "error",
                InvocationDetails: $"scg-nodes:{graph.NodeCount} edges:{graph.EdgeCount}",
                Proposals: Array.Empty<CandidateMrProposal>(),
                ErrorMessage: $"SCG mine failed: {ex.Message}");
        }

        var proposals = new List<CandidateMrProposal>(matches.Count);
        foreach (var m in matches)
        {
            proposals.Add(new CandidateMrProposal(
                ProposedCode: $"SCG-{m.PatternKind}-{m.Hash:X8}",
                ProposedName: m.HumanName,
                SuggestedTransformationName: m.TransformationHint,
                SuggestedAssertionTypeCode: m.AssertionHint,
                ProposedValueName: m.AffectedVariable,
                Rationale: m.CausalExplanation,
                Confidence: m.Confidence));
        }

        return new DiscoveryRunOutcome(
            Status: "ok",
            InvocationDetails: $"scg-nodes:{graph.NodeCount} edges:{graph.EdgeCount} matches:{matches.Count}",
            Proposals: proposals);
    }
}
