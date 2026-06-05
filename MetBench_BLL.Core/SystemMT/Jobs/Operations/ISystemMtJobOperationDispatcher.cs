namespace MetBench_BLL.SystemMT.Jobs;

public interface ISystemMtJobOperationDispatcher
{
    Task<JobExecutionOutcome> ExecuteAsync(
        Guid jobId,
        SystemMtJobRecord record,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken);
}
