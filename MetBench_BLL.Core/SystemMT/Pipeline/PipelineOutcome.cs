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
    /// populate this so `SystemMtExecutionRecorder.RecordAsync` can project the
    /// typed verification into `ExecutionEvidence.TypedVerification` without
    /// each caller re-passing the triple. Init-only so the positional ctor
    /// surface stays unchanged.
    /// </summary>
    public MrSpec? TypedSpec { get; init; }

    /// <summary>Typed predicate dispatched against <see cref="TypedSpec"/>; null when the typed path was not taken.</summary>
    public PredicateSpec? TypedPredicate { get; init; }

    /// <summary>Typed verifier result for <see cref="TypedPredicate"/>; null when the typed path was not taken.</summary>
    public VerificationResult? TypedVerification { get; init; }

    /// <summary>
    /// PR-Bol-2A: per-phase metrics dict (role → metric snapshot) when
    /// <see cref="SystemMtPipeline.ExecuteMultiPhaseAsync"/> was used; null for the
    /// single-source/followup path. The display-compat fields <see cref="SourceMetrics"/>
    /// + <see cref="FollowupMetrics"/> are populated from the first / last phase
    /// respectively so the existing report renderer + recorder still see something
    /// meaningful for multi-phase runs.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>? PhaseMetrics { get; init; }

    public string SourceRuntimeRunId { get; init; } = string.Empty;

    public string FollowupRuntimeRunId { get; init; } = string.Empty;
}
