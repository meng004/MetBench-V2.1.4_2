using LiteDB;
using MetBench_BLL.Paging;
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
    /// The repository owns and disposes the database handle. Uses a private
    /// <see cref="BsonMapper"/> instance so the entity-id registration does not
    /// mutate <see cref="BsonMapper.Global"/> for unrelated callers.
    /// </summary>
    public LiteDbSystemMtResultRepository(string connectionString)
        : this(new LiteDatabase(connectionString, new BsonMapper()), ownsDatabase: true)
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

        // UTC_DATE: deserialize DateTime/DateTimeOffset as Kind=Utc, not Local.
        // Pragma is persisted in the file header on first set; matches
        // DbConfig's setting so SystemMt.Litedb and MR.Litedb stay aligned.
        _database.Pragma("UTC_DATE", true);

        // Map the Guid Id without forcing a [BsonId] attribute on the
        // BLL.Core entity, which would leak a LiteDB dependency upstream.
        // autoId=true lets LiteDB assign a fresh Guid on Insert when the
        // field is Guid.Empty (matches every other v2 entity in this repo).
        _database.Mapper.Entity<SystemMtResultRecord>().Id(x => x.Id, autoId: true);

        // v2.2 schema migration: BSON field "ScenarioName" → "MrName".
        // Older .Litedb files written by v2.1.x have "ScenarioName"; renaming
        // the C# property without migrating the field would lose the column.
        // Idempotent — second call is a no-op (no docs with the old field).
        MigrateScenarioNameFieldToMrName(_database, CollectionName);

        // v2.1.0 schema migration: legacy rows persisted by pre-Guid code
        // (PR #75 era) have a string-typed _id holding a 24-hex BSON ObjectId.
        // The typed mapper now expects a Guid _id; without rewriting these
        // rows would fail to read and would break round-trips through
        // AnomalyService (which Guid.TryParse-s the resultId). Idempotent.
        MigrateObjectIdStringToGuid(_database, CollectionName);

        _collection = _database.GetCollection<SystemMtResultRecord>(CollectionName);
        _collection.EnsureIndex(x => x.RunAt, unique: false);
        _collection.EnsureIndex(x => x.MrName, unique: false);
    }

    /// <summary>
    /// One-shot schema migration: rename BSON field <c>ScenarioName</c> to
    /// <c>MrName</c> on every persisted record that still has the old name.
    /// Drops the old field, copies value to the new field, and removes the
    /// stale index. Idempotent: second invocation is O(1) when no old-field
    /// docs remain. Migration is performed via raw <see cref="BsonDocument"/>
    /// to avoid the typed mapper rejecting unknown fields on the first read.
    /// </summary>
    /// <remarks>
    /// Why migrate (vs. dual-name <c>[BsonField]</c> aliasing):
    /// preserving a hidden "ScenarioName" alias would keep two divergent
    /// names in play for v2.2 readers and writers. Migration runs once on
    /// repo open and the schema is clean afterwards. The cost is one
    /// linear scan + N small updates per stale doc on first upgrade.
    /// </remarks>
    private static void MigrateScenarioNameFieldToMrName(ILiteDatabase database, string collectionName)
    {
        const string OldField = "ScenarioName";
        const string NewField = "MrName";

        // Raw (untyped) collection so we can see BsonDocuments containing the
        // old field name without the typed mapper hiding it.
        var raw = database.GetCollection(collectionName);

        // Find every doc with the legacy field. LiteDB v5 has no "field
        // exists" query op, but unset fields equal BsonValue.Null when
        // selected; we ToList() and filter client-side. The dataset is small
        // (review history; thousands not millions) so this is safe.
        var stale = raw.FindAll()
            .Where(d => d.ContainsKey(OldField))
            .ToList();
        if (stale.Count == 0)
        {
            return;
        }

        foreach (var doc in stale)
        {
            if (!doc.ContainsKey(NewField) || doc[NewField].IsNull)
            {
                doc[NewField] = doc[OldField];
            }
            doc.Remove(OldField);
            raw.Update(doc);
        }

        // Stale index on the old field name still occupies disk + slows
        // writes. DropIndex is a no-op if it does not exist (older LiteDB
        // versions may swallow the exception; we tolerate either path).
        try { raw.DropIndex(OldField); } catch { /* index absent, fine */ }
    }

    /// <summary>
    /// One-shot migration: rewrite legacy rows whose <c>_id</c> is a BSON
    /// string (24-hex ObjectId from the pre-Guid era) into rows keyed by a
    /// fresh Guid. Performed by re-inserting under the new key and removing
    /// the old document, since LiteDB does not allow updating <c>_id</c>.
    /// </summary>
    private static void MigrateObjectIdStringToGuid(ILiteDatabase database, string collectionName)
    {
        var raw = database.GetCollection(collectionName);
        var stale = raw.FindAll()
            .Where(d => d.ContainsKey("_id") && d["_id"].IsString)
            .ToList();
        if (stale.Count == 0)
        {
            return;
        }

        foreach (var doc in stale)
        {
            var oldId = doc["_id"];
            doc["_id"] = new BsonValue(Guid.NewGuid());
            raw.Insert(doc);
            raw.Delete(oldId);
        }
    }

    public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = SystemMtResultRecord.FromResult(mrName, result);
        record.RunAt = DateTimeOffset.UtcNow;
        // record.Id stays Guid.Empty here — LiteDB autoId fills a fresh Guid
        // on Insert and writes it back onto the record before returning.
        _collection.Insert(record);
        return Task.FromResult(record.Id.ToString());
    }

    public Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(id, out var guid))
        {
            return Task.FromResult<SystemMtResultRecord?>(null);
        }

        var record = _collection.FindById(guid);
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

    public Task<IReadOnlyList<SystemMtResultRecord>> ListByMrNameAsync(string mrName, int limit = 100, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(mrName))
        {
            throw new ArgumentException("MR name is required", nameof(mrName));
        }
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit must be positive");
        }

        var query = _collection.Query()
            .Where(x => x.MrName == mrName)
            .OrderByDescending(x => x.RunAt)
            .Limit(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<SystemMtResultRecord>>(query);
    }

    public Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null) throw new ArgumentNullException(nameof(request));
        request.Validate();

        var totalCount = _collection.Count();
        if (totalCount == 0 || request.Skip >= totalCount)
        {
            return Task.FromResult(PagedResult<SystemMtResultRecord>.Empty(request.PageIndex, request.PageSize) with { TotalCount = totalCount });
        }

        var items = _collection.Query()
            .OrderByDescending(x => x.RunAt)
            .Skip(request.Skip)
            .Limit(request.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<SystemMtResultRecord>(
            items, totalCount, request.PageIndex, request.PageSize));
    }

    public Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(mrName))
        {
            throw new ArgumentException("MR name is required", nameof(mrName));
        }
        if (request is null) throw new ArgumentNullException(nameof(request));
        request.Validate();

        var totalCount = _collection.Count(x => x.MrName == mrName);
        if (totalCount == 0 || request.Skip >= totalCount)
        {
            return Task.FromResult(PagedResult<SystemMtResultRecord>.Empty(request.PageIndex, request.PageSize) with { TotalCount = totalCount });
        }

        var items = _collection.Query()
            .Where(x => x.MrName == mrName)
            .OrderByDescending(x => x.RunAt)
            .Skip(request.Skip)
            .Limit(request.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<SystemMtResultRecord>(
            items, totalCount, request.PageIndex, request.PageSize));
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
