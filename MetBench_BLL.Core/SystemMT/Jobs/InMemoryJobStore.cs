using System.Collections.Concurrent;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 线程安全内存 job store：默认实现 + 测试 double。durable 持久用
/// <c>LiteDbJobStore</c>（DAL 侧）。
/// </summary>
public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<Guid, SystemMtJobRecord> _records = new();
    private readonly ConcurrentDictionary<Guid, MrRunResult> _results = new();

    public Task CreateAsync(SystemMtJobRecord record, CancellationToken cancellationToken)
    {
        if (!_records.TryAdd(record.JobId, record))
            throw new InvalidOperationException($"Job {record.JobId} already exists.");
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(SystemMtJobRecord record, CancellationToken cancellationToken)
    {
        // Fail closed (spec §10 / CLAUDE.md §6): an update to a job that was never
        // CreateAsync'd is a caller bug, not a silent insert. AddOrUpdate's add-factory
        // throws atomically when the key is absent.
        //
        // Terminal-immutable invariant: once a job reaches a terminal state it is final.
        // The update-factory keeps the existing terminal record, so a CancelAsync that races a
        // worker Finalize (or vice versa) cannot silently overwrite the first terminal state with
        // a different one (first-terminal-wins). Atomic via ConcurrentDictionary.AddOrUpdate.
        _records.AddOrUpdate(
            record.JobId,
            _ => throw new InvalidOperationException(
                $"Cannot update unknown job {record.JobId}; it was never created."),
            (_, existing) => existing.State.IsTerminal() ? existing : record);
        return Task.CompletedTask;
    }

    public Task<SystemMtJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken)
        => Task.FromResult(_records.TryGetValue(jobId, out var r) ? r : null);

    public Task SaveResultAsync(Guid jobId, MrRunResult result, CancellationToken cancellationToken)
    {
        _results[jobId] = result;
        return Task.CompletedTask;
    }

    public Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
        => Task.FromResult(_results.TryGetValue(jobId, out var r) ? r : null);
}
