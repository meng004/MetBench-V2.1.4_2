using System;

namespace MetBench_BLL.SystemMT.Catalog.Binding;

/// <summary>
/// Audit metadata attached to a successful <see cref="DiscoveredMrBindingResult"/>.
/// Provides candidate-to-catalog traceability: a maintainer can answer
/// "which discovery method produced this MR, in which run, with what confidence" by
/// reading the provenance alone, without having to re-trace through discovery logs.
/// </summary>
/// <param name="DiscoveryMethod">
/// Discovery technique that produced the candidate (e.g.
/// <c>MetaPattern-Structural</c>, <c>SCG-DirectCause</c>, <c>LLM-MultiVote</c>).
/// </param>
/// <param name="DiscoveryRunId">
/// Stable identifier for the discovery run that generated the candidate; lets the
/// catalog row be cross-referenced against discovery storage.
/// </param>
/// <param name="Confidence">
/// Discovery-reported confidence in the candidate, in <c>[0, 1]</c>.
/// </param>
/// <param name="SutId">SUT id the candidate was bound for (mirrors the draft).</param>
/// <param name="MrId">MR id the candidate was bound for (mirrors the draft).</param>
/// <param name="BoundAtUtc">UTC wall-clock at the moment the binder accepted the draft.</param>
public sealed record DiscoveredMrBindingProvenance(
    string DiscoveryMethod,
    string DiscoveryRunId,
    double Confidence,
    string SutId,
    string MrId,
    DateTimeOffset BoundAtUtc);
