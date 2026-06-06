using LiteDB;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

/// <summary>
/// LiteDB Repository 通用基类（int PK 实体）。
/// 模仿 v1 风格 — 每次操作 open/dispose LiteDatabase；线程安全；
/// 复用 <see cref="DbConfig.Instance"/> 的连接字符串。
/// </summary>
/// <typeparam name="T">实体类型，必须有 [BsonId] 标注的 int PK 字段。</typeparam>
public abstract class LiteDbIntPkRepositoryBase<T> where T : class
{
    protected readonly DbConfig _dbConfig;
    protected readonly string _conn;

    protected LiteDbIntPkRepositoryBase()
    {
        _dbConfig = DbConfig.Instance;
        _conn = _dbConfig._conn;
    }

    /// <summary>
    /// 测试用构造器：直接注入连接字符串 + DbConfig 实例。
    /// 生产代码不应该使用此构造器。
    /// </summary>
    protected LiteDbIntPkRepositoryBase(DbConfig dbConfig, string conn)
    {
        _dbConfig = dbConfig;
        _conn = conn;
    }

    /// <summary>子类指定 collection key（DbConfig 暴露的 *_Key 常量）。</summary>
    protected abstract string CollectionKey { get; }

    public virtual ObservableCollection<T> GetAll()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        return new ObservableCollection<T>(col.FindAll());
    }

    /// <summary>
    /// Legacy <see cref="IRepository{T}.Get(int)"/> contract returns non-nullable T:
    /// callers (BLL legacy services + BLL.Core ReplayContextBuilder) assume the row
    /// exists and access it directly. LiteDB <c>FindById</c> can return null at
    /// runtime (deleted row, stale id) — that is treated as a caller bug and surfaces
    /// as an NPE at the consumer, same as before this nullable annotation. Tightening
    /// the contract to <c>T?</c> + <see cref="KeyNotFoundException"/> on miss is a
    /// follow-up tracked by the maturity remediation plan.
    /// </summary>
    public virtual T Get(int id)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        return col.FindById(id)!;
    }

    public virtual ObservableCollection<T> Get(T template)
    {
        // 默认实现：调用方应在子类 override 以实现条件查询。
        // 基础行为返回全部（避免误用）。
        return GetAll();
    }

    public virtual bool Add(T entity)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        var result = col.Insert(entity);
        return result != null;
    }

    public virtual bool Modify(T entity)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        return col.Update(entity);
    }

    public virtual bool Remove(T entity)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        var id = col.GetBsonDocumentId(entity);
        return col.Delete(id);
    }
}

/// <summary>
/// LiteDB extension to extract a BsonValue id from an entity via mapper.
/// </summary>
internal static class LiteCollectionExtensions
{
    public static BsonValue GetBsonDocumentId<T>(this ILiteCollection<T> col, T entity)
        where T : class
    {
        var mapper = BsonMapper.Global;
        var bson = mapper.ToDocument(entity);
        return bson["_id"];
    }
}
