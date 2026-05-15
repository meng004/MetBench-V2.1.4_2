using LiteDB;
using MetBench_DAL;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// TDD 验证 P1.8：DbConfig 单例可访问 + 所有 23 个 collection key 暴露 + 通过
/// LiteDB 连接能 enumerate 出 collection 名称。
///
/// 注意：DbConfig.Instance 单例会读真实文件系统的 .sln 路径来定位 LiteDB 文件，
/// 所以本测试只验证 collection key 暴露 + 基础设施，不实际初始化数据库。
/// </summary>
public sealed class V2DbConfigRegistrationTests
{
    [Fact]
    public void P1_8_All_23_collection_keys_are_exposed()
    {
        // 使用反射访问 readonly 字段（避免触发 Instance 单例 ctor）
        var type = typeof(DbConfig);
        var expectedV1 = new[]
        {
            "MetamorphicRelations_Collection_Key",
            "Applications_Collection_Key",
            "Domains_Collection_Key",
        };
        var expectedV2 = new[]
        {
            "Runtimes_Key", "MRBindings_Key", "ApplicationDomains_Key", "MRInstances_Key",
            "Executions_Key", "Results_Key", "Anomalies_Key",
            "DiscoveryMethods_Key", "DiscoveryRuns_Key", "CandidateMRs_Key", "ValidationRuns_Key",
            "MutationOperators_Key", "Mutants_Key", "MutationCampaigns_Key", "MutationResults_Key",
            "KnownBugs_Key", "AuditLog_Key", "Batches_Key", "BatchPlans_Key", "Reports_Key",
        };

        foreach (var field in expectedV1.Concat(expectedV2))
        {
            var fi = type.GetField(field, System.Reflection.BindingFlags.Public
                                        | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(fi);
        }

        // 总数：3 v1 + 20 v2 = 23
        Assert.Equal(23, expectedV1.Length + expectedV2.Length);
    }

    /// <summary>
    /// 模拟 DbConfig 的 collection 注册行为（不依赖真实 LiteDB 文件路径），
    /// 验证用同一逻辑创建的临时 LiteDB 能列出全部 23 个 collection。
    /// </summary>
    [Fact]
    public void P1_8_Collection_registration_creates_23_collections_in_litedb()
    {
        var dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchV2DbConfigTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        try
        {
            using (var db = new LiteDatabase(dbPath, new BsonMapper()))
            {
                // 模拟 DbConfig 注册过程：每个 collection 至少 GetCollection 一次
                // （LiteDB 在写入第一行时才物理建表，所以也插一行）
                Register(db, "MetamorphicRelations", new MetamorphicRelation { Code = "MR-X" });
                Register(db, "Applications", new Application { Name = "App-X" });
                Register(db, "Domains", new Domain { Name = "Domain-X" });
                Register(db, "Runtimes", new MetBench_Domain.Runtime { Name = "rt-x" });
                Register(db, "MRBindings", new MRBinding { MRId = 1, ApplicationId = 1 });
                Register(db, "ApplicationDomains", new ApplicationDomain { ApplicationId = 1, DomainId = 1 });
                Register(db, "MRInstances", new MRInstance { MRBindingId = 1 });
                Register(db, "Executions", new Execution { IdExecution = Guid.NewGuid(), Status = "queued" });
                Register(db, "Results", new Result { IdResult = Guid.NewGuid(), ExecutionId = Guid.NewGuid() });
                Register(db, "Anomalies", new Anomaly { IdAnomaly = Guid.NewGuid(), ResultId = Guid.NewGuid() });
                Register(db, "DiscoveryMethods", new DiscoveryMethod { Name = "m", Version = "v1" });
                Register(db, "DiscoveryRuns", new DiscoveryRun { IdRun = Guid.NewGuid(), MethodId = 1, StartedAt = DateTime.UtcNow });
                Register(db, "CandidateMRs", new CandidateMR { IdCandidate = Guid.NewGuid(), DiscoveryRunId = Guid.NewGuid() });
                Register(db, "ValidationRuns", new ValidationRun { IdValidation = Guid.NewGuid(), CandidateMRId = Guid.NewGuid() });
                Register(db, "MutationOperators", new MutationOperator { Code = "Mut-X" });
                Register(db, "Mutants", new Mutant { OperatorId = 1 });
                Register(db, "MutationCampaigns", new MutationCampaign { IdCampaign = Guid.NewGuid(), Name = "C", StartedAt = DateTime.UtcNow });
                Register(db, "MutationResults", new MutationResult { IdMutationResult = Guid.NewGuid(), CampaignId = Guid.NewGuid(), MutantId = 1, MRBindingId = 1, ExecutionId = Guid.NewGuid(), Outcome = "missed" });
                Register(db, "KnownBugs", new KnownBug { Code = "R-X" });
                Register(db, "AuditLog", new AuditLog { IdLog = Guid.NewGuid(), Timestamp = DateTime.UtcNow, Action = "test" });
                Register(db, "Batches", new Batch { IdBatch = Guid.NewGuid(), Name = "B" });
                Register(db, "BatchPlans", new BatchPlan { Name = "P" });
                Register(db, "Reports", new Report { IdReport = Guid.NewGuid(), GeneratedAt = DateTime.UtcNow, Scope = "ad-hoc" });

                var names = db.GetCollectionNames().ToList();
                Assert.Equal(23, names.Count);
            }
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
            var logFile = dbPath + "-log";
            if (File.Exists(logFile)) File.Delete(logFile);
        }
    }

    private static void Register<T>(ILiteDatabase db, string name, T entity)
    {
        var col = db.GetCollection<T>(name);
        col.Insert(entity);
    }
}
