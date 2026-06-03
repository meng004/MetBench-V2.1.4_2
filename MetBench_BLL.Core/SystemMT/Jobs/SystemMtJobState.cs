namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>System MT 异步作业状态机（设计 spec §6）。非终止态可转入任一终止态。</summary>
public enum SystemMtJobState
{
    Queued,
    Preparing,
    RunningSource,
    RunningFollowup,
    ParsingOutputs,
    Asserting,
    Succeeded,
    Failed,
    TimedOut,
    Cancelled,
    ArtifactMissing,
}
