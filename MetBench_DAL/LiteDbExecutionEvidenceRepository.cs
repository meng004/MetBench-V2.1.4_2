using LiteDB;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_DAL;

/// <summary>
/// LiteDB-backed repository for <see cref="ExecutionEvidence"/>. Stores sample-level
/// evidence in its own collection (separate from <c>SystemMtResultRecord</c> — the
/// summary table). Index on <see cref="ExecutionEvidence.ExecutionId"/> so
/// <c>GetByExecutionAsync</c> is O(log n).
/// </summary>
/// <remarks>
/// Owns its <see cref="LiteDatabase"/> when constructed from a connection string;
/// when handed an existing <see cref="ILiteDatabase"/>, the caller retains ownership
/// (mirrors <see cref="LiteDbSystemMtResultRepository"/> ctor pair).
/// </remarks>
public sealed class LiteDbExecutionEvidenceRepository : IExecutionEvidenceRepository, IDisposable
{
    private const string CollectionName = "ExecutionEvidence";

    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<ExecutionEvidence> _collection;
    private readonly bool _ownsDatabase;
    private bool _disposed;

    public LiteDbExecutionEvidenceRepository(string connectionString)
        : this(new LiteDatabase(connectionString, new BsonMapper()), ownsDatabase: true)
    {
    }

    public LiteDbExecutionEvidenceRepository(ILiteDatabase database)
        : this(database, ownsDatabase: false)
    {
    }

    private LiteDbExecutionEvidenceRepository(ILiteDatabase database, bool ownsDatabase)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _ownsDatabase = ownsDatabase;

        _database.Pragma("UTC_DATE", true);
        _database.Mapper.Entity<ExecutionEvidence>().Id(x => x.IdEvidence, autoId: true);

        _collection = _database.GetCollection<ExecutionEvidence>(CollectionName);
        _collection.EnsureIndex(x => x.ExecutionId, unique: true);
    }

    public Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        cancellationToken.ThrowIfCancellationRequested();
        _collection.Upsert(evidence);
        return Task.CompletedTask;
    }

    public Task<ExecutionEvidence?> GetByExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var match = _collection.FindOne(e => e.ExecutionId == executionId);
        return Task.FromResult<ExecutionEvidence?>(match);
    }

    public Task<bool> DeleteByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // The ExecutionId index is unique (line 45), so at most one row matches.
        // DeleteMany returns the count of removed rows; > 0 means we actually deleted.
        var removed = _collection.DeleteMany(e => e.ExecutionId == executionId);
        return Task.FromResult(removed > 0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_ownsDatabase) _database.Dispose();
        _disposed = true;
    }
}
