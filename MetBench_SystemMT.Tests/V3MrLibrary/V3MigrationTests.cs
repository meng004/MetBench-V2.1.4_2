using MetBench_BLL.SystemMT.Migrations;
using MetBench_Domain;
using MetBench_Domain.V2.Enums;
using MetBench_SystemMT.Tests.SystemMT.Launcher;
using Xunit;

namespace MetBench_SystemMT.Tests.V3MrLibrary;

/// <summary>
/// S8-P5c: V2 MetamorphicRelation → V3 MetamorphicRelationV3 投影迁移测试。
/// </summary>
public sealed class V3MigrationTests
{
    private readonly LauncherCatalogV2ImporterTests.FakeImporterMrRepo _v2 = new();
    private readonly FakeV3Repo _v3 = new();

    private static MetamorphicRelation MakeV2(
        string code,
        string equationKey = "bateman",
        string metaPattern = "m_mono",
        string kind = "system-level",
        string discovery = "manual",
        string assertion = "greater")
        => new()
        {
            Code = code, Kind = kind, EquationKey = equationKey,
            MetaPatternCode = metaPattern, DiscoveryMethod = discovery,
            AssertionTypeCode = assertion, ValueName = "x",
            Description = $"v2 MR {code}",
            Context = "", Constraint = "", OrderOfMR = "",
            InputPattern = "", OutputPattern = "",
            InputPatterntosympy = "", OutputPatterntosympy = "",
            DimensionOfInputPattern = "", DimensionOfOutputPattern = "",
            Granularity = "", Hierarchy = "", Operator = "", Expression = "",
            ApplicationName = "decay-chain",
        };

    [Fact]
    public void MigrateAll_creates_V3_rows_for_each_system_level_V2()
    {
        _v2.Add(MakeV2("a"));
        _v2.Add(MakeV2("b", equationKey: "heat-equation-1d", metaPattern: "m_conv"));

        var summary = V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);

        Assert.Equal(2, summary.Created);
        Assert.Equal(0, summary.Updated);
        Assert.Equal(0, summary.SkippedNonSystem);
        Assert.Equal(2, _v3.Data.Count);
    }

    [Fact]
    public void MigrateAll_skips_method_level_rows()
    {
        _v2.Add(MakeV2("sys", kind: "system-level"));
        _v2.Add(MakeV2("method", kind: "method-level"));

        var summary = V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);

        Assert.Equal(1, summary.Created);
        Assert.Equal(1, summary.SkippedNonSystem);
        Assert.Single(_v3.Data);
        Assert.Equal("sys", _v3.Data[0].MrCode);
    }

    [Fact]
    public void MigrateAll_is_idempotent_on_second_run()
    {
        _v2.Add(MakeV2("a"));

        var first = V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);
        var second = V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);

        Assert.Equal(1, first.Created);
        Assert.Equal(0, first.Updated);
        Assert.Equal(0, second.Created);
        Assert.Equal(1, second.Updated);
        Assert.Single(_v3.Data);
    }

    [Fact]
    public void MapEquation_maps_known_keys_else_Other_with_empty_as_Unspecified()
    {
        _v2.Add(MakeV2("a", equationKey: "bateman"));
        _v2.Add(MakeV2("b", equationKey: "heat-equation-1d"));
        _v2.Add(MakeV2("c", equationKey: "neutron-transport"));
        _v2.Add(MakeV2("d", equationKey: "diffusion"));
        _v2.Add(MakeV2("e", equationKey: "navier-stokes"));
        _v2.Add(MakeV2("f", equationKey: ""));
        _v2.Add(MakeV2("g", equationKey: "projectile-motion"));

        V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);

        Assert.Equal(EquationKind.Bateman, _v3.Data.Single(m => m.MrCode == "a").Equation);
        Assert.Equal(EquationKind.Fourier, _v3.Data.Single(m => m.MrCode == "b").Equation);
        Assert.Equal(EquationKind.Boltzmann, _v3.Data.Single(m => m.MrCode == "c").Equation);
        Assert.Equal(EquationKind.Diffusion, _v3.Data.Single(m => m.MrCode == "d").Equation);
        Assert.Equal(EquationKind.NavierStokes, _v3.Data.Single(m => m.MrCode == "e").Equation);
        // S8-P5 review fix: 空 EquationKey → Unspecified（"未指定"），不是 Other（"非反应堆物理"）
        Assert.Equal(EquationKind.Unspecified, _v3.Data.Single(m => m.MrCode == "f").Equation);
        // 未知 key 仍 → Other
        Assert.Equal(EquationKind.Other, _v3.Data.Single(m => m.MrCode == "g").Equation);
    }

    [Fact]
    public void MapMetaPattern_maps_strings_to_enum_else_Unspecified()
    {
        _v2.Add(MakeV2("a", metaPattern: "m_mono"));
        _v2.Add(MakeV2("b", metaPattern: "m_inv"));
        _v2.Add(MakeV2("c", metaPattern: "m_conv"));
        _v2.Add(MakeV2("d", metaPattern: ""));

        V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);

        Assert.Equal(MetaPatternKind.Mono, _v3.Data.Single(m => m.MrCode == "a").MetaPattern);
        Assert.Equal(MetaPatternKind.Inv, _v3.Data.Single(m => m.MrCode == "b").MetaPattern);
        Assert.Equal(MetaPatternKind.Conv, _v3.Data.Single(m => m.MrCode == "c").MetaPattern);
        Assert.Equal(MetaPatternKind.Unspecified, _v3.Data.Single(m => m.MrCode == "d").MetaPattern);
    }

    [Fact]
    public void MapRelation_routes_assertion_codes()
    {
        _v2.Add(MakeV2("greater-mr", assertion: "greater"));
        _v2.Add(MakeV2("less-mr", assertion: "less"));
        _v2.Add(MakeV2("approx-mr", assertion: "approx"));
        _v2.Add(MakeV2("unknown-mr", assertion: "noise-aware"));

        V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);

        Assert.Equal(RelationKind.Ordinal, _v3.Data.Single(m => m.MrCode == "greater-mr").RelationType);
        Assert.Equal(RelationKind.Ordinal, _v3.Data.Single(m => m.MrCode == "less-mr").RelationType);
        Assert.Equal(RelationKind.Equality, _v3.Data.Single(m => m.MrCode == "approx-mr").RelationType);
        Assert.Equal(RelationKind.Unspecified, _v3.Data.Single(m => m.MrCode == "unknown-mr").RelationType);
    }

    [Fact]
    public void MapRigor_classifies_Inv_as_A_Conv_as_B_others_C()
    {
        _v2.Add(MakeV2("inv-mr", metaPattern: "m_inv"));
        _v2.Add(MakeV2("conv-mr", metaPattern: "m_conv"));
        _v2.Add(MakeV2("mono-mr", metaPattern: "m_mono"));

        V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);

        Assert.Equal(RigorClassKind.A, _v3.Data.Single(m => m.MrCode == "inv-mr").RigorClass);
        Assert.Equal(RigorClassKind.B, _v3.Data.Single(m => m.MrCode == "conv-mr").RigorClass);
        Assert.Equal(RigorClassKind.C, _v3.Data.Single(m => m.MrCode == "mono-mr").RigorClass);
    }

    [Fact]
    public void MapProgram_uses_MRBinding_to_Application_for_SUT_name_lookup()
    {
        // S8-P5 review fix: 之前 MapProgram 依赖已 [Obsolete] 的 v2.ApplicationName 字段，
        // 而 LauncherCatalogV2Importer 从不写该字段 → 所有 OpenMC MR 误分类为 Num。
        // 修复后通过 MR.IdMR → MRBinding → Application.Name 查 SutName。
        var v2Mr1 = MakeV2("openmc-mr");
        var v2Mr2 = MakeV2("openmoc-mr");
        var v2Mr3 = MakeV2("subchannel-mr");
        _v2.Add(v2Mr1); _v2.Add(v2Mr2); _v2.Add(v2Mr3);
        // _v2.Add 自增 IdMR，从 1 起；记录实际 ID
        var apps = new LauncherCatalogV2ImporterTests.FakeImporterAppRepo();
        var openmcApp = new MetBench_Domain.Application { Name = "openmc" };
        var openmocApp = new MetBench_Domain.Application { Name = "openmoc" };
        var subApp = new MetBench_Domain.Application { Name = "subchannel-1d" };
        apps.Add(openmcApp); apps.Add(openmocApp); apps.Add(subApp);
        var bindings = new LauncherCatalogV2ImporterTests.FakeImporterBindingRepo();
        bindings.Add(new MetBench_Domain.MRBinding { MRId = v2Mr1.IdMR, ApplicationId = openmcApp.IdApplication });
        bindings.Add(new MetBench_Domain.MRBinding { MRId = v2Mr2.IdMR, ApplicationId = openmocApp.IdApplication });
        bindings.Add(new MetBench_Domain.MRBinding { MRId = v2Mr3.IdMR, ApplicationId = subApp.IdApplication });

        V3MetamorphicRelationMigration.MigrateAll(_v2, _v3, bindings, apps);

        Assert.Equal(ProgramKind.MC, _v3.Data.Single(m => m.MrCode == "openmc-mr").ProgramType);
        Assert.Equal(ProgramKind.Num, _v3.Data.Single(m => m.MrCode == "openmoc-mr").ProgramType);
        Assert.Equal(ProgramKind.Analytic, _v3.Data.Single(m => m.MrCode == "subchannel-mr").ProgramType);
    }

    [Fact]
    public void MapProgram_without_binding_repos_defaults_to_Unspecified()
    {
        _v2.Add(MakeV2("orphan"));

        V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);  // no bindingRepo / appRepo

        Assert.Equal(ProgramKind.Unspecified, _v3.Data.Single(m => m.MrCode == "orphan").ProgramType);
    }

    [Fact]
    public void MigrateAll_counts_duplicate_codes_as_Conflicts_last_wins()
    {
        _v2.Add(MakeV2("dup"));
        var second = MakeV2("dup", equationKey: "heat-equation-1d");
        _v2.Add(second);

        var summary = V3MetamorphicRelationMigration.MigrateAll(_v2, _v3);

        Assert.Equal(1, summary.Conflicts);
        // last-wins: second row 的 equationKey 应在 V3 row 上
        Assert.Equal(EquationKind.Fourier, _v3.Data.Single(m => m.MrCode == "dup").Equation);
    }
}
