namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// polling 返回的只读快照（设计 spec §7）。来源仅为 <c>IJobStore</c>，不反映 live backend。
/// </summary>
public sealed record SystemMtJobStatus(
    Guid JobId,
    string MrId,
    string SutName,
    SystemMtJobState State,
    string CurrentPhase,
    int ProgressPercent,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? FinishedAtUtc,
    string? FailureReason,
    string? BackendKind = null,
    string? BackendExternalId = null,
    DateTime? LastPolledAtUtc = null,
    string? FailureKind = null);
