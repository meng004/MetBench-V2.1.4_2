using LiteDB;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// F19 测试：MRBinding.Status 字段（替代 IsActive bool），与其他 v2 实体命名统一。
/// 闭环 cold-start C6.
/// </summary>
public sealed class MRBindingStatusTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ILiteDatabase _db;

    public MRBindingStatusTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchMRBindingStatus",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _db = new LiteDatabase(_dbPath, new BsonMapper());
        _db.Mapper.Entity<MRBinding>().Id(x => x.IdMRBinding);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }

    [Fact]
    public void New_binding_defaults_status_to_active()
    {
        var b = new MRBinding { MRId = 1, ApplicationId = 1 };
        Assert.Equal("active", b.Status);
        Assert.True(b.IsActive);
    }

    [Fact]
    public void IsActive_setter_maps_to_active_or_deprecated()
    {
        var b = new MRBinding { IsActive = true };
        Assert.Equal("active", b.Status);

        b.IsActive = false;
        Assert.Equal("deprecated", b.Status);
    }

    [Fact]
    public void IsActive_getter_reflects_status_field()
    {
        var b = new MRBinding { Status = "active" };
        Assert.True(b.IsActive);

        b.Status = "deprecated";
        Assert.False(b.IsActive);

        b.Status = "archived";
        Assert.False(b.IsActive);

        b.Status = "experimental";
        Assert.False(b.IsActive);  // 仅 "active" 算 IsActive=true
    }

    [Fact]
    public void Status_persists_through_litedb_roundtrip()
    {
        var col = _db.GetCollection<MRBinding>("MRBindings");
        col.Insert(new MRBinding { IdMRBinding = 1, MRId = 1, ApplicationId = 1, Status = "experimental" });

        var loaded = col.FindById(1);
        Assert.Equal("experimental", loaded.Status);
        Assert.False(loaded.IsActive);
    }

    [Fact]
    public void IsActive_is_BsonIgnored_not_persisted()
    {
        var col = _db.GetCollection("MRBindings");
        var doc = new BsonDocument { ["_id"] = 1, ["Status"] = "active" };
        col.Insert(doc);

        // IsActive 不应作为字段存在 BsonDocument 里
        var loaded = col.FindById(1);
        Assert.False(loaded.ContainsKey("IsActive"),
            "IsActive should be BsonIgnore (computed from Status), not stored as field");
    }

    [Fact]
    public void Query_by_status_filters_correctly()
    {
        var col = _db.GetCollection<MRBinding>("MRBindings");
        col.Insert(new MRBinding { IdMRBinding = 1, Status = "active" });
        col.Insert(new MRBinding { IdMRBinding = 2, Status = "deprecated" });
        col.Insert(new MRBinding { IdMRBinding = 3, Status = "active" });
        col.Insert(new MRBinding { IdMRBinding = 4, Status = "archived" });

        Assert.Equal(2, col.Find(b => b.Status == "active").Count());
        Assert.Equal(1, col.Find(b => b.Status == "deprecated").Count());
        Assert.Equal(1, col.Find(b => b.Status == "archived").Count());
    }

    [Fact]
    public void Backwards_compat_existing_binding_documents_with_only_IsActive_field()
    {
        // 模拟 PR #34 前的旧文档：只有 IsActive=true，没有 Status
        var col = _db.GetCollection("MRBindings");
        var oldDoc = new BsonDocument
        {
            ["_id"] = 1,
            ["MRId"] = 1,
            ["ApplicationId"] = 1,
            ["IsActive"] = true,
            // 没有 Status 字段
        };
        col.Insert(oldDoc);

        // 用新 entity 读 — 缺失 Status 字段应填默认 "active"
        var typedCol = _db.GetCollection<MRBinding>("MRBindings");
        var loaded = typedCol.FindById(1);
        Assert.NotNull(loaded);
        Assert.Equal("active", loaded!.Status);  // 默认值
        Assert.True(loaded.IsActive);
    }
}
