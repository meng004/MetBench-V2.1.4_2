using LiteDB;
using MetBench_BLL.SystemMT.Metadata;

namespace MetBench_DAL;

/// <summary>
/// LiteDB-backed repository for System-MT reference metadata
/// (<see cref="EquationMetadata"/> and <see cref="MrMetadata"/>). Owns its
/// LiteDatabase handle and a private <see cref="BsonMapper"/> so the entity-id
/// registration never mutates <see cref="BsonMapper.Global"/>. Unique indexes
/// on the business keys make upsert (find-by-key then insert-or-update) safe.
/// </summary>
public sealed class LiteDbSystemMtMetadataRepository : ISystemMtMetadataRepository, IDisposable
{
    private const string EquationCollection = "EquationMetadata";
    private const string MrCollection = "MrMetadata";
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<EquationMetadata> _equations;
    private readonly ILiteCollection<MrMetadata> _mrs;
    private readonly bool _ownsDatabase;
    private bool _disposed;

    /// <summary>
    /// Open a repository against a LiteDB file at <paramref name="connectionString"/>.
    /// The repository owns and disposes the database handle.
    /// </summary>
    public LiteDbSystemMtMetadataRepository(string connectionString)
        : this(new LiteDatabase(connectionString, new BsonMapper()), ownsDatabase: true)
    {
    }

    /// <summary>
    /// Use an existing LiteDB handle (for tests or shared-handle scenarios).
    /// The caller retains ownership.
    /// </summary>
    public LiteDbSystemMtMetadataRepository(ILiteDatabase database)
        : this(database, ownsDatabase: false)
    {
    }

    private LiteDbSystemMtMetadataRepository(ILiteDatabase database, bool ownsDatabase)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _ownsDatabase = ownsDatabase;

        _database.Mapper.Entity<EquationMetadata>().Id(x => x.Id, autoId: true);
        _database.Mapper.Entity<MrMetadata>().Id(x => x.Id, autoId: true);

        _equations = _database.GetCollection<EquationMetadata>(EquationCollection);
        _equations.EnsureIndex(x => x.EquationKey, unique: true);

        _mrs = _database.GetCollection<MrMetadata>(MrCollection);
        _mrs.EnsureIndex(x => x.MrId, unique: true);
        _mrs.EnsureIndex(x => x.EquationKey, unique: false);
    }

    public Task UpsertEquationAsync(EquationMetadata equation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (equation is null)
        {
            throw new ArgumentNullException(nameof(equation));
        }
        if (string.IsNullOrWhiteSpace(equation.EquationKey))
        {
            throw new ArgumentException("EquationKey is required", nameof(equation));
        }

        var existing = _equations.FindOne(x => x.EquationKey == equation.EquationKey);
        if (existing is null)
        {
            _equations.Insert(equation);
        }
        else
        {
            equation.Id = existing.Id;
            _equations.Update(equation);
        }
        return Task.CompletedTask;
    }

    public Task<EquationMetadata?> GetEquationAsync(string equationKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(equationKey))
        {
            return Task.FromResult<EquationMetadata?>(null);
        }

        var found = _equations.FindOne(x => x.EquationKey == equationKey);
        return Task.FromResult<EquationMetadata?>(found);
    }

    public Task<IReadOnlyList<EquationMetadata>> ListEquationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var all = _equations.Query()
            .OrderBy(x => x.EquationKey)
            .ToList();
        return Task.FromResult<IReadOnlyList<EquationMetadata>>(all);
    }

    public Task UpsertMrAsync(MrMetadata mr, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (mr is null)
        {
            throw new ArgumentNullException(nameof(mr));
        }
        if (string.IsNullOrWhiteSpace(mr.MrId))
        {
            throw new ArgumentException("MrId is required", nameof(mr));
        }

        var existing = _mrs.FindOne(x => x.MrId == mr.MrId);
        if (existing is null)
        {
            _mrs.Insert(mr);
        }
        else
        {
            mr.Id = existing.Id;
            _mrs.Update(mr);
        }
        return Task.CompletedTask;
    }

    public Task<MrMetadata?> GetMrAsync(string mrId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(mrId))
        {
            return Task.FromResult<MrMetadata?>(null);
        }

        var found = _mrs.FindOne(x => x.MrId == mrId);
        return Task.FromResult<MrMetadata?>(found);
    }

    public Task<IReadOnlyList<MrMetadata>> ListMrsByEquationAsync(string equationKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(equationKey))
        {
            return Task.FromResult<IReadOnlyList<MrMetadata>>(Array.Empty<MrMetadata>());
        }

        var matches = _mrs.Query()
            .Where(x => x.EquationKey == equationKey)
            .OrderBy(x => x.MrId)
            .ToList();
        return Task.FromResult<IReadOnlyList<MrMetadata>>(matches);
    }

    public Task<IReadOnlyList<MrMetadata>> ListMrsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var all = _mrs.Query()
            .OrderBy(x => x.MrId)
            .ToList();
        return Task.FromResult<IReadOnlyList<MrMetadata>>(all);
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
