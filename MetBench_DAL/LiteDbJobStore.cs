using LiteDB;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_DAL;

/// <summary>
/// LiteDB 持久 <see cref="IJobStore"/>。私有 <see cref="BsonMapper"/> + 独立库文件
/// （默认 SystemMtJobs.Litedb），与 SystemMT.Litedb / MR.Litedb 物理隔离（CLAUDE.md §6）。
/// 拥有并释放 LiteDatabase 句柄；job 记录与运行结果分集合存放，避免 <see cref="MrRunResult"/>
/// schema 漂移污染记录集合。
/// </summary>
public sealed class LiteDbJobStore : IJobStore, IDisposable
{
    private const string RecordsCollection = "JobRecords";
    private const string ResultsCollection = "JobResults";

    private readonly ILiteDatabase _database;
    private readonly bool _ownsDatabase;
    private readonly ILiteCollection<SystemMtJobRecord> _records;
    private readonly ILiteCollection<JobResultEntry> _results;
    private bool _disposed;

    /// <summary>Open against a LiteDB file. The store owns and disposes the handle.</summary>
    public LiteDbJobStore(string connectionString)
        : this(new LiteDatabase(connectionString, BuildMapper()), ownsDatabase: true)
    {
    }

    /// <summary>Use an existing handle (tests / shared-handle). Caller retains ownership.</summary>
    public LiteDbJobStore(ILiteDatabase database)
        : this(database, ownsDatabase: false)
    {
    }

    private LiteDbJobStore(ILiteDatabase database, bool ownsDatabase)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _ownsDatabase = ownsDatabase;

        // Match DbConfig / result repo: deserialize DateTime as Kind=Utc.
        _database.Pragma("UTC_DATE", true);

        _records = _database.GetCollection<SystemMtJobRecord>(RecordsCollection);
        _records.EnsureIndex(x => x.CreatedAtUtc, unique: false);
        _results = _database.GetCollection<JobResultEntry>(ResultsCollection);
    }

    private static BsonMapper BuildMapper()
    {
        // Private mapper so id registration never mutates BsonMapper.Global.
        var mapper = new BsonMapper();
        mapper.Entity<SystemMtJobRecord>().Id(x => x.JobId, autoId: false);
        mapper.Entity<JobResultEntry>().Id(x => x.JobId, autoId: false);
        return mapper;
    }

    public Task CreateAsync(SystemMtJobRecord record, CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        _records.Insert(record);   // throws LiteException on duplicate _id (fail-closed)
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(SystemMtJobRecord record, CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        // Fail closed (spec §10 / §6): Update returns false when _id is absent — never
        // silently insert a ghost record. Mirrors InMemoryJobStore semantics.
        if (!_records.Update(record))
            throw new InvalidOperationException(
                $"Cannot update unknown job {record.JobId}; it was never created.");
        return Task.CompletedTask;
    }

    public Task<SystemMtJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        return Task.FromResult<SystemMtJobRecord?>(_records.FindById(jobId));
    }

    public Task SaveResultAsync(Guid jobId, MrRunResult result, CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        _results.Upsert(new JobResultEntry { JobId = jobId, Result = result });
        return Task.CompletedTask;
    }

    public Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        return Task.FromResult(_results.FindById(jobId)?.Result);
    }

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LiteDbJobStore));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ownsDatabase) _database.Dispose();
    }

    /// <summary>Wrapper so the job result is stored keyed by JobId in its own collection.</summary>
    internal sealed class JobResultEntry
    {
        public Guid JobId { get; set; }
        public MrRunResult Result { get; set; } = null!;
    }
}
