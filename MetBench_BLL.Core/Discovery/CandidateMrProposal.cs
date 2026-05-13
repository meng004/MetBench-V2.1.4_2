namespace MetBench_BLL.Discovery;

/// <summary>
/// Discoverer 在还未写入 LiteDB 之前的纯 DTO —— 一个 MR 候选提议。
/// </summary>
/// <remarks>
/// 不带 <c>IdCandidate</c> / <c>DiscoveryRunId</c>。<see cref="DiscoveryService"/>
/// 负责为每条 proposal 分配 Guid 并和 DiscoveryRun 绑定后落库。
/// </remarks>
public sealed record CandidateMrProposal(
    string ProposedCode,
    string ProposedName,
    string? SuggestedTransformationName,
    string? SuggestedAssertionTypeCode,
    string ProposedValueName,
    string Rationale,
    double Confidence);
