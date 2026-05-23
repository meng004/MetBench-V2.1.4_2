using MetBench_BLL.SystemMT.Catalog;
using MetBench_Domain;
using MetBench_SystemMT.Tests.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.MethodMT;

/// <summary>
/// G-06: MethodMtCatalogService 的 MR CRUD（决策 1：method MR 必须持久化）。
/// </summary>
public sealed class MethodMtCatalogCrudTests
{
    private readonly LauncherCatalogV2ImporterTests.FakeImporterAppRepo _apps = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterMrRepo _mrs = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterBindingRepo _bindings = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterAuditRepo _audit = new();

    private MethodMtCatalogService MakeSvc() => new(_apps, _mrs, _bindings, _audit);

    private static MetamorphicRelation MakeMr(string code, string eqKey = "bateman") => new()
    {
        Code = code,
        EquationKey = eqKey,
        TransformationName = "ScaleField",
        AssertionTypeCode = "greater",
        ValueName = "N_C",
        Description = $"test MR {code}",
        Context = "",
        Constraint = "",
        OrderOfMR = "",
        InputPattern = "",
        OutputPattern = "",
        InputPatterntosympy = "",
        OutputPatterntosympy = "",
        DimensionOfInputPattern = "",
        DimensionOfOutputPattern = "",
        Granularity = "",
        Hierarchy = "",
        Operator = "",
        Expression = "",
    };

    [Fact]
    public void CreateMr_persists_with_kind_method_level_and_returns_id()
    {
        var svc = MakeSvc();
        var mr = MakeMr("mt-bateman-x2");

        var id = svc.CreateMr(mr, "alice");

        Assert.True(id > 0);
        var back = svc.FindMrByCode("mt-bateman-x2");
        Assert.NotNull(back);
        Assert.Equal("method-level", back!.Kind);
        Assert.Equal("alice", back.CreatedBy);
        // audit
        Assert.Single(_audit.Data);
        Assert.Equal("mr.create", _audit.Data[0].Action);
    }

    [Fact]
    public void CreateMr_rejects_non_empty_MetaPatternCode()
    {
        var svc = MakeSvc();
        var mr = MakeMr("mt-meta-rejected");
        mr.MetaPatternCode = "m_mono";

        var ex = Assert.Throws<ArgumentException>(() => svc.CreateMr(mr));
        Assert.Contains("MetaPatternCode", ex.Message);
    }

    [Fact]
    public void CreateMr_rejects_duplicate_code()
    {
        var svc = MakeSvc();
        svc.CreateMr(MakeMr("mt-dup"));

        var ex = Assert.Throws<InvalidOperationException>(() => svc.CreateMr(MakeMr("mt-dup")));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void GetMr_returns_only_method_level()
    {
        var svc = MakeSvc();
        var id = svc.CreateMr(MakeMr("mt-get"));
        // 在 _mrs 直接塞一条 system-level，确认 service 不返回
        _mrs.Add(new MetamorphicRelation
        {
            Code = "sys-foo", Kind = "system-level",
            Description = "", Context = "", Constraint = "", OrderOfMR = "",
            InputPattern = "", OutputPattern = "",
            InputPatterntosympy = "", OutputPatterntosympy = "",
            DimensionOfInputPattern = "", DimensionOfOutputPattern = "",
            Granularity = "", Hierarchy = "", Operator = "", Expression = "",
        });

        Assert.NotNull(svc.GetMr(id));
        Assert.Null(svc.GetMr(_mrs.Data.First(m => m.Code == "sys-foo").IdMR));
    }

    [Fact]
    public void ListMrs_filters_kind()
    {
        var svc = MakeSvc();
        svc.CreateMr(MakeMr("mt-a"));
        svc.CreateMr(MakeMr("mt-b"));
        _mrs.Add(new MetamorphicRelation
        {
            Code = "sys-x", Kind = "system-level",
            Description = "", Context = "", Constraint = "", OrderOfMR = "",
            InputPattern = "", OutputPattern = "",
            InputPatterntosympy = "", OutputPatterntosympy = "",
            DimensionOfInputPattern = "", DimensionOfOutputPattern = "",
            Granularity = "", Hierarchy = "", Operator = "", Expression = "",
        });

        var list = svc.ListMrs();

        Assert.Equal(2, list.Count);
        Assert.All(list, m => Assert.Equal("method-level", m.Kind));
    }

    [Fact]
    public void UpdateMr_modifies_existing_and_audits()
    {
        var svc = MakeSvc();
        var id = svc.CreateMr(MakeMr("mt-update"));
        var mr = svc.GetMr(id)!;
        mr.Description = "updated";

        var ok = svc.UpdateMr(mr, "bob");

        Assert.True(ok);
        Assert.Contains(_audit.Data, l => l.Action == "mr.update");
    }

    [Fact]
    public void UpdateMr_rejects_non_method_kind()
    {
        var svc = MakeSvc();
        var id = svc.CreateMr(MakeMr("mt-bad-kind"));
        var mr = svc.GetMr(id)!;
        mr.Kind = "system-level";  // 篡改

        Assert.Throws<ArgumentException>(() => svc.UpdateMr(mr));
    }

    [Fact]
    public void DeleteMr_removes_and_audits()
    {
        var svc = MakeSvc();
        var id = svc.CreateMr(MakeMr("mt-del"));

        var outcome = svc.DeleteMr(id, "carol");

        Assert.Equal(DeleteOutcome.Deleted, outcome);
        Assert.Null(svc.GetMr(id));
        Assert.Contains(_audit.Data, l => l.Action == "mr.delete");
    }

    [Fact]
    public void DeleteMr_not_found_returns_NotFound()
    {
        var svc = MakeSvc();

        var outcome = svc.DeleteMr(9999);

        Assert.Equal(DeleteOutcome.NotFound, outcome);
    }
}
