using LiteDB;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_DAL;

/// <summary>
/// LiteDB-backed repository for <see cref="SystemMtResultRecord"/>. Owns its
/// LiteDatabase handle and the entity-id mapping, so callers can resolve it
/// without knowing about LiteDB attributes. Configures a descending index on
/// <see cref="SystemMtResultRecord.RunAt"/> so the most-recent-first list
/// queries are O(log n).
/// </summary>
public sealed class LiteDbSystemMtResultRepository : ISystemMtResultRepository, IDisposable
{
    private const string CollectionName = "SystemMtResults";
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<SystemMtResultRecord> _collection;
    private readonly bool _ownsDatabase;
    private bool _disposed;

    /// <summary>
    /// Open a repository against a LiteDB file at <paramref name="connectionString"/>.
    /// The repository owns and disposes the database handle.
    /// </summary>
    public LiteDbSystemMtResultRepository(string connectionString)
        : this(new LiteDatabase(connectionString), ownsDatabase: true)
    {
    }

    /// <summary>
    /// Use an existing LiteDB handle (for tests or shared-handle scenarios).
    /// The caller retains ownership.
    /// </summary>
    public LiteDbSystemMtResultRepository(ILiteDatabase database)
        : this(database, ownsDatabase: false)
    {
    }

    private LiteDbSystemMtResultRepository(ILiteDatabase database, bool ownsDatabase)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _ownsDatabase = ownsDatabase;

        // Map the string Id without forcing a [BsonId] attribute on the
        // BLL.Core entity, which would leak a LiteDB dependency upstream.
        _database.Mapper.Entity<SystemMtResultRecord>().Id(x => x.Id);

        _collection = _database.GetCollection<SystemMtResultRecord>(CollectionName);
        _collection.EnsureIndex(x => x.RunAt, unique: false);
        _collection.EnsureIndex(x => x.ScenarioName, unique: false);
    }

    public Task<string> SaveAsync(string scenarioName, SystemMtResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = SystemMtResultRecord.FromResult(scenarioName, result);
        record.Id = ObjectId.NewObjectId().ToString();
        record.RunAt = DateTimeOffset.UtcNow;
        _collection.Insert(record);
        return Task.FromResult(record.Id);
    }

    public Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<SystemMtResultRecord?>(null);
        }

        var record = _collection.FindById(id);
        return Task.FromResult<SystemMtResultRecord?>(record);
    }

    public Task<IReadOnlyList<SystemMtResultRecord>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit must be positive");
        }

        var query = _collection.Query()
            .OrderByDescending(x => x.RunAt)
            .Limit(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<SystemMtResultRecord>>(query);
    }

    public Task<IReadOnlyList<SystemMtResultRecord>> ListByScenarioAsync(string scenarioName, int limit = 100, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(scenarioName))
        {
            throw new ArgumentException("Scenario name is required", nameof(scenarioName));
        }
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit must be positive");
        }

        var query = _collection.Query()
            .Where(x => x.ScenarioName == scenarioName)
            .OrderByDescending(x => x.RunAt)
            .Limit(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<SystemMtResultRecord>>(query);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        if (_ownsDatabase)
        {
            _database.Dispose();
        }
    }
}
