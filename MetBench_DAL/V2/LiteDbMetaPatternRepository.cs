using System.Collections.ObjectModel;
using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_DAL.V2;

/// <summary>
/// LiteDB Repository —— MetaPattern (string PK = Code)。
/// </summary>
/// <remarks>
/// MetaPattern 用 string PK 与 v2 其他 int/Guid PK 实体不同，所以不复用 base class，
/// 直接实现。Code 唯一性靠 LiteDB <c>_id</c> 自然约束。
/// </remarks>
public sealed class LiteDbMetaPatternRepository : IMetaPatternRepository
{
    private readonly string _conn;
    private const string CollectionKey = "MetaPatterns";

    public LiteDbMetaPatternRepository()
    {
        _conn = DbConfig.Instance._conn;
    }

    /// <summary>测试用构造器：直接注入连接字符串，绕开 <see cref="DbConfig.Instance"/>（避开 cold-start C5）。</summary>
    public LiteDbMetaPatternRepository(string conn)
    {
        _conn = conn;
    }

    public ObservableCollection<MetaPattern> GetAll()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetaPattern>(CollectionKey);
        return new ObservableCollection<MetaPattern>(col.FindAll());
    }

    public MetaPattern? Get(string code)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetaPattern>(CollectionKey);
        return col.FindById(code);
    }

    public ObservableCollection<MetaPattern> GetByStatus(string status)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetaPattern>(CollectionKey);
        return new ObservableCollection<MetaPattern>(col.Find(p => p.Status == status));
    }

    public ObservableCollection<MetaPattern> GetActive()
        => GetByStatus("active");

    public ObservableCollection<MetaPattern> GetByBlock(string block)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetaPattern>(CollectionKey);
        return new ObservableCollection<MetaPattern>(col.Find(p => p.Block == block));
    }

    public bool Add(MetaPattern entity)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetaPattern>(CollectionKey);
        col.EnsureIndex(p => p.Status);
        col.EnsureIndex(p => p.Block);
        col.Insert(entity);  // duplicate Code → LiteDB throws
        return true;
    }

    public bool Modify(MetaPattern entity)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetaPattern>(CollectionKey);
        return col.Update(entity);
    }

    public bool Remove(MetaPattern entity)
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetaPattern>(CollectionKey);
        return col.Delete(entity.Code);
    }

    public int Count()
    {
        using var db = new LiteDatabase(_conn);
        var col = db.GetCollection<MetaPattern>(CollectionKey);
        return col.Count();
    }
}
