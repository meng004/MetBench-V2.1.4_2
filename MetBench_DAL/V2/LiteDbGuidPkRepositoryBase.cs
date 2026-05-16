using LiteDB;
using System.Collections.ObjectModel;

namespace MetBench_DAL.V2;

/// <summary>
/// LiteDB Repository 通用基类（Guid PK 实体）。
/// 用于 v2 高频生成实体：Execution / Result / Anomaly / ...
/// </summary>
/// <typeparam name="T">实体类型，必须有 [BsonId] 标注的 Guid PK 字段。</typeparam>
public abstract class LiteDbGuidPkRepositoryBase<T> where T : class
{
    protected readonly DbConfig _dbConfig;
    protected readonly string _conn;

    protected LiteDbGuidPkRepositoryBase()
    {
        _dbConfig = DbConfig.Instance;
        _conn = _dbConfig._conn;
    }

    /// <summary>
    /// 测试用构造器：直接注入连接字符串 + DbConfig 实例。
    /// 生产代码不应该使用此构造器。
    /// </summary>
    protected LiteDbGuidPkRepositoryBase(DbConfig dbConfig, string conn)
    {
        _dbConfig = dbConfig;
        _conn = conn;
    }

    /// <summary>子类指定 collection key。</summary>
    protected abstract string CollectionKey { get; }

    public virtual ObservableCollection<T> GetAll()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        return new ObservableCollection<T>(col.FindAll());
    }

    public virtual T? Get(Guid id)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        return col.FindById(id);
    }

    public virtual ObservableCollection<T> Get(T template)
    {
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

    /// <remarks>
    /// LiteDB 的 <c>Skip(N)</c> 是 in-memory 线性扫描而非索引偏移：每页都从头跳 N 行。
    /// 集合 ≤ 10k 行时可忽略；超大集合（Executions / MutationResults）做深翻页前应改成
    /// <see cref="GetPageKeyset"/>（按 IdXxx &gt; lastSeenId）。
    /// </remarks>
    public virtual ObservableCollection<T> GetPage(int pageIndex, int pageSize)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        var query = col.Query()
            .Skip(pageIndex * pageSize)
            .Limit(pageSize)
            .ToList();
        return new ObservableCollection<T>(query);
    }

    /// <inheritdoc cref="IGuidRepository{T}.GetPageKeyset"/>
    /// <remarks>
    /// 用 PK (<c>_id</c>) 索引做 keyset 过滤，O(pageSize) 复杂度。
    /// LiteDB 的 <c>_id</c> 索引是自动创建且唯一。
    /// </remarks>
    public virtual ObservableCollection<T> GetPageKeyset(Guid? afterId, int pageSize)
    {
        if (pageSize <= 0) return new ObservableCollection<T>();
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        var query = col.Query();
        if (afterId.HasValue)
            query = query.Where("_id > @0", new BsonValue(afterId.Value));
        var rows = query.OrderBy("_id").Limit(pageSize).ToList();
        return new ObservableCollection<T>(rows);
    }

    public virtual int Count()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        return col.Count();
    }
}
