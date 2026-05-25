using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Typed.Runtime;
using MetBench_BLL.SystemMT.Catalog.Typed.Specs;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// Pipeline 跑完一次的结果摘要（Service 层用这个写 LiteDB Execution + Result + Anomaly）。
/// </summary>
public sealed record PipelineOutcome(
    string FinalStatus,                              // Ok / Anomaly / Error / Timeout
    string? ErrorMessage,
    DateTime StartedAt,
    DateTime FinishedAt,
    string ArtifactsDirectory,                       // 路径，含源/后继输入输出
    string SourceInputPath,
    string FollowupInputPath,
    string SourceOutputPath,
    string FollowupOutputPath,
    IReadOnlyDictionary<string, double>? SourceMetrics,
    IReadOnlyDictionary<string, double>? FollowupMetrics,
    SystemMtAssertionResultV2? AssertionResult,
    TimeSpan SourceElapsed,
    TimeSpan FollowupElapsed,
    int SourceExitCode,
    int FollowupExitCode)
{
    /// <summary>
    /// Typed Semantic Catalog spec used at runtime, captured for downstream
    /// evidence projection. PR-123 wires `SystemMtPipeline.ExecuteAsync` to
    /// populate this so `SystemMtExecutionRecorder.Record` can project the
    /// typed verification into `ExecutionEvidence.TypedVerification` without
    /// each caller re-passing the triple. Init-only so the positional ctor
    /// surface stays unchanged.
    /// </summary>
    public MrSpec? TypedSpec { get; init; }

    /// <summary>Typed predicate dispatched against <see cref="TypedSpec"/>; null when the typed path was not taken.</summary>
    public PredicateSpec? TypedPredicate { get; init; }

    /// <summary>Typed verifier result for <see cref="TypedPredicate"/>; null when the typed path was not taken.</summary>
    public VerificationResult? TypedVerification { get; init; }
}
