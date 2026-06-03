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
        _records[record.JobId] = record;
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
