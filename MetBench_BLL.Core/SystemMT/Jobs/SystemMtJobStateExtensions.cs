namespace MetBench_BLL.SystemMT.Jobs;

public static class SystemMtJobStateExtensions
{
    /// <summary>终止态：Succeeded / Failed / TimedOut / Cancelled / ArtifactMissing。</summary>
    public static bool IsTerminal(this SystemMtJobState state) => state switch
    {
        SystemMtJobState.Succeeded => true,
        SystemMtJobState.Failed => true,
        SystemMtJobState.TimedOut => true,
        SystemMtJobState.Cancelled => true,
        SystemMtJobState.ArtifactMissing => true,
        _ => false,
    };
}
