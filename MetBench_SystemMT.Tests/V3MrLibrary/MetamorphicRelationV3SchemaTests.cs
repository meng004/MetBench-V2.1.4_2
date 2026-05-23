using LiteDB;
using MetBench_Domain.V2;
using MetBench_Domain.V2.Enums;
using Xunit;

namespace MetBench_SystemMT.Tests.V3MrLibrary;

/// <summary>
/// S8-P5a: MetamorphicRelationV3 实体 + 7 个 5D enum 的 round-trip + 默认值验证。
/// V3 schema 与既有 v1/v2 MR 并存，是 5D tag 索引视图（由 .feature canonical
/// 或 V2→V3 migration 投影而来）。
/// </summary>
[Collection("DbConfigGlobal")]
public sealed class MetamorphicRelationV3SchemaTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ILiteDatabase _db;

    public MetamorphicRelationV3SchemaTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchV3SchemaTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _db = new LiteDatabase(_dbPath, new BsonMapper());
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }

    [Fact]
    public void Default_constructed_V3_has_all_tags_unspecified()
    {
        var mr = new MetamorphicRelationV3();

        Assert.Equal(EquationKind.Unspecified, mr.Equation);
        Assert.Equal(ProgramKind.Unspecified, mr.ProgramType);
        Assert.Equal(MetaPatternKind.Unspecified, mr.MetaPattern);
        Assert.Equal(SourceLevelKind.Unspecified, mr.SourceLevel);
        Assert.Equal(FailureCorrelationKind.Unspecified, mr.FailureCorrelation);
        Assert.Equal(RigorClassKind.Unspecified, mr.RigorClass);
        Assert.Equal(RelationKind.Unspecified, mr.RelationType);
        Assert.Equal(0.0, mr.Tolerance);
        Assert.True((DateTime.UtcNow - mr.SyncedAt).TotalSeconds < 5);
    }

    [Fact]
    public void V3_full_field_roundtrip_via_LiteDB()
    {
        var col = _db.GetCollection<MetamorphicRelationV3>("v3_mrs");
        var src = new MetamorphicRelationV3
        {
            MrCode = "bateman-mass-conservation",
            Description = "Mass conservation invariance",
            Equation = EquationKind.Bateman,
            ProgramType = ProgramKind.Analytic,
            MetaPattern = MetaPatternKind.Inv,
            SourceLevel = SourceLevelKind.Manual,
            FailureCorrelation = FailureCorrelationKind.None,
            RigorClass = RigorClassKind.A,
            RelationType = RelationKind.Equality,
            Tolerance = 1e-9,
        };
        col.Insert(src);

        var back = col.FindOne(m => m.MrCode == "bateman-mass-conservation");

        Assert.NotNull(back);
        Assert.Equal(src.MrCode, back!.MrCode);
        Assert.Equal(EquationKind.Bateman, back.Equation);
        Assert.Equal(ProgramKind.Analytic, back.ProgramType);
        Assert.Equal(MetaPatternKind.Inv, back.MetaPattern);
        Assert.Equal(SourceLevelKind.Manual, back.SourceLevel);
        Assert.Equal(FailureCorrelationKind.None, back.FailureCorrelation);
        Assert.Equal(RigorClassKind.A, back.RigorClass);
        Assert.Equal(RelationKind.Equality, back.RelationType);
        Assert.Equal(1e-9, back.Tolerance, 12);
    }

    [Fact]
    public void V3_collection_indexes_by_5D_tags_for_queryability()
    {
        var col = _db.GetCollection<MetamorphicRelationV3>("v3_mrs");
        // Insert 3 MR with distinct Equation / MetaPattern
        col.Insert(new MetamorphicRelationV3
        {
            MrCode = "a", Equation = EquationKind.Bateman,
            MetaPattern = MetaPatternKind.Inv, ProgramType = ProgramKind.Analytic,
        });
        col.Insert(new MetamorphicRelationV3
        {
            MrCode = "b", Equation = EquationKind.Fourier,
            MetaPattern = MetaPatternKind.Conv, ProgramType = ProgramKind.Num,
        });
        col.Insert(new MetamorphicRelationV3
        {
            MrCode = "c", Equation = EquationKind.Bateman,
            MetaPattern = MetaPatternKind.Conv, ProgramType = ProgramKind.Num,
        });

        // 按 Equation=Bateman 查
        var bateman = col.Query().Where(m => m.Equation == EquationKind.Bateman).ToList();
        Assert.Equal(2, bateman.Count);

        // 按 (Equation=Bateman, MetaPattern=Inv) 联合查
        var batemanInv = col.Query().Where(m =>
            m.Equation == EquationKind.Bateman &&
            m.MetaPattern == MetaPatternKind.Inv).ToList();
        Assert.Single(batemanInv);
        Assert.Equal("a", batemanInv[0].MrCode);
    }

    [Fact]
    public void Each_5D_enum_has_Unspecified_default_at_index_0()
    {
        // Verify .NET enum default rules — default(EnumType) returns the underlying-0 value,
        // which must be Unspecified for safe V2→V3 migration default填充.
        Assert.Equal(EquationKind.Unspecified, default(EquationKind));
        Assert.Equal(ProgramKind.Unspecified, default(ProgramKind));
        Assert.Equal(MetaPatternKind.Unspecified, default(MetaPatternKind));
        Assert.Equal(SourceLevelKind.Unspecified, default(SourceLevelKind));
        Assert.Equal(FailureCorrelationKind.Unspecified, default(FailureCorrelationKind));
        Assert.Equal(RelationKind.Unspecified, default(RelationKind));
        Assert.Equal(RigorClassKind.Unspecified, default(RigorClassKind));
    }
}
