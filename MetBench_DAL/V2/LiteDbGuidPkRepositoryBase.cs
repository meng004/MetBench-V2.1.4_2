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

    public virtual int Count()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<T>(CollectionKey);
        return col.Count();
    }
}
