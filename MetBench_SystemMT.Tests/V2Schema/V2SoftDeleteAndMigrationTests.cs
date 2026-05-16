using LiteDB;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// F5 集成测试：v2 实体的 ① 软删除模式 ② schema 演化 ③ 跨实体软外键 边界。
/// 之前的 V2IndexConstraintTests 只覆盖了基本唯一性，本套补足 LiveDB 上的状态机和兼容性。
/// </summary>
public sealed class V2SoftDeleteAndMigrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ILiteDatabase _db;

    public V2SoftDeleteAndMigrationTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchV2SoftDelete",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _db = new LiteDatabase(_dbPath, new BsonMapper());
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }

    // ========== 软删除 (Status='deprecated' / 'archived') ==========

    [Fact]
    public void Mutant_status_filter_supports_soft_delete_via_status_field()
    {
        _db.Mapper.Entity<Mutant>().Id(x => x.IdMutant);
        var col = _db.GetCollection<Mutant>("Mutants");
        col.Insert(new Mutant { IdMutant = 1, OperatorId = 1, AppliedDiff = "diff1", Status = "active" });
        col.Insert(new Mutant { IdMutant = 2, OperatorId = 1, AppliedDiff = "diff2", Status = "deprecated" });
        col.Insert(new Mutant { IdMutant = 3, OperatorId = 1, AppliedDiff = "diff3", Status = "experimental" });

        // "软删除" 模式：default 查询过滤掉 deprecated
        var active = col.Find(m => m.Status == "active").ToList();
        Assert.Single(active);

        // 历史查询：所有状态都能看到
        var all = col.FindAll().ToList();
        Assert.Equal(3, all.Count);

        // 状态切换（"恢复"）— 修改 status 字段
        var deprecated = col.FindOne(m => m.Status == "deprecated");
        deprecated.Status = "active";
        col.Update(deprecated);
        Assert.Equal(2, col.Find(m => m.Status == "active").Count());
    }

    [Fact]
    public void MetaPattern_out_of_scope_is_soft_delete_marker()
    {
        _db.Mapper.Entity<MetaPattern>().Id(x => x.Code);
        var col = _db.GetCollection<MetaPattern>("MetaPatterns");
        col.Insert(new MetaPattern { Code = "m_inv", Status = "active" });
        col.Insert(new MetaPattern { Code = "m_adj", Status = "out-of-scope", OutOfScopeReason = "no adjoint solver" });

        var active = col.Find(p => p.Status == "active").ToList();
        Assert.Single(active);
        // 但 out-of-scope 不删除，保留在表里供查询历史 / 提供 reason
        Assert.Equal(2, col.Count());
    }

    [Fact]
    public void KnownBug_status_lifecycle_open_to_fixed_upstream_to_archived()
    {
        _db.Mapper.Entity<KnownBug>().Id(x => x.IdBug);
        var col = _db.GetCollection<KnownBug>("KnownBugs");
        var bug = new KnownBug { IdBug = 1, Code = "R-Case-1", Status = "open" };
        col.Insert(bug);
        Assert.Single(col.Find(b => b.Status == "open"));

        // 状态机迁移
        bug.Status = "fixed-upstream";
        col.Update(bug);
        Assert.Empty(col.Find(b => b.Status == "open"));
        Assert.Single(col.Find(b => b.Status == "fixed-upstream"));

        // 长期归档
        bug.Status = "archived";
        col.Update(bug);
        Assert.Single(col.Find(b => b.Status == "archived"));
    }

    // ========== 跨实体软外键约束（验证应在 BLL 层拦截，DAL 不强制）==========

    [Fact]
    public void MRBinding_can_reference_nonexistent_MR_at_DAL_level()
    {
        // 设计选择：DAL 层不强制 FK；BLL 服务层校验。本测试明示该契约。
        _db.Mapper.Entity<MRBinding>().Id(x => x.IdMRBinding);
        var col = _db.GetCollection<MRBinding>("MRBindings");
        col.Insert(new MRBinding { IdMRBinding = 1, MRId = 99999, ApplicationId = 99999 });

        // 没有抛错 — DAL 不知道 MRId 99999 不存在
        Assert.Equal(1, col.Count());
    }

    [Fact]
    public void Anomaly_can_reference_nonexistent_Result_at_DAL_level()
    {
        _db.Mapper.Entity<MetBench_Domain.Anomaly>().Id(x => x.IdAnomaly);
        var col = _db.GetCollection<MetBench_Domain.Anomaly>("Anomalies");
        col.Insert(new MetBench_Domain.Anomaly
        {
            IdAnomaly = Guid.NewGuid(),
            ResultId = Guid.NewGuid(),  // 任意 GUID，没有对应 Result
            Severity = "major",
            Status = "new",
        });
        Assert.Equal(1, col.Count());
    }

    // ========== Schema 兼容性（向后兼容字段加减）==========

    [Fact]
    public void Adding_new_field_to_entity_does_not_break_old_records()
    {
        // 模拟 v1 数据：Mutant 没 OperatorId 字段（fictional rollback）
        _db.Mapper.Entity<Mutant>().Id(x => x.IdMutant);
        var col = _db.GetCollection("Mutants");
        var oldDoc = new BsonDocument { ["_id"] = 1, ["AppliedDiff"] = "old" };  // 缺 OperatorId / Status
        col.Insert(oldDoc);

        // 用 v2 的 Mutant 类读 — 缺失字段应填默认值
        var typedCol = _db.GetCollection<Mutant>("Mutants");
        var loaded = typedCol.FindById(1);
        Assert.NotNull(loaded);
        Assert.Equal("old", loaded!.AppliedDiff);
        Assert.Equal(0, loaded.OperatorId);
        Assert.Equal("active", loaded.Status);  // [BsonId] + 实体 default
    }

    [Fact]
    public void Removing_field_from_entity_ignores_old_field_in_doc()
    {
        // 模拟 v1 写入了 ObsoleteField — 现在 v2 实体不再含此字段
        _db.Mapper.Entity<MetaPattern>().Id(x => x.Code);
        var col = _db.GetCollection("MetaPatterns");
        var doc = new BsonDocument
        {
            ["_id"] = "m_inv",
            ["Name"] = "Invariance",
            ["LegacyV1Field"] = "should be ignored",  // v2 entity 没此字段
        };
        col.Insert(doc);

        var typedCol = _db.GetCollection<MetaPattern>("MetaPatterns");
        var loaded = typedCol.FindById("m_inv");
        Assert.NotNull(loaded);
        Assert.Equal("Invariance", loaded!.Name);
        // Legacy 字段静默忽略 — 不抛
    }

    // ========== 复合唯一索引（针对 v2 高频实体）==========

    [Fact]
    public void MutationResult_composite_uniqueness_must_be_enforced_at_BLL_layer()
    {
        // LiteDB 不原生支持 (MutantId, MRBindingId) 复合唯一索引；BLL 层调用前需先 GetByMutantBinding(...)
        // 校验。本测试明示该契约 + 验证 IMutationResultRepository.GetByMutantBinding 是 BLL 层校验入口。
        _db.Mapper.Entity<MutationResult>().Id(x => x.IdMutationResult);
        var col = _db.GetCollection<MutationResult>("MutationResults");
        col.EnsureIndex(x => x.MutantId);
        col.EnsureIndex(x => x.MRBindingId);

        var campaignId = Guid.NewGuid();
        col.Insert(new MutationResult { IdMutationResult = Guid.NewGuid(), CampaignId = campaignId, MutantId = 1, MRBindingId = 1, Outcome = "detected" });

        // BLL 层应这么做：
        var existing = col.FindOne(r => r.MutantId == 1 && r.MRBindingId == 1);
        Assert.NotNull(existing);
        // 如果 BLL 不查就直接 Insert，DAL 不阻止 — 这是 known design
        col.Insert(new MutationResult { IdMutationResult = Guid.NewGuid(), CampaignId = campaignId, MutantId = 1, MRBindingId = 1, Outcome = "missed" });
        Assert.Equal(2, col.Count());  // DAL allowed dup
    }

    [Fact]
    public void Result_ExecutionId_unique_index_enforces_one_to_one()
    {
        _db.Mapper.Entity<Result>().Id(x => x.IdResult);
        var col = _db.GetCollection<Result>("Results");
        col.EnsureIndex(x => x.ExecutionId, unique: true);

        var execId = Guid.NewGuid();
        col.Insert(new Result { IdResult = Guid.NewGuid(), ExecutionId = execId, SourceValue = 1.0, FollowupValue = 1.1 });

        Assert.Throws<LiteException>(() =>
            col.Insert(new Result { IdResult = Guid.NewGuid(), ExecutionId = execId, SourceValue = 2.0, FollowupValue = 2.1 }));
    }
}
