using System.Threading.Channels;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 无界 FIFO 队列。单进程内 worker 消费；多进程部署时换 LiteDb/外部队列实现。
/// </summary>
public sealed class ChannelJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(jobId, cancellationToken);

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}
