using System.Text.Json;
using LiteDB;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.ColdStart;

/// <summary>
/// 冷启动集成测试 —— 5 anchor 案例端到端入库 + 查询验证。
///
/// 模拟一个全新部署 MetBench v2 的场景：空 LiteDB → 按 anchors.json 注入
///   Applications(openmoc, openmc)
///   MetamorphicRelations(5 个 anchor MR)
///   MRBindings(笛卡尔积)
///   KnownBugs(R-Case-4 复现锚点)
/// 然后做一系列查询验证数据一致性。
///
/// 不依赖 DbConfig.Instance —— 避开 v1 ConfigurationManager Windows-only 路径
/// （cold-start finding C5）；用 LiteDatabase + 临时文件做隔离。
/// </summary>
[Collection("DbConfigGlobal")]
public sealed class ColdStartIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ILiteDatabase _db;

    public ColdStartIntegrationTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchColdStartTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _db = new LiteDatabase(_dbPath, new BsonMapper());

        // 显式 PK 映射（避免依赖 reflection convention）
        _db.Mapper.Entity<Application>().Id(x => x.IdApplication);
        _db.Mapper.Entity<MetamorphicRelation>().Id(x => x.IdMR);
        _db.Mapper.Entity<MRBinding>().Id(x => x.IdMRBinding);
        _db.Mapper.Entity<KnownBug>().Id(x => x.IdBug);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var logFile = _dbPath + "-log";
        if (File.Exists(logFile)) File.Delete(logFile);
    }

    /// <summary>5 个 anchor (跟 cold_start_demo/anchors/anchors.json 同源).</summary>
    private static readonly (string Code, string MetaPattern, string Assertion, string[] Suts)[] Anchors =
    {
        ("MR01-inv-quarter-rotation-90", "m_inv",  "approx-invariant", new[]{"openmoc","openmc"}),
        ("MR-NuSigmaF",                 "m_mono", "greater",          new[]{"openmoc","openmc"}),
        ("MR12-conv-particles-refine",  "m_conv", "bound",            new[]{"openmc"}),
        ("MR14-cmp-openmoc-vs-openmc",  "m_cmp",  "bound",            new[]{"openmoc","openmc"}),
        ("MR15-cmp-p0-vs-p1-scattering","m_cmp",  "bound",            new[]{"openmc"}),
    };

    // ===== 测试 1：冷启动入库不抛 =====

    [Fact]
    public void ColdStart_inserts_applications_mrs_bindings_known_bugs_without_throw()
    {
        var (appCount, mrCount, bindingCount, bugCount) = LoadAnchors();
        Assert.Equal(2, appCount);
        Assert.Equal(5, mrCount);
        // anchor SUT 计数：A1(2) + A2(2) + A3(1) + A4(2) + A5(1) = 8 binding rows
        Assert.Equal(8, bindingCount);
        Assert.Equal(1, bugCount); // R-Case-4
    }

    // ===== 测试 2：查询 Applications 能拿回去 =====

    [Fact]
    public void After_cold_start_can_query_applications_by_name()
    {
        LoadAnchors();
        var col = _db.GetCollection<Application>("Applications");
        var openmoc = col.FindOne(a => a.Name == "openmoc");
        Assert.NotNull(openmoc);
        Assert.Equal("openmoc", openmoc!.Name);
        var openmc = col.FindOne(a => a.Name == "openmc");
        Assert.NotNull(openmc);
    }

    // ===== 测试 3：MR 按 MetaPattern 查询 =====

    [Fact]
    public void After_cold_start_can_query_mrs_by_metapattern()
    {
        LoadAnchors();
        var col = _db.GetCollection<MetamorphicRelation>("MetamorphicRelations");
        var monoMrs = col.Find(m => m.MetaPatternCode == "m_mono").ToList();
        var cmpMrs = col.Find(m => m.MetaPatternCode == "m_cmp").ToList();
        var invMrs = col.Find(m => m.MetaPatternCode == "m_inv").ToList();

        Assert.Single(monoMrs);  // MR-NuSigmaF
        Assert.Equal(2, cmpMrs.Count);  // MR14, MR15
        Assert.Single(invMrs);  // MR-Rot90
    }

    // ===== 测试 4：MRBinding 反查 MR + Application =====

    [Fact]
    public void Bindings_correctly_reference_mr_and_application_ids()
    {
        LoadAnchors();
        var mrCol = _db.GetCollection<MetamorphicRelation>("MetamorphicRelations");
        var appCol = _db.GetCollection<Application>("Applications");
        var bindingCol = _db.GetCollection<MRBinding>("MRBindings");

        var nu = mrCol.FindOne(m => m.Code == "MR-NuSigmaF");
        var openmocBindings = bindingCol.Find(b => b.MRId == nu!.IdMR).ToList();
        Assert.Equal(2, openmocBindings.Count);  // openmoc + openmc

        var openmoc = appCol.FindOne(a => a.Name == "openmoc");
        var nuOpenmoc = bindingCol.FindOne(b => b.MRId == nu!.IdMR && b.ApplicationId == openmoc!.IdApplication);
        Assert.NotNull(nuOpenmoc);
    }

    // ===== 测试 5：KnownBug R-Case-4 入库 + Code 查询 =====

    [Fact]
    public void KnownBug_R_Case_4_inserted_and_findable_by_code()
    {
        LoadAnchors();
        var col = _db.GetCollection<KnownBug>("KnownBugs");
        var bug = col.FindOne(b => b.Code == "R-Case-4");
        Assert.NotNull(bug);
        Assert.Equal("openmoc", FindAppName(bug!.RelatedApplicationId));
        Assert.Contains("narrow basin", bug.Description, StringComparison.OrdinalIgnoreCase);
    }

    // ===== 测试 6：bug C1 暴露 —— m_cmp 的 binding 模型不完整 =====

    [Fact]
    public void Cold_start_finding_C1_m_cmp_binding_arity_documented()
    {
        // m_cmp 一个 MR 需要"对比两个 SUT"，但 MRBinding (MRId, ApplicationId) 是单 SUT 二元约束。
        // 本测试明示当前数据模型把 m_cmp MR 当成两条独立 binding 处理（一行 openmoc，一行 openmc），
        // 上层算法需自行 group 起来 → 提醒 follow-up F16 需要正式建模。
        LoadAnchors();
        var mrCol = _db.GetCollection<MetamorphicRelation>("MetamorphicRelations");
        var bindingCol = _db.GetCollection<MRBinding>("MRBindings");
        var mr14 = mrCol.FindOne(m => m.Code == "MR14-cmp-openmoc-vs-openmc");
        var mr14Bindings = bindingCol.Find(b => b.MRId == mr14!.IdMR).ToList();
        Assert.Equal(2, mr14Bindings.Count); // 两条 binding，分别指向 openmoc + openmc，但缺少 SutPair 字段
    }

    // ===== 工具方法 =====

    /// <summary>把 5 anchor + R-Case-4 注入空库；返回 (appCount, mrCount, bindingCount, bugCount).</summary>
    private (int AppCount, int MrCount, int BindingCount, int BugCount) LoadAnchors()
    {
        var appCol = _db.GetCollection<Application>("Applications");
        var mrCol = _db.GetCollection<MetamorphicRelation>("MetamorphicRelations");
        var bindingCol = _db.GetCollection<MRBinding>("MRBindings");
        var bugCol = _db.GetCollection<KnownBug>("KnownBugs");

        // 1. Applications
        var apps = new Dictionary<string, Application>();
        foreach (var name in new[] { "openmoc", "openmc" })
        {
            var app = new Application
            {
                Name = name, CodeName = name, SourceTestCaseName = $"{name}_pincell",
                DomainName = "neutron-transport",
            };
            appCol.Insert(app);
            apps[name] = app;
        }

        // 2. MetamorphicRelations + 3. MRBindings
        foreach (var (code, mp, assertion, suts) in Anchors)
        {
            var mr = new MetamorphicRelation
            {
                Code = code,
                Description = $"Cold-start anchor — {code}",
                MetaPatternCode = mp,
                AssertionTypeCode = assertion,
                Context = "", Constraint = "", OrderOfMR = "",
                InputPattern = "", OutputPattern = "",
                InputPatterntosympy = "", OutputPatterntosympy = "",
                DimensionOfInputPattern = "", DimensionOfOutputPattern = "",
                Granularity = "", Hierarchy = "", Operator = "", Expression = "",
                InputPatternImageData = Array.Empty<byte>(),
                OutputPatternImageData = Array.Empty<byte>(),
            };
            mrCol.Insert(mr);

            foreach (var sutName in suts)
            {
                bindingCol.Insert(new MRBinding
                {
                    MRId = mr.IdMR,
                    ApplicationId = apps[sutName].IdApplication,
                    DefaultSampleCasePath = $"SUT/{sutName}/sample/pincell.json",
                    IsActive = true,
                });
            }
        }

        // 4. KnownBug R-Case-4
        bugCol.Insert(new KnownBug
        {
            Code = "R-Case-4",
            Title = "OpenMOC CPUSolver power-iter basin @ moderator-σ_a factor=1.5",
            RelatedApplicationId = apps["openmoc"].IdApplication,
            Status = "open",
            Description = "MetBench self-discovery: OpenMOC k=0.4764 vs OpenMC k=0.9683 — 51% Δ, narrow basin in power iteration.",
        });

        return (appCol.Count(), mrCol.Count(), bindingCol.Count(), bugCol.Count());
    }

    private string FindAppName(int? appId)
    {
        if (appId is null) return "";
        var app = _db.GetCollection<Application>("Applications").FindById(appId.Value);
        return app?.Name ?? "";
    }
}
