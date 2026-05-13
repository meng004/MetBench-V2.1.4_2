namespace MetBench_BLL.Discovery;

/// <summary>
/// 一次 <see cref="IMRDiscoverer"/> 跑完后的纯结果（尚未落库的 in-memory 视图）。
/// </summary>
public sealed record DiscoveryRunOutcome(
    string Status,
    string? InvocationDetails,
    IReadOnlyList<CandidateMrProposal> Proposals,
    string? ErrorMessage = null);
