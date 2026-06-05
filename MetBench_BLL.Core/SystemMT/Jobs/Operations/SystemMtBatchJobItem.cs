namespace MetBench_BLL.SystemMT.Jobs;

public enum SystemMtBatchItemState
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Cancelled,
}

/// <summary>Per-MR summary for a batch async job.</summary>
public sealed record SystemMtBatchJobItem(
    string MrId,
    SystemMtBatchItemState State,
    string? FailureReason = null,
    string? RecordId = null);
