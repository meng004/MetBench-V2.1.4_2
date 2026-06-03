using System.Collections.Concurrent;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// Thread-safe in-process <see cref="IJobCancellationRegistry"/>. Register as a singleton so the
/// job service (enqueue/cancel side) and the worker (execution side) share one instance.
/// </summary>
public sealed class JobCancellationRegistry : IJobCancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _sources = new();

    public CancellationToken Register(Guid jobId)
    {
        var cts = new CancellationTokenSource();
        // Replace any stale source for the same id (a fresh worker run owns the token).
        _sources.AddOrUpdate(jobId, cts, (_, old) =>
        {
            old.Dispose();
            return cts;
        });
        return cts.Token;
    }

    public void Cancel(Guid jobId)
    {
        if (_sources.TryGetValue(jobId, out var cts))
        {
            try { cts.Cancel(); }
            catch (ObjectDisposedException) { /* released concurrently — nothing to cancel */ }
        }
    }

    public void Release(Guid jobId)
    {
        if (_sources.TryRemove(jobId, out var cts))
            cts.Dispose();
    }
}
