using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class ChannelJobQueueTests
{
    [Fact]
    public async Task Enqueued_id_is_dequeued_in_fifo_order()
    {
        var queue = new ChannelJobQueue();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await queue.EnqueueAsync(a, default);
        await queue.EnqueueAsync(b, default);

        Assert.Equal(a, await queue.DequeueAsync(default));
        Assert.Equal(b, await queue.DequeueAsync(default));
    }

    [Fact]
    public async Task DequeueAsync_honors_cancellation_when_empty()
    {
        var queue = new ChannelJobQueue();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await queue.DequeueAsync(cts.Token));
    }
}
