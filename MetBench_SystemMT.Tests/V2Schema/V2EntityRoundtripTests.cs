using LiteDB;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// TDD 验证 P1.1-P1.7：所有 v2 实体（含扩展的 v1 实体）+ value object 都能在 LiteDB 完成
/// insert/find round-trip，字段值无损失。
/// 每个实体独立 [Fact]；本测试不依赖 DbConfig.Instance（避免污染 v1 数据库），
/// 用临时 LiteDatabase 文件做隔离。
/// </summary>
public sealed class V2EntityRoundtripTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ILiteDatabase _db;

    public V2EntityRoundtripTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchV2RoundtripTests",
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

    // ============================================================
    // P1.1 — 扩展的 v1 实体（v2 字段）
    // ============================================================

    [Fact]
    public void P1_1_MetamorphicRelation_v2_fields_roundtrip()
    {
        _db.Mapper.Entity<MetamorphicRelation>().Id(x => x.IdMR);
        var col = _db.GetCollection<MetamorphicRelation>("MetamorphicRelations");

        var entity = new MetamorphicRelation
        {
            // v1 字段
            Description = "MR-T 升温降 k_eff",
            InputPattern = "T → factor·T",
            OutputPattern = "k(T) > k(factor·T)",
            // v2 字段
            Code = "MR-T",
            MetaPatternCode = "m_mono",
            TransformationName = "ScaleField",
            AssertionTypeCode = "less-noise-aware",
            ValueName = "k_eff",
            NoiseAware = true,
            ToleranceRel = 0.001,
            NoiseMultiplier = 3.0,
            FeatureFilePath = "metbench/catalog/features/m_mono/MR-T-RaiseFuelTemperature.feature",
            DiscoveryRunId = Guid.NewGuid(),
            DiscoveryConfidence = 0.85,
            DiscoveryMethod = "llm-native",
            CreatedAt = new DateTime(2026, 5, 13, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "alice@example.com",
            Kind = "system-level"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdMR);
        Assert.NotNull(fetched);
        Assert.Equal("MR-T", fetched.Code);
        Assert.Equal("m_mono", fetched.MetaPatternCode);
        Assert.Equal("ScaleField", fetched.TransformationName);
        Assert.Equal("less-noise-aware", fetched.AssertionTypeCode);
        Assert.True(fetched.NoiseAware);
        Assert.Equal(0.001, fetched.ToleranceRel);
        Assert.Equal(3.0, fetched.NoiseMultiplier);
        Assert.Equal("llm-native", fetched.DiscoveryMethod);
        Assert.Equal(0.85, fetched.DiscoveryConfidence);
        Assert.Equal("system-level", fetched.Kind);
        // v1 字段保留：
        Assert.Equal("MR-T 升温降 k_eff", fetched.Description);
        Assert.Equal("T → factor·T", fetched.InputPattern);
    }

    [Fact]
    public void P1_1_Application_v2_fields_roundtrip()
    {
        _db.Mapper.Entity<Application>().Id(x => x.IdApplication);
        var col = _db.GetCollection<Application>("Applications");

        var entity = new Application
        {
            Name = "openmoc-prod-2026q2",
            Description = "OpenMOC 3D-MOC build for production",
            ProgrammingLanguage = "Python",
            Version = "3D-MOC@a1f2b3c",
            RuntimeId = 1,
            RunnerEntryPath = "SUT/openmoc/openmoc_runner.py",
            InputParserPath = "SUT/openmoc/openmoc_input_parser.py",
            OutputParserPath = "SUT/openmoc/openmoc_output_parser.py",
            DefaultTimeoutSeconds = 120,
            MaxConcurrentRuns = 2,
            Kind = "system-level"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdApplication);
        Assert.NotNull(fetched);
        Assert.Equal("openmoc-prod-2026q2", fetched.Name);
        Assert.Equal("3D-MOC@a1f2b3c", fetched.Version);
        Assert.Equal(1, fetched.RuntimeId);
        Assert.Equal("SUT/openmoc/openmoc_input_parser.py", fetched.InputParserPath);
        Assert.Equal(120, fetched.DefaultTimeoutSeconds);
        Assert.Equal("system-level", fetched.Kind);
    }

    // ============================================================
    // P1.2 — Value object
    // ============================================================

    [Fact]
    public void P1_2_ValueRange_roundtrip_via_embedded()
    {
        var range = new ValueRange { Min = 273.15, Max = 3000.0 };
        var bson = _db.Mapper.ToDocument(range);
        var restored = _db.Mapper.ToObject<ValueRange>(bson);
        Assert.Equal(273.15, restored.Min);
        Assert.Equal(3000.0, restored.Max);
    }

    [Fact]
    public void P1_2_ToleranceConfig_roundtrip()
    {
        var tol = new ToleranceConfig
        {
            NoiseAware = true,
            ToleranceRel = 0.005,
            ToleranceAbs = 0.0,
            NoiseMultiplier = 3.0
        };
        var bson = _db.Mapper.ToDocument(tol);
        var restored = _db.Mapper.ToObject<ToleranceConfig>(bson);
        Assert.True(restored.NoiseAware);
        Assert.Equal(0.005, restored.ToleranceRel);
        Assert.Equal(3.0, restored.NoiseMultiplier);
    }

    [Fact]
    public void P1_2_SutHyperparams_roundtrip_with_extras()
    {
        var hp = new SutHyperparams
        {
            Seed = 42,
            Particles = 50000,
            Batches = 60,
            MaxIterations = null,
            FitnessFunction = "k_inf_drift",
            OtherParams = new Dictionary<string, string> { ["solver"] = "moc-3d" }
        };
        var bson = _db.Mapper.ToDocument(hp);
        var restored = _db.Mapper.ToObject<SutHyperparams>(bson);
        Assert.Equal(42L, restored.Seed);
        Assert.Equal(50000, restored.Particles);
        Assert.Equal(60, restored.Batches);
        Assert.Null(restored.MaxIterations);
        Assert.Equal("k_inf_drift", restored.FitnessFunction);
        Assert.NotNull(restored.OtherParams);
        Assert.Equal("moc-3d", restored.OtherParams!["solver"]);
    }

    [Fact]
    public void P1_2_SamplingSpec_roundtrip_fixed_list()
    {
        var spec = new SamplingSpec
        {
            Distribution = "fixed-list",
            SampleCount = 5,
            DistributionParams = new Dictionary<string, object>
            {
                ["values"] = new List<double> { 1.1, 1.25, 1.5, 1.75, 2.0 }
            }
        };
        var bson = _db.Mapper.ToDocument(spec);
        var restored = _db.Mapper.ToObject<SamplingSpec>(bson);
        Assert.Equal("fixed-list", restored.Distribution);
        Assert.Equal(5, restored.SampleCount);
        Assert.True(restored.DistributionParams.ContainsKey("values"));
    }

    [Fact]
    public void P1_2_ParameterMapping_roundtrip()
    {
        var pm = new ParameterMapping
        {
            AbstractParamName = "fuel.temperature",
            ConcreteFieldPath = "materials.fuel.temperature_kelvin",
            FieldType = "number",
            ValueRange = new ValueRange { Min = 273.0, Max = 3000.0 },
            Unit = "K",
            PathSyntax = "json-pointer",
            Notes = "Doppler 燃料温度场"
        };
        var bson = _db.Mapper.ToDocument(pm);
        var restored = _db.Mapper.ToObject<ParameterMapping>(bson);
        Assert.Equal("fuel.temperature", restored.AbstractParamName);
        Assert.Equal("materials.fuel.temperature_kelvin", restored.ConcreteFieldPath);
        Assert.NotNull(restored.ValueRange);
        Assert.Equal(273.0, restored.ValueRange!.Min);
        Assert.Equal(3000.0, restored.ValueRange.Max);
    }

    // ============================================================
    // P1.3 — 基础设施实体
    // ============================================================

    [Fact]
    public void P1_3_Runtime_roundtrip()
    {
        _db.Mapper.Entity<MetBench_Domain.Runtime>().Id(x => x.IdRuntime);
        var col = _db.GetCollection<MetBench_Domain.Runtime>("Runtimes");
        var entity = new MetBench_Domain.Runtime
        {
            Name = "python-openmoc-venv",
            Kind = "python",
            InvokeTemplate = "{python} {script} --input {input} --output {output}",
            EnvVars = new Dictionary<string, string> { ["PYTHONHASHSEED"] = "0" },
            HealthCheckCommand = "{python} -c 'import openmoc; print(openmoc.__version__)'",
            Description = "OpenMOC 专属 venv"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdRuntime);
        Assert.NotNull(fetched);
        Assert.Equal("python", fetched.Kind);
        Assert.Equal("0", fetched.EnvVars["PYTHONHASHSEED"]);
    }

    [Fact]
    public void P1_3_MRBinding_with_embedded_ParameterMappings()
    {
        _db.Mapper.Entity<MRBinding>().Id(x => x.IdMRBinding);
        var col = _db.GetCollection<MRBinding>("MRBindings");
        var entity = new MRBinding
        {
            MRId = 1,
            ApplicationId = 1,
            ParameterMappings = new List<ParameterMapping>
            {
                new() { AbstractParamName = "fuel.temperature",
                        ConcreteFieldPath = "materials.fuel.temperature_kelvin",
                        Unit = "K" }
            },
            DefaultSampleCasePath = "SUT/openmoc/sample/pincell.json",
            DefaultTolerance = new ToleranceConfig { NoiseAware = false, ToleranceRel = 0.0 },
            DefaultHyperparams = new SutHyperparams { Particles = 5000 },
            IsActive = true,
            BoundBy = "alice"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdMRBinding);
        Assert.NotNull(fetched);
        Assert.Single(fetched.ParameterMappings);
        Assert.Equal("fuel.temperature", fetched.ParameterMappings[0].AbstractParamName);
        Assert.Equal(5000, fetched.DefaultHyperparams.Particles);
    }

    [Fact]
    public void P1_3_ApplicationDomain_roundtrip()
    {
        _db.Mapper.Entity<ApplicationDomain>().Id(x => x.IdJunction);
        var col = _db.GetCollection<ApplicationDomain>("ApplicationDomains");
        var entity = new ApplicationDomain { ApplicationId = 1, DomainId = 2 };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdJunction);
        Assert.NotNull(fetched);
        Assert.Equal(1, fetched.ApplicationId);
        Assert.Equal(2, fetched.DomainId);
    }

    [Fact]
    public void P1_3_MRInstance_with_full_overrides()
    {
        _db.Mapper.Entity<MRInstance>().Id(x => x.IdInstance);
        var col = _db.GetCollection<MRInstance>("MRInstances");
        var entity = new MRInstance
        {
            MRBindingId = 1,
            ParameterOverrides = new Dictionary<string, string> { ["factor"] = "1.5" },
            Sampling = new SamplingSpec { Distribution = "log-uniform", SampleCount = 5 },
            HyperparamsOverride = new SutHyperparams { Seed = 42, Particles = 50000 },
            ToleranceOverride = new ToleranceConfig { NoiseAware = true, ToleranceRel = 0.001 },
            IsReusable = true,
            Name = "MR-T@OpenMOC@factor=1.5@particles=50K",
            CreatedBy = "alice"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdInstance);
        Assert.NotNull(fetched);
        Assert.Equal("1.5", fetched.ParameterOverrides["factor"]);
        Assert.NotNull(fetched.Sampling);
        Assert.Equal("log-uniform", fetched.Sampling!.Distribution);
        Assert.Equal(42L, fetched.HyperparamsOverride!.Seed);
        Assert.True(fetched.IsReusable);
    }

    // ============================================================
    // P1.4 — 执行记录实体（Guid PK）
    // ============================================================

    [Fact]
    public void P1_4_Execution_Guid_PK_status_machine()
    {
        _db.Mapper.Entity<Execution>().Id(x => x.IdExecution);
        var col = _db.GetCollection<Execution>("Executions");
        var execId = Guid.NewGuid();
        var entity = new Execution
        {
            IdExecution = execId,
            MRInstanceId = 1,
            TriggeredBy = "ci-bot",
            QueuedAt = new DateTime(2026, 5, 13, 10, 0, 0, DateTimeKind.Utc),
            StartedAt = new DateTime(2026, 5, 13, 10, 0, 5, DateTimeKind.Utc),
            FinishedAt = new DateTime(2026, 5, 13, 10, 1, 25, DateTimeKind.Utc),
            Status = "anomaly",
            CatalogVersionSha = "abc123",
            SutVersionSnapshot = "openmoc-3D-MOC@a1f2b3c",
            MetbenchVersion = "v2.0.0",
            ArtifactsDirectory = "runtime/artifacts/2026/05/13/" + execId.ToString("N")
        };
        col.Insert(entity);
        var fetched = col.FindById(execId);
        Assert.NotNull(fetched);
        Assert.Equal("anomaly", fetched.Status);
        Assert.Equal("abc123", fetched.CatalogVersionSha);
        Assert.Equal("openmoc-3D-MOC@a1f2b3c", fetched.SutVersionSnapshot);
    }

    [Fact]
    public void P1_4_Result_metrics_dict_roundtrip()
    {
        _db.Mapper.Entity<Result>().Id(x => x.IdResult);
        var col = _db.GetCollection<Result>("Results");
        var entity = new Result
        {
            IdResult = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            SourceValue = 1.13306,
            SourceStd = 0.0,
            FollowupValue = 0.50781,
            FollowupStd = 0.0,
            SourceMetrics = new Dictionary<string, double>
            {
                ["k_eff"] = 1.13306, ["iterations"] = 553
            },
            FollowupMetrics = new Dictionary<string, double>
            {
                ["k_eff"] = 0.50781, ["iterations"] = 612
            },
            AssertionPassed = false,
            AssertionExpression = "0.50781 < 1.13306 - max(0, 0)",
            ObservedDelta = -0.62525,
            FailureReason = "k_followup drop exceeds physical expectation",
            SourceElapsed = TimeSpan.FromSeconds(30),
            FollowupElapsed = TimeSpan.FromSeconds(32),
            SourceExitCode = 0,
            FollowupExitCode = 0
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdResult);
        Assert.NotNull(fetched);
        Assert.False(fetched.AssertionPassed);
        Assert.Equal(1.13306, fetched.SourceValue);
        Assert.Equal(2, fetched.SourceMetrics.Count);
        Assert.Equal(1.13306, fetched.SourceMetrics["k_eff"]);
    }

    [Fact]
    public void P1_4_Anomaly_full_metadata()
    {
        _db.Mapper.Entity<Anomaly>().Id(x => x.IdAnomaly);
        var col = _db.GetCollection<Anomaly>("Anomalies");
        var entity = new Anomaly
        {
            IdAnomaly = Guid.NewGuid(),
            ResultId = Guid.NewGuid(),
            Severity = "major",
            Category = "basin",
            ReplayCount = 3,
            Status = "confirmed-bug",
            Notes = "OpenMOC CPUSolver 第二收敛盆，factor=1.25",
            LinkedKnownBugId = 6,
            DiscoveredAt = new DateTime(2026, 5, 12, 9, 30, 0, DateTimeKind.Utc),
            DiscoveredBy = "alice",
            LastReplayedAt = new DateTime(2026, 5, 13, 9, 30, 0, DateTimeKind.Utc)
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdAnomaly);
        Assert.NotNull(fetched);
        Assert.Equal("major", fetched.Severity);
        Assert.Equal("basin", fetched.Category);
        Assert.Equal(3, fetched.ReplayCount);
        Assert.Equal(6, fetched.LinkedKnownBugId);
    }

    // ============================================================
    // P1.5 — Discovery 实体
    // ============================================================

    [Fact]
    public void P1_5_DiscoveryMethod_roundtrip()
    {
        _db.Mapper.Entity<DiscoveryMethod>().Id(x => x.IdMethod);
        var col = _db.GetCollection<DiscoveryMethod>("DiscoveryMethods");
        var entity = new DiscoveryMethod
        {
            Name = "LLM-Native",
            Version = "deepseek-v4-pro@2026-05",
            ConfigJson = "{\"temperature\": 0.7}",
            Enabled = true,
            Description = "LLM 启发式 MR 识别"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdMethod);
        Assert.NotNull(fetched);
        Assert.Equal("LLM-Native", fetched.Name);
        Assert.True(fetched.Enabled);
    }

    [Fact]
    public void P1_5_DiscoveryRun_with_target_sut()
    {
        _db.Mapper.Entity<DiscoveryRun>().Id(x => x.IdRun);
        var col = _db.GetCollection<DiscoveryRun>("DiscoveryRuns");
        var entity = new DiscoveryRun
        {
            IdRun = Guid.NewGuid(),
            MethodId = 1,
            TargetApplicationId = 5,
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow.AddMinutes(10),
            Status = "ok",
            CandidatesProduced = 7
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdRun);
        Assert.NotNull(fetched);
        Assert.Equal(7, fetched.CandidatesProduced);
        Assert.Equal("ok", fetched.Status);
    }

    [Fact]
    public void P1_5_CandidateMR_promotion_path()
    {
        _db.Mapper.Entity<CandidateMR>().Id(x => x.IdCandidate);
        var col = _db.GetCollection<CandidateMR>("CandidateMRs");
        var entity = new CandidateMR
        {
            IdCandidate = Guid.NewGuid(),
            DiscoveryRunId = Guid.NewGuid(),
            ProposedCode = "MR-T-auto-001",
            ProposedName = "RaiseFuelTemperature (auto)",
            SuggestedTransformationName = "ScaleField",
            SuggestedAssertionTypeCode = "less-noise-aware",
            ProposedValueName = "k_eff",
            Rationale = "Doppler 展宽",
            Confidence = 0.85,
            Status = "promoted",
            PromotedToMRId = 42
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdCandidate);
        Assert.NotNull(fetched);
        Assert.Equal("promoted", fetched.Status);
        Assert.Equal(42, fetched.PromotedToMRId);
    }

    [Fact]
    public void P1_5_ValidationRun_pass_fail()
    {
        _db.Mapper.Entity<ValidationRun>().Id(x => x.IdValidation);
        var col = _db.GetCollection<ValidationRun>("ValidationRuns");
        var entity = new ValidationRun
        {
            IdValidation = Guid.NewGuid(),
            CandidateMRId = Guid.NewGuid(),
            ValidatorName = "empirical",
            RunAt = DateTime.UtcNow,
            Passed = true,
            DetailsJson = "{\"baseline_n\": 5, \"pass_rate\": 1.0}"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdValidation);
        Assert.NotNull(fetched);
        Assert.True(fetched.Passed);
        Assert.Equal("empirical", fetched.ValidatorName);
    }

    // ============================================================
    // P1.6 — Mutation 实体
    // ============================================================

    [Fact]
    public void P1_6_MutationOperator_categories()
    {
        _db.Mapper.Entity<MutationOperator>().Id(x => x.IdOperator);
        var col = _db.GetCollection<MutationOperator>("MutationOperators");
        var entity = new MutationOperator
        {
            Code = "Mut02-runner-sigt-from-siga",
            Category = "runner-level",
            TargetFileType = "<sut>_runner.py",
            ApplicationSpec = "sed 's/sigma_t/sigma_a/'",
            PredictedClass = "semantic",
            RelatedMetaPattern = "m_mono",
            Description = "把 sigma_t 取自 sigma_a，k_eff diverges to inf"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdOperator);
        Assert.NotNull(fetched);
        Assert.Equal("Mut02-runner-sigt-from-siga", fetched.Code);
        Assert.Equal("semantic", fetched.PredictedClass);
    }

    [Fact]
    public void P1_6_Mutant_with_sut_binding()
    {
        _db.Mapper.Entity<Mutant>().Id(x => x.IdMutant);
        var col = _db.GetCollection<Mutant>("Mutants");
        var entity = new Mutant
        {
            OperatorId = 2,
            ApplicationId = 5,
            AppliedDiff = "@@ ... @@\n-sigma_t = ...\n+sigma_t = sigma_a\n",
            Status = "active"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdMutant);
        Assert.NotNull(fetched);
        Assert.Equal("active", fetched.Status);
        Assert.Equal(2, fetched.OperatorId);
    }

    [Fact]
    public void P1_6_MutationCampaign_lifecycle()
    {
        _db.Mapper.Entity<MutationCampaign>().Id(x => x.IdCampaign);
        var col = _db.GetCollection<MutationCampaign>("MutationCampaigns");
        var entity = new MutationCampaign
        {
            IdCampaign = Guid.NewGuid(),
            Name = "Phase-2-2026-05-12",
            ScopeSpecJson = "{\"mutants\":\"all\",\"mrBindings\":\"all\"}",
            CatalogVersionSha = "abc123",
            StartedAt = DateTime.UtcNow,
            Status = "running",
            CreatedBy = "ci"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdCampaign);
        Assert.NotNull(fetched);
        Assert.Equal("running", fetched.Status);
        Assert.Equal("abc123", fetched.CatalogVersionSha);
    }

    [Fact]
    public void P1_6_MutationResult_outcomes()
    {
        _db.Mapper.Entity<MutationResult>().Id(x => x.IdMutationResult);
        var col = _db.GetCollection<MutationResult>("MutationResults");
        var entity = new MutationResult
        {
            IdMutationResult = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            MutantId = 2,
            MRBindingId = 7,
            ExecutionId = Guid.NewGuid(),
            Outcome = "detected",
            ObservedDelta = -0.65,
            ExpectedDelta = -0.02,
            Notes = "MR-T 成功检出"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdMutationResult);
        Assert.NotNull(fetched);
        Assert.Equal("detected", fetched.Outcome);
        Assert.Equal(-0.65, fetched.ObservedDelta);
    }

    // ============================================================
    // P1.7 — 杂项实体
    // ============================================================

    [Fact]
    public void P1_7_KnownBug_R_Case_record()
    {
        _db.Mapper.Entity<KnownBug>().Id(x => x.IdBug);
        var col = _db.GetCollection<KnownBug>("KnownBugs");
        var entity = new KnownBug
        {
            Code = "R-Case-6",
            Title = "OpenMOC CPUSolver basin @factor=1.25 fuel-temperature",
            RelatedApplicationId = 5,
            UpstreamFixCommit = null,
            Status = "open",
            Description = "Power-iteration narrow basin"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdBug);
        Assert.NotNull(fetched);
        Assert.Equal("R-Case-6", fetched.Code);
        Assert.Equal("open", fetched.Status);
    }

    [Fact]
    public void P1_7_AuditLog_action_record()
    {
        _db.Mapper.Entity<AuditLog>().Id(x => x.IdLog);
        var col = _db.GetCollection<AuditLog>("AuditLog");
        var entity = new AuditLog
        {
            IdLog = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Actor = "alice",
            Action = "mr.promote",
            TargetEntityType = "CandidateMR",
            TargetEntityId = Guid.NewGuid().ToString(),
            DetailsJson = "{\"reason\": \"all 3 validators passed\"}"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdLog);
        Assert.NotNull(fetched);
        Assert.Equal("mr.promote", fetched.Action);
        Assert.Equal("alice", fetched.Actor);
    }

    [Fact]
    public void P1_7_Batch_with_plan_reference()
    {
        _db.Mapper.Entity<Batch>().Id(x => x.IdBatch);
        var col = _db.GetCollection<Batch>("Batches");
        var entity = new Batch
        {
            IdBatch = Guid.NewGuid(),
            Name = "nightly-regression-2026-05-13",
            PlanId = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "scheduler",
            Status = "ok"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdBatch);
        Assert.NotNull(fetched);
        Assert.Equal("nightly-regression-2026-05-13", fetched.Name);
        Assert.Equal(1, fetched.PlanId);
    }

    [Fact]
    public void P1_7_BatchPlan_with_cron()
    {
        _db.Mapper.Entity<BatchPlan>().Id(x => x.IdPlan);
        var col = _db.GetCollection<BatchPlan>("BatchPlans");
        var entity = new BatchPlan
        {
            Name = "nightly-regression",
            DefinitionJson = "{\"mrBindings\":\"all\"}",
            Schedule = "0 2 * * *",
            Enabled = true
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdPlan);
        Assert.NotNull(fetched);
        Assert.Equal("0 2 * * *", fetched.Schedule);
        Assert.True(fetched.Enabled);
    }

    [Fact]
    public void P1_7_Report_scopes()
    {
        _db.Mapper.Entity<Report>().Id(x => x.IdReport);
        var col = _db.GetCollection<Report>("Reports");
        var entity = new Report
        {
            IdReport = Guid.NewGuid(),
            GeneratedAt = DateTime.UtcNow,
            Scope = "weekly",
            Format = "html",
            ContentPath = "runtime/reports/2026-W19.html",
            Notes = "MR pass rate 98.5%"
        };
        col.Insert(entity);
        var fetched = col.FindById(entity.IdReport);
        Assert.NotNull(fetched);
        Assert.Equal("weekly", fetched.Scope);
        Assert.Equal("html", fetched.Format);
    }
}
