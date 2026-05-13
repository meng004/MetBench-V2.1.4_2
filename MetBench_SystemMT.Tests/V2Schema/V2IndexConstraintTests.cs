using LiteDB;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// TDD 验证 P1.8：LiteDB 唯一索引约束生效。
/// 用临时 LiteDB 文件 + 手动 EnsureIndex(unique:true) 模拟 DbConfig 行为，
/// 验证违约时抛 LiteException。
/// </summary>
public sealed class V2IndexConstraintTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ILiteDatabase _db;

    public V2IndexConstraintTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchV2IndexTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _db = new LiteDatabase(_dbPath, new BsonMapper());
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var logFile = _dbPath + "-log";
        if (File.Exists(logFile)) File.Delete(logFile);
    }

    [Fact]
    public void Runtimes_Name_unique_index_blocks_duplicate()
    {
        _db.Mapper.Entity<MetBench_Domain.Runtime>().Id(x => x.IdRuntime);
        var col = _db.GetCollection<MetBench_Domain.Runtime>("Runtimes");
        col.EnsureIndex(x => x.Name, unique: true);

        col.Insert(new MetBench_Domain.Runtime { Name = "python-x", Kind = "python", InvokeTemplate = "..." });
        Assert.Throws<LiteException>(() =>
            col.Insert(new MetBench_Domain.Runtime { Name = "python-x", Kind = "python", InvokeTemplate = "..." }));
    }

    [Fact]
    public void MRBindings_composite_unique_blocks_duplicate()
    {
        _db.Mapper.Entity<MRBinding>().Id(x => x.IdMRBinding);
        var col = _db.GetCollection<MRBinding>("MRBindings");
        col.EnsureIndex("MRBinding_Composite",
            x => new { x.MRId, x.ApplicationId, x.DefaultSampleCasePath }, unique: true);

        col.Insert(new MRBinding { MRId = 1, ApplicationId = 1, DefaultSampleCasePath = "case-a.json" });
        // 相同三元组应被拒
        Assert.Throws<LiteException>(() =>
            col.Insert(new MRBinding { MRId = 1, ApplicationId = 1, DefaultSampleCasePath = "case-a.json" }));
        // 不同 case path 应允许
        col.Insert(new MRBinding { MRId = 1, ApplicationId = 1, DefaultSampleCasePath = "case-b.json" });
        Assert.Equal(2, col.Count());
    }

    [Fact]
    public void ApplicationDomains_composite_unique_blocks_duplicate()
    {
        _db.Mapper.Entity<ApplicationDomain>().Id(x => x.IdJunction);
        var col = _db.GetCollection<ApplicationDomain>("ApplicationDomains");
        col.EnsureIndex("AppDomain_Composite",
            x => new { x.ApplicationId, x.DomainId }, unique: true);

        col.Insert(new ApplicationDomain { ApplicationId = 1, DomainId = 1 });
        Assert.Throws<LiteException>(() =>
            col.Insert(new ApplicationDomain { ApplicationId = 1, DomainId = 1 }));
    }

    [Fact]
    public void DiscoveryMethods_NameVersion_unique_blocks_duplicate()
    {
        _db.Mapper.Entity<DiscoveryMethod>().Id(x => x.IdMethod);
        var col = _db.GetCollection<DiscoveryMethod>("DiscoveryMethods");
        col.EnsureIndex("Method_NameVersion",
            x => new { x.Name, x.Version }, unique: true);

        col.Insert(new DiscoveryMethod { Name = "LLM-Native", Version = "v1" });
        Assert.Throws<LiteException>(() =>
            col.Insert(new DiscoveryMethod { Name = "LLM-Native", Version = "v1" }));
        // 不同 version 允许
        col.Insert(new DiscoveryMethod { Name = "LLM-Native", Version = "v2" });
        Assert.Equal(2, col.Count());
    }

    [Fact]
    public void MutationOperators_Code_unique_blocks_duplicate()
    {
        _db.Mapper.Entity<MutationOperator>().Id(x => x.IdOperator);
        var col = _db.GetCollection<MutationOperator>("MutationOperators");
        col.EnsureIndex(x => x.Code, unique: true);

        col.Insert(new MutationOperator { Code = "Mut00-identity" });
        Assert.Throws<LiteException>(() =>
            col.Insert(new MutationOperator { Code = "Mut00-identity" }));
    }

    [Fact]
    public void MutationResults_composite_unique_blocks_duplicate()
    {
        _db.Mapper.Entity<MutationResult>().Id(x => x.IdMutationResult);
        var col = _db.GetCollection<MutationResult>("MutationResults");
        col.EnsureIndex("MutResult_Composite",
            x => new { x.MutantId, x.MRBindingId }, unique: true);

        col.Insert(new MutationResult
        {
            IdMutationResult = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            MutantId = 1, MRBindingId = 1,
            ExecutionId = Guid.NewGuid(),
            Outcome = "detected"
        });
        Assert.Throws<LiteException>(() =>
            col.Insert(new MutationResult
            {
                IdMutationResult = Guid.NewGuid(),
                CampaignId = Guid.NewGuid(),
                MutantId = 1, MRBindingId = 1,
                ExecutionId = Guid.NewGuid(),
                Outcome = "missed"
            }));
    }

    [Fact]
    public void KnownBugs_Code_unique_blocks_duplicate()
    {
        _db.Mapper.Entity<KnownBug>().Id(x => x.IdBug);
        var col = _db.GetCollection<KnownBug>("KnownBugs");
        col.EnsureIndex(x => x.Code, unique: true);

        col.Insert(new KnownBug { Code = "R-Case-1" });
        Assert.Throws<LiteException>(() =>
            col.Insert(new KnownBug { Code = "R-Case-1" }));
    }

    [Fact]
    public void BatchPlans_Name_unique_blocks_duplicate()
    {
        _db.Mapper.Entity<BatchPlan>().Id(x => x.IdPlan);
        var col = _db.GetCollection<BatchPlan>("BatchPlans");
        col.EnsureIndex(x => x.Name, unique: true);

        col.Insert(new BatchPlan { Name = "nightly" });
        Assert.Throws<LiteException>(() =>
            col.Insert(new BatchPlan { Name = "nightly" }));
    }

    [Fact]
    public void Results_ExecutionId_unique_blocks_duplicate()
    {
        _db.Mapper.Entity<Result>().Id(x => x.IdResult);
        var col = _db.GetCollection<Result>("Results");
        col.EnsureIndex(x => x.ExecutionId, unique: true);

        var execId = Guid.NewGuid();
        col.Insert(new Result { IdResult = Guid.NewGuid(), ExecutionId = execId, AssertionPassed = true });
        Assert.Throws<LiteException>(() =>
            col.Insert(new Result { IdResult = Guid.NewGuid(), ExecutionId = execId, AssertionPassed = false }));
    }

    [Fact]
    public void Anomalies_ResultId_unique_blocks_duplicate()
    {
        _db.Mapper.Entity<Anomaly>().Id(x => x.IdAnomaly);
        var col = _db.GetCollection<Anomaly>("Anomalies");
        col.EnsureIndex(x => x.ResultId, unique: true);

        var resultId = Guid.NewGuid();
        col.Insert(new Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = resultId, Severity = "major" });
        Assert.Throws<LiteException>(() =>
            col.Insert(new Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = resultId, Severity = "minor" }));
    }
}
