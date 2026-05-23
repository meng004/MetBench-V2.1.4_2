using MetBench_DAL.V2.Migrations;
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
    public void MapEquation_maps_known_keys_else_Other()
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
        Assert.Equal(EquationKind.Other, _v3.Data.Single(m => m.MrCode == "f").Equation);
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
}
