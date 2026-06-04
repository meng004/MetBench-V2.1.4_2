namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// durable job 记录（设计 spec §5）。job store 落这一行；polling 从它投影
/// <see cref="SystemMtJobStatus"/>。
/// </summary>
public sealed record SystemMtJobRecord
{
    public Guid JobId { get; init; }
    public string MrId { get; init; } = string.Empty;
    public string SutName { get; init; } = string.Empty;
    public SystemMtJobState State { get; init; } = SystemMtJobState.Queued;
    public int ProgressPercent { get; init; }
    public string CurrentPhase { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public string? FailureKind { get; init; }
    public string? BackendKind { get; init; }
    public string? BackendExternalId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? FinishedAtUtc { get; init; }
    public DateTime? LastPolledAtUtc { get; init; }

    /// <summary>投影成 polling 快照。</summary>
    public SystemMtJobStatus ToStatus() => new(
        JobId, MrId, SutName, State, CurrentPhase, ProgressPercent,
        CreatedAtUtc, UpdatedAtUtc, FinishedAtUtc, FailureReason,
        BackendKind, BackendExternalId, LastPolledAtUtc, FailureKind);
}
