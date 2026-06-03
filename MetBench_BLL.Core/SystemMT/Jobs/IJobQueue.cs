namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>job 入队 / 取队。worker 在 <see cref="DequeueAsync"/> 上阻塞等下一个 job。</summary>
public interface IJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
