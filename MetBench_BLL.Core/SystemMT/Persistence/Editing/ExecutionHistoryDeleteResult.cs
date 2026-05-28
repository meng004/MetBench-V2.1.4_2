namespace MetBench_BLL.SystemMT.Persistence.Editing;

/// <summary>
/// Three-segment counter returned by <see cref="IExecutionHistoryEditor.DeleteBatchAsync"/>.
/// LiteDB v5 has no cross-collection transaction, so a successful Evidence
/// delete followed by a failed Result delete leaves an "evidence-only" orphan
/// that needs visibility in the UI rather than being silently lost.
/// </summary>
public sealed record ExecutionHistoryDeleteResult(
    int Deleted,
    int EvidenceOnly,
    int Failed);
