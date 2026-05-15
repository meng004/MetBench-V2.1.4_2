using LiteDB;
using MetBench_Domain;
using System.Configuration;
using System.Reflection;

namespace MetBench_DAL
{
    //数据库配置单例类
    //v2 扩展：在 v1 三个 collection (MetamorphicRelations / Applications / Domains) 基础上
    //新增 20 个 v2 collection。v1 + v2 共享同一 LiteDB 文件；
    //MetamorphicRelations / Applications 通过 Kind 字段区分 method-level / system-level。
    public sealed class DbConfig
    {
        // ===== v1 collection keys (保留) =====
        //蜕变关系集合的键
        public readonly string MetamorphicRelations_Collection_Key = "MetamorphicRelations";
        //应用程序集合的键
        public readonly string Applications_Collection_Key = "Applications";
        //应用领域集合的键
        public readonly string Domains_Collection_Key = "Domains";

        // ===== v2 新增 collection keys =====
        public readonly string Runtimes_Key = "Runtimes";
        public readonly string MRBindings_Key = "MRBindings";
        public readonly string ApplicationDomains_Key = "ApplicationDomains";
        public readonly string MRInstances_Key = "MRInstances";
        public readonly string Executions_Key = "Executions";
        public readonly string Results_Key = "Results";
        public readonly string Anomalies_Key = "Anomalies";
        public readonly string DiscoveryMethods_Key = "DiscoveryMethods";
        public readonly string DiscoveryRuns_Key = "DiscoveryRuns";
        public readonly string CandidateMRs_Key = "CandidateMRs";
        public readonly string ValidationRuns_Key = "ValidationRuns";
        public readonly string MutationOperators_Key = "MutationOperators";
        public readonly string Mutants_Key = "Mutants";
        public readonly string MutationCampaigns_Key = "MutationCampaigns";
        public readonly string MutationResults_Key = "MutationResults";
        public readonly string KnownBugs_Key = "KnownBugs";
        public readonly string AuditLog_Key = "AuditLog";
        public readonly string Batches_Key = "Batches";
        public readonly string BatchPlans_Key = "BatchPlans";
        public readonly string Reports_Key = "Reports";
        public readonly string MetaPatterns_Key = "MetaPatterns";

        /// <summary>
        /// 连接字符串
        /// database connection string
        /// </summary>
        //配置文件读取数据库连接字符串
        public string _conn
        {
            get
            {
                //读取数据库连接字符串
                //read connectionstring.

                Assembly assembly = Assembly.GetEntryAssembly();
                // 获取执行程序集的文件路径
                string assemblyPath = assembly.Location;

                // 获取解决方案的目录路径
                string solutionDirPath = Path.GetDirectoryName(assemblyPath);

                // 循环向上查找解决方案文件（.sln）
                while (!Directory.GetFiles(solutionDirPath, "*.sln").Any())
                {
                    // 获取上级目录路径
                    string parentDirPath = Directory.GetParent(solutionDirPath)?.FullName;

                    // 如果已经到达根目录，则返回空字符串
                    if (parentDirPath == null)
                    {
                        return string.Empty;
                    }

                    solutionDirPath = parentDirPath;
                }

                var db_file = ConfigurationManager.ConnectionStrings["litedb"].ConnectionString;
                //string appName = Assembly.GetEntryAssembly().GetName().Name;//获取应用程序名称
                string appPath = $"{solutionDirPath}\\MetBench_DataBase";//获取应用程序的路径
                Directory.CreateDirectory(appPath); //目录存在则无操作
                var conn = db_file.Replace("|DataDirectory|", appPath);
                return conn;
            }
        }

        //DbConfig实例
         private static DbConfig instance;
        //锁
        private static readonly object _lock = new object();

        //使用单例模式 完成实体映射数据表
        private DbConfig()
        {
            //实体映射数据表
            using (var db = new LiteDatabase(_conn))
            {
                var mapper = BsonMapper.Global;
                //建立引用

                RegisterV1Collections(db, mapper);
                RegisterV2InfrastructureCollections(db, mapper);
                RegisterV2ExecutionCollections(db, mapper);
                RegisterV2DiscoveryCollections(db, mapper);
                RegisterV2MutationCollections(db, mapper);
                RegisterV2MiscCollections(db, mapper);
            }
        }

        // ============================================================
        // v1 既有 collection（保留行为不变）
        // ============================================================
        private void RegisterV1Collections(LiteDatabase db, BsonMapper mapper)
        {
            if (!db.CollectionExists(MetamorphicRelations_Collection_Key))
            {
                //配置MetamorphicRelations
                var collection = db.GetCollection<MetamorphicRelation>(MetamorphicRelations_Collection_Key);
                //添加唯一复合索引
                collection.EnsureIndex("MR_Idx", x => new { x.InputPattern, x.OutputPattern, x.ApplicationName }, true);
                //配置MetamorphicRelations
                mapper.Entity<MetamorphicRelation>()
               .Id(x => x.IdMR)
               .Field(x => x.ApplicationName, "ApplicationName");// 映射属性到字段
                // v2 字段：自动序列化（无需 .Field 映射）；查询时按 Code / MetaPatternCode / Kind 等过滤
                collection.EnsureIndex(x => x.Code);
                collection.EnsureIndex(x => x.MetaPatternCode);
                collection.EnsureIndex(x => x.Kind);
            }

            if (!db.CollectionExists(Applications_Collection_Key))
            {
                //配置Applications
                var collection = db.GetCollection<Application>(Applications_Collection_Key);
                // 添加唯一索引
                collection.EnsureIndex(x => x.Name, unique: true);
                // 添加唯一复合索引
                collection.EnsureIndex("App_Id", x => new { x.Name, x.ProgrammingLanguage }, unique: true);
                mapper.Entity<Application>()
                .Id(x => x.IdApplication)
                .Field(x => x.DomainName, "DomainName"); // 映射属性到字段
                // v2 字段索引
                collection.EnsureIndex(x => x.Kind);
                collection.EnsureIndex(x => x.RuntimeId);
                collection.EnsureIndex(x => x.Version);
            }

            if (!db.CollectionExists(Domains_Collection_Key))
            {
                //配置Domains
                var collection = db.GetCollection<Domain>(Domains_Collection_Key);
                // 添加唯一索引
                collection.EnsureIndex(x => x.Name, unique: true);
                // 添加唯一复合索引
                collection.EnsureIndex("Domain_Id", x => new { x.Name, x.Description }, unique: true);
                mapper.Entity<Domain>()
                .Id(x => x.IdDomain);
            }
        }

        // ============================================================
        // v2 基础设施 collection (Runtime / MRBinding / ApplicationDomain / MRInstance)
        // ============================================================
        private void RegisterV2InfrastructureCollections(LiteDatabase db, BsonMapper mapper)
        {
            if (!db.CollectionExists(Runtimes_Key))
            {
                var collection = db.GetCollection<MetBench_Domain.Runtime>(Runtimes_Key);
                collection.EnsureIndex(x => x.Name, unique: true);
                collection.EnsureIndex(x => x.Kind);
                mapper.Entity<MetBench_Domain.Runtime>().Id(x => x.IdRuntime);
            }

            if (!db.CollectionExists(MRBindings_Key))
            {
                var collection = db.GetCollection<MRBinding>(MRBindings_Key);
                // 复合唯一：同一 MR + SUT + 默认案例 只能 1 个 active binding
                collection.EnsureIndex("MRBinding_Composite",
                    x => new { x.MRId, x.ApplicationId, x.DefaultSampleCasePath }, unique: true);
                collection.EnsureIndex(x => x.MRId);
                collection.EnsureIndex(x => x.ApplicationId);
                collection.EnsureIndex(x => x.IsActive);
                mapper.Entity<MRBinding>().Id(x => x.IdMRBinding);
            }

            if (!db.CollectionExists(ApplicationDomains_Key))
            {
                var collection = db.GetCollection<ApplicationDomain>(ApplicationDomains_Key);
                // 复合唯一
                collection.EnsureIndex("AppDomain_Composite",
                    x => new { x.ApplicationId, x.DomainId }, unique: true);
                mapper.Entity<ApplicationDomain>().Id(x => x.IdJunction);
            }

            if (!db.CollectionExists(MRInstances_Key))
            {
                var collection = db.GetCollection<MRInstance>(MRInstances_Key);
                collection.EnsureIndex(x => x.MRBindingId);
                collection.EnsureIndex(x => x.IsReusable);
                collection.EnsureIndex(x => x.Name);
                mapper.Entity<MRInstance>().Id(x => x.IdInstance);
            }
        }

        // ============================================================
        // v2 执行相关 collection (Execution / Result / Anomaly) — Guid PK
        // ============================================================
        private void RegisterV2ExecutionCollections(LiteDatabase db, BsonMapper mapper)
        {
            if (!db.CollectionExists(Executions_Key))
            {
                var collection = db.GetCollection<Execution>(Executions_Key);
                collection.EnsureIndex(x => x.MRInstanceId);
                collection.EnsureIndex(x => x.BatchId);
                collection.EnsureIndex(x => x.Status);
                collection.EnsureIndex(x => x.QueuedAt);
                collection.EnsureIndex(x => x.TriggeredBy);
                collection.EnsureIndex(x => x.SutVersionSnapshot);
                mapper.Entity<Execution>().Id(x => x.IdExecution);
            }

            if (!db.CollectionExists(Results_Key))
            {
                var collection = db.GetCollection<Result>(Results_Key);
                collection.EnsureIndex(x => x.ExecutionId, unique: true);
                collection.EnsureIndex(x => x.AssertionPassed);
                mapper.Entity<Result>().Id(x => x.IdResult);
            }

            if (!db.CollectionExists(Anomalies_Key))
            {
                var collection = db.GetCollection<Anomaly>(Anomalies_Key);
                collection.EnsureIndex(x => x.ResultId, unique: true);
                collection.EnsureIndex(x => x.Status);
                collection.EnsureIndex(x => x.Severity);
                collection.EnsureIndex(x => x.LinkedKnownBugId);
                collection.EnsureIndex(x => x.Category);
                mapper.Entity<Anomaly>().Id(x => x.IdAnomaly);
            }
        }

        // ============================================================
        // v2 Discovery 子系统 collection
        // ============================================================
        private void RegisterV2DiscoveryCollections(LiteDatabase db, BsonMapper mapper)
        {
            if (!db.CollectionExists(DiscoveryMethods_Key))
            {
                var collection = db.GetCollection<DiscoveryMethod>(DiscoveryMethods_Key);
                collection.EnsureIndex("Method_NameVersion",
                    x => new { x.Name, x.Version }, unique: true);
                collection.EnsureIndex(x => x.Enabled);
                mapper.Entity<DiscoveryMethod>().Id(x => x.IdMethod);
            }

            if (!db.CollectionExists(DiscoveryRuns_Key))
            {
                var collection = db.GetCollection<DiscoveryRun>(DiscoveryRuns_Key);
                collection.EnsureIndex(x => x.MethodId);
                collection.EnsureIndex(x => x.StartedAt);
                collection.EnsureIndex(x => x.TargetApplicationId);
                collection.EnsureIndex(x => x.Status);
                mapper.Entity<DiscoveryRun>().Id(x => x.IdRun);
            }

            if (!db.CollectionExists(CandidateMRs_Key))
            {
                var collection = db.GetCollection<CandidateMR>(CandidateMRs_Key);
                collection.EnsureIndex(x => x.DiscoveryRunId);
                collection.EnsureIndex(x => x.Status);
                collection.EnsureIndex(x => x.PromotedToMRId);
                mapper.Entity<CandidateMR>().Id(x => x.IdCandidate);
            }

            if (!db.CollectionExists(ValidationRuns_Key))
            {
                var collection = db.GetCollection<ValidationRun>(ValidationRuns_Key);
                collection.EnsureIndex(x => x.CandidateMRId);
                collection.EnsureIndex(x => x.ValidatorName);
                collection.EnsureIndex(x => x.Passed);
                mapper.Entity<ValidationRun>().Id(x => x.IdValidation);
            }
        }

        // ============================================================
        // v2 Mutation 子系统 collection
        // ============================================================
        private void RegisterV2MutationCollections(LiteDatabase db, BsonMapper mapper)
        {
            if (!db.CollectionExists(MutationOperators_Key))
            {
                var collection = db.GetCollection<MutationOperator>(MutationOperators_Key);
                collection.EnsureIndex(x => x.Code, unique: true);
                collection.EnsureIndex(x => x.Category);
                mapper.Entity<MutationOperator>().Id(x => x.IdOperator);
            }

            if (!db.CollectionExists(Mutants_Key))
            {
                var collection = db.GetCollection<Mutant>(Mutants_Key);
                collection.EnsureIndex("Mutant_Composite",
                    x => new { x.OperatorId, x.ApplicationId });
                collection.EnsureIndex(x => x.Status);
                mapper.Entity<Mutant>().Id(x => x.IdMutant);
            }

            if (!db.CollectionExists(MutationCampaigns_Key))
            {
                var collection = db.GetCollection<MutationCampaign>(MutationCampaigns_Key);
                collection.EnsureIndex(x => x.Status);
                collection.EnsureIndex(x => x.StartedAt);
                mapper.Entity<MutationCampaign>().Id(x => x.IdCampaign);
            }

            if (!db.CollectionExists(MutationResults_Key))
            {
                var collection = db.GetCollection<MutationResult>(MutationResults_Key);
                collection.EnsureIndex(x => x.CampaignId);
                collection.EnsureIndex("MutResult_Composite",
                    x => new { x.MutantId, x.MRBindingId }, unique: true);
                collection.EnsureIndex(x => x.Outcome);
                mapper.Entity<MutationResult>().Id(x => x.IdMutationResult);
            }
        }

        // ============================================================
        // v2 杂项 collection (KnownBug / AuditLog / Batch / BatchPlan / Report)
        // ============================================================
        private void RegisterV2MiscCollections(LiteDatabase db, BsonMapper mapper)
        {
            if (!db.CollectionExists(KnownBugs_Key))
            {
                var collection = db.GetCollection<KnownBug>(KnownBugs_Key);
                collection.EnsureIndex(x => x.Code, unique: true);
                collection.EnsureIndex(x => x.Status);
                collection.EnsureIndex(x => x.RelatedApplicationId);
                mapper.Entity<KnownBug>().Id(x => x.IdBug);
            }

            if (!db.CollectionExists(AuditLog_Key))
            {
                var collection = db.GetCollection<AuditLog>(AuditLog_Key);
                collection.EnsureIndex(x => x.Timestamp);
                collection.EnsureIndex(x => x.Action);
                collection.EnsureIndex(x => x.TargetEntityId);
                mapper.Entity<AuditLog>().Id(x => x.IdLog);
            }

            if (!db.CollectionExists(Batches_Key))
            {
                var collection = db.GetCollection<Batch>(Batches_Key);
                collection.EnsureIndex(x => x.PlanId);
                collection.EnsureIndex(x => x.Status);
                collection.EnsureIndex(x => x.CreatedAt);
                mapper.Entity<Batch>().Id(x => x.IdBatch);
            }

            if (!db.CollectionExists(BatchPlans_Key))
            {
                var collection = db.GetCollection<BatchPlan>(BatchPlans_Key);
                collection.EnsureIndex(x => x.Name, unique: true);
                collection.EnsureIndex(x => x.Enabled);
                mapper.Entity<BatchPlan>().Id(x => x.IdPlan);
            }

            if (!db.CollectionExists(Reports_Key))
            {
                var collection = db.GetCollection<Report>(Reports_Key);
                collection.EnsureIndex(x => x.Scope);
                collection.EnsureIndex(x => x.GeneratedAt);
                mapper.Entity<Report>().Id(x => x.IdReport);
            }
        }

       //加锁
        public static DbConfig Instance
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new DbConfig();
                    }
                    return instance;
                }
            }
        }
    }
}
