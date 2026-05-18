using LiteDB;
using MetBench_BLL.Discovery;
using MetBench_DAL;
using MetBench_DAL.V2;
using MetBench_Domain;
using MetBench_IDAL;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Schema;

/// <summary>
/// TDD for MetaPattern 实体（v2 24-th collection）：
///   round-trip 入库 / Code 唯一性 / GetByStatus / Active 过滤 / Seed 8 个 NOETHER / DI 注册。
/// </summary>
[Collection("DbConfigGlobal")]
public sealed class MetaPatternEntityTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDbMetaPatternRepository _repo;

    public MetaPatternEntityTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchMetaPatternTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        // 直接传 conn，避开 DbConfig.Instance 在 Linux 上的 NRE（cold-start C5）
        _repo = new LiteDbMetaPatternRepository(_dbPath);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var logFile = _dbPath + "-log";
        if (File.Exists(logFile)) File.Delete(logFile);
    }

    [Fact]
    public void Add_then_Get_roundtrips_all_fields()
    {
        var entity = new MetaPattern
        {
            Code = "m_inv", Name = "Invariance", Block = "B1-symmetry",
            DefaultAssertionTypeCode = "approx-invariant", DefaultToleranceRel = 0.0001,
            NoiseAware = false,
            HypothesisTemplate = "k_followup ≈ k_source within ε",
            ExampleParamHints = new() { "geometry.rotation_deg", "geometry.mirror_axis" },
            Status = "active",
            CreatedBy = "test",
        };
        Assert.True(_repo.Add(entity));

        var fetched = _repo.Get("m_inv");
        Assert.NotNull(fetched);
        Assert.Equal("Invariance", fetched!.Name);
        Assert.Equal("B1-symmetry", fetched.Block);
        Assert.Equal(0.0001, fetched.DefaultToleranceRel);
        Assert.False(fetched.NoiseAware);
        Assert.Equal(2, fetched.ExampleParamHints.Count);
        Assert.Contains("geometry.rotation_deg", fetched.ExampleParamHints);
    }

    [Fact]
    public void Add_duplicate_code_throws()
    {
        _repo.Add(new MetaPattern { Code = "m_inv", Name = "first" });
        Assert.ThrowsAny<Exception>(() =>
            _repo.Add(new MetaPattern { Code = "m_inv", Name = "second" }));
    }

    [Fact]
    public void GetByStatus_filters_correctly()
    {
        _repo.Add(new MetaPattern { Code = "m_a", Status = "active" });
        _repo.Add(new MetaPattern { Code = "m_b", Status = "out-of-scope" });
        _repo.Add(new MetaPattern { Code = "m_c", Status = "active" });

        var active = _repo.GetByStatus("active");
        var oos = _repo.GetByStatus("out-of-scope");
        Assert.Equal(2, active.Count);
        Assert.Single(oos);
    }

    [Fact]
    public void GetActive_is_shorthand_for_active_status()
    {
        _repo.Add(new MetaPattern { Code = "m_a", Status = "active" });
        _repo.Add(new MetaPattern { Code = "m_b", Status = "deprecated" });

        var active = _repo.GetActive();
        Assert.Single(active);
        Assert.Equal("m_a", active[0].Code);
    }

    [Fact]
    public void GetByBlock_filters_by_noether_block()
    {
        _repo.Add(new MetaPattern { Code = "m_inv", Block = "B1-symmetry" });
        _repo.Add(new MetaPattern { Code = "m_mono", Block = "B2-order" });
        _repo.Add(new MetaPattern { Code = "m_conv", Block = "B5-limit" });

        var b2 = _repo.GetByBlock("B2-order");
        Assert.Single(b2);
        Assert.Equal("m_mono", b2[0].Code);
    }

    [Fact]
    public void Modify_updates_existing_row()
    {
        _repo.Add(new MetaPattern { Code = "m_inv", Status = "active" });

        var entity = _repo.Get("m_inv")!;
        entity.Status = "deprecated";
        entity.OutOfScopeReason = "replaced by new symmetry framework";
        Assert.True(_repo.Modify(entity));

        var fetched = _repo.Get("m_inv");
        Assert.Equal("deprecated", fetched!.Status);
        Assert.Contains("symmetry framework", fetched.OutOfScopeReason);
    }

    [Fact]
    public void Remove_deletes_row()
    {
        _repo.Add(new MetaPattern { Code = "m_x" });
        Assert.True(_repo.Remove(new MetaPattern { Code = "m_x" }));
        Assert.Null(_repo.Get("m_x"));
    }

    // ===== Seed integration =====

    [Fact]
    public void EnsureSeeded_inserts_eight_noether_metapatterns_in_empty_db()
    {
        var inserted = MetaPatternSeed.EnsureSeeded(_repo);
        Assert.Equal(8, inserted);
        Assert.Equal(8, _repo.Count());

        // 4 active + 4 out-of-scope
        Assert.Equal(4, _repo.GetActive().Count);
        Assert.Equal(4, _repo.GetByStatus("out-of-scope").Count);
    }

    [Fact]
    public void EnsureSeeded_is_idempotent_when_already_populated()
    {
        MetaPatternSeed.EnsureSeeded(_repo);
        var insertedSecondTime = MetaPatternSeed.EnsureSeeded(_repo);
        Assert.Equal(0, insertedSecondTime);
        Assert.Equal(8, _repo.Count());
    }

    [Fact]
    public void Seeded_set_contains_all_four_active_metapatterns()
    {
        MetaPatternSeed.EnsureSeeded(_repo);
        foreach (var code in new[] { "m_inv", "m_mono", "m_conv", "m_cmp" })
        {
            var mp = _repo.Get(code);
            Assert.NotNull(mp);
            Assert.Equal("active", mp!.Status);
            Assert.False(string.IsNullOrEmpty(mp.DefaultAssertionTypeCode));
            Assert.False(string.IsNullOrEmpty(mp.HypothesisTemplate));
        }
    }

    [Fact]
    public void Seeded_out_of_scope_set_includes_adjoint_reversal_dynamics_idempotent()
    {
        MetaPatternSeed.EnsureSeeded(_repo);
        foreach (var code in new[] { "m_adj", "m_rev", "m_dyn", "m_rel" })
        {
            var mp = _repo.Get(code);
            Assert.NotNull(mp);
            Assert.Equal("out-of-scope", mp!.Status);
            Assert.False(string.IsNullOrEmpty(mp.OutOfScopeReason),
                $"out-of-scope MetaPattern {code} must explain why");
        }
    }

    // ===== DI registration =====

    [Fact]
    public void AddSystemMtRepositories_registers_IMetaPatternRepository()
    {
        var services = new ServiceCollection();
        services.AddSystemMtRepositories();
        var has = services.Any(d =>
            d.ServiceType == typeof(IMetaPatternRepository) &&
            d.ImplementationType == typeof(LiteDbMetaPatternRepository));
        Assert.True(has, "AddSystemMtRepositories should register IMetaPatternRepository → LiteDbMetaPatternRepository");
    }
}
