using MetBench_BLL.SystemMT.Catalog;
using MetBench_Domain;
using MetBench_SystemMT.Tests.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog;

/// <summary>
/// 方向 3 — SystemMtCatalogService:v2 系统级 MR / SUT / Binding 的 CRUD 入口,
/// 按 Kind="system-level" 过滤、唯一性校验、删除安全检查、写审计。
/// </summary>
public sealed class SystemMtCatalogServiceTests
{
    private readonly LauncherCatalogV2ImporterTests.FakeImporterAppRepo _apps = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterMrRepo _mrs = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterBindingRepo _bindings = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterAuditRepo _audit = new();
    private readonly SystemMtCatalogService _svc;

    public SystemMtCatalogServiceTests()
    {
        _svc = new SystemMtCatalogService(_apps, _mrs, _bindings, _audit);
    }

    // ==================== SUT CRUD ====================

    [Fact]
    public void ListSuts_filters_by_system_level_kind()
    {
        SeedMethodLevelApp("legacy-app");
        SeedSystemSut("openmoc");
        SeedSystemSut("openmc");

        var suts = _svc.ListSuts();

        Assert.Equal(2, suts.Count);
        Assert.All(suts, s => Assert.Equal("system-level", s.Kind));
    }

    [Fact]
    public void FindSutByName_only_finds_system_level()
    {
        SeedMethodLevelApp("openmoc");  // 同名但 v1
        var sysId = SeedSystemSut("openmoc");

        var sut = _svc.FindSutByName("openmoc");

        Assert.NotNull(sut);
        Assert.Equal(sysId, sut.IdApplication);
        Assert.Equal("system-level", sut.Kind);
    }

    [Fact]
    public void CreateSut_assigns_system_level_kind_and_writes_audit()
    {
        var sut = new Application { Name = "newsut", ProgrammingLanguage = "Python" };

        var id = _svc.CreateSut(sut, actor: "alice");

        Assert.True(id > 0);
        Assert.Equal("system-level", sut.Kind);
        var log = Assert.Single(_audit.Data);
        Assert.Equal("sut.create", log.Action);
        Assert.Equal("alice", log.Actor);
    }

    [Fact]
    public void CreateSut_rejects_duplicate_name()
    {
        _svc.CreateSut(new Application { Name = "openmoc" });
        Assert.Throws<InvalidOperationException>(() =>
            _svc.CreateSut(new Application { Name = "openmoc" }));
    }

    [Fact]
    public void CreateSut_rejects_blank_name()
    {
        Assert.Throws<ArgumentException>(() =>
            _svc.CreateSut(new Application { Name = "" }));
        Assert.Throws<ArgumentException>(() =>
            _svc.CreateSut(new Application { Name = "   " }));
    }

    [Fact]
    public void UpdateSut_changes_description_preserves_kind()
    {
        var id = _svc.CreateSut(new Application { Name = "openmoc" });
        var sut = _svc.GetSut(id)!;
        sut.Description = "updated description";

        Assert.True(_svc.UpdateSut(sut));
        Assert.Equal("updated description", _svc.GetSut(id)!.Description);
    }

    [Fact]
    public void UpdateSut_rejects_rename_collision()
    {
        var id1 = _svc.CreateSut(new Application { Name = "openmoc" });
        var id2 = _svc.CreateSut(new Application { Name = "openmc" });
        var sut2 = _svc.GetSut(id2)!;
        sut2.Name = "openmoc";  // 撞名

        Assert.Throws<InvalidOperationException>(() => _svc.UpdateSut(sut2));
    }

    [Fact]
    public void DeleteSut_succeeds_when_no_bindings()
    {
        var id = _svc.CreateSut(new Application { Name = "openmoc" });

        var outcome = _svc.DeleteSut(id);

        Assert.Equal("deleted", outcome.Status);
        Assert.Null(_svc.GetSut(id));
    }

    [Fact]
    public void DeleteSut_blocks_when_bindings_exist()
    {
        var sutId = _svc.CreateSut(new Application { Name = "openmoc" });
        var mrId = _svc.CreateMr(new MetamorphicRelation { Code = "MR-1" });
        _svc.BindMrToSut(mrId, sutId, "sample.json");

        var outcome = _svc.DeleteSut(sutId);

        Assert.Equal("blocked", outcome.Status);
        Assert.Contains("binding", outcome.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(_svc.GetSut(sutId));  // 还在
    }

    [Fact]
    public void DeleteSut_returns_NotFound_for_unknown_id()
    {
        Assert.Equal("not-found", _svc.DeleteSut(999).Status);
    }

    // ==================== MR CRUD ====================

    [Fact]
    public void ListMrs_filters_by_system_level_kind()
    {
        SeedMethodLevelMr("legacy-mr");
        _svc.CreateMr(new MetamorphicRelation { Code = "MR-1" });
        _svc.CreateMr(new MetamorphicRelation { Code = "MR-2" });

        var mrs = _svc.ListMrs();

        Assert.Equal(2, mrs.Count);
        Assert.All(mrs, m => Assert.Equal("system-level", m.Kind));
    }

    [Fact]
    public void CreateMr_assigns_kind_and_default_audit()
    {
        var id = _svc.CreateMr(new MetamorphicRelation { Code = "MR-Test" }, actor: "alice");

        var mr = _svc.GetMr(id)!;
        Assert.Equal("system-level", mr.Kind);
        Assert.Equal("alice", mr.CreatedBy);
        Assert.NotEqual(default, mr.CreatedAt);
        var log = Assert.Single(_audit.Data);
        Assert.Equal("mr.create", log.Action);
    }

    [Fact]
    public void CreateMr_rejects_duplicate_code()
    {
        _svc.CreateMr(new MetamorphicRelation { Code = "MR-1" });
        Assert.Throws<InvalidOperationException>(() =>
            _svc.CreateMr(new MetamorphicRelation { Code = "MR-1" }));
    }

    [Fact]
    public void DeleteMr_blocks_when_bindings_exist()
    {
        var sutId = _svc.CreateSut(new Application { Name = "openmoc" });
        var mrId = _svc.CreateMr(new MetamorphicRelation { Code = "MR-1" });
        _svc.BindMrToSut(mrId, sutId, "sample.json");

        var outcome = _svc.DeleteMr(mrId);

        Assert.Equal("blocked", outcome.Status);
        Assert.NotNull(_svc.GetMr(mrId));
    }

    [Fact]
    public void DeleteMr_succeeds_after_unbinding()
    {
        var sutId = _svc.CreateSut(new Application { Name = "openmoc" });
        var mrId = _svc.CreateMr(new MetamorphicRelation { Code = "MR-1" });
        _svc.BindMrToSut(mrId, sutId, "sample.json");
        Assert.True(_svc.UnbindMrFromSut(mrId, sutId));

        var outcome = _svc.DeleteMr(mrId);

        Assert.Equal("deleted", outcome.Status);
        Assert.Null(_svc.GetMr(mrId));
    }

    // ==================== Binding CRUD ====================

    [Fact]
    public void BindMrToSut_creates_new_binding_with_active_status()
    {
        var sutId = _svc.CreateSut(new Application { Name = "openmoc" });
        var mrId = _svc.CreateMr(new MetamorphicRelation { Code = "MR-1" });

        var outcome = _svc.BindMrToSut(mrId, sutId, "sample.json", actor: "alice");

        Assert.Equal("created", outcome.Status);
        Assert.NotNull(outcome.BindingId);
        var binding = Assert.Single(_bindings.Data);
        Assert.Equal("active", binding.Status);
        Assert.Equal("sample.json", binding.DefaultSampleCasePath);
        Assert.Equal("alice", binding.BoundBy);
    }

    [Fact]
    public void BindMrToSut_is_idempotent_returns_existing_on_duplicate()
    {
        var sutId = _svc.CreateSut(new Application { Name = "openmoc" });
        var mrId = _svc.CreateMr(new MetamorphicRelation { Code = "MR-1" });
        var first = _svc.BindMrToSut(mrId, sutId, "a.json");

        var second = _svc.BindMrToSut(mrId, sutId, "b.json");

        Assert.Equal("created", first.Status);
        Assert.Equal("existed", second.Status);
        Assert.Equal(first.BindingId, second.BindingId);
        Assert.Single(_bindings.Data);
    }

    [Fact]
    public void BindMrToSut_errors_when_mr_missing()
    {
        var sutId = _svc.CreateSut(new Application { Name = "openmoc" });
        var outcome = _svc.BindMrToSut(mrId: 999, sutId, "x.json");
        Assert.Equal("error", outcome.Status);
        Assert.Contains("MR", outcome.Error!);
    }

    [Fact]
    public void BindMrToSut_errors_when_sut_missing()
    {
        var mrId = _svc.CreateMr(new MetamorphicRelation { Code = "MR-1" });
        var outcome = _svc.BindMrToSut(mrId, sutId: 999, "x.json");
        Assert.Equal("error", outcome.Status);
        Assert.Contains("SUT", outcome.Error!);
    }

    [Fact]
    public void Constructor_rejects_null_args()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtCatalogService(null!, _mrs, _bindings, _audit));
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtCatalogService(_apps, null!, _bindings, _audit));
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtCatalogService(_apps, _mrs, null!, _audit));
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMtCatalogService(_apps, _mrs, _bindings, null!));
    }

    // ==================== seed helpers ====================

    private int SeedSystemSut(string name)
    {
        var sut = new Application { Name = name, Kind = "system-level", ProgrammingLanguage = "Python" };
        _apps.Add(sut);
        return sut.IdApplication;
    }

    private void SeedMethodLevelApp(string name)
    {
        _apps.Add(new Application { Name = name, Kind = "method-level" });
    }

    private void SeedMethodLevelMr(string code)
    {
        _mrs.Add(new MetamorphicRelation { Code = code, Kind = "method-level" });
    }
}
