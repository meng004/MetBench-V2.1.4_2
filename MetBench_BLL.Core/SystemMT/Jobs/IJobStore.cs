using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>job 持久契约。polling 只读这里；后台 worker 只写这里。</summary>
public interface IJobStore
{
    Task CreateAsync(SystemMtJobRecord record, CancellationToken cancellationToken);
    Task UpdateStatusAsync(SystemMtJobRecord record, CancellationToken cancellationToken);
    Task<SystemMtJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken);
    Task SaveResultAsync(Guid jobId, MrRunResult result, CancellationToken cancellationToken);
    Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken);
}
