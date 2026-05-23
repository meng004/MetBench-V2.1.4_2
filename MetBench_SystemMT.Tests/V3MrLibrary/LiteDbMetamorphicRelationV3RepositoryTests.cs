using MetBench_DAL;
using MetBench_DAL.V2;
using MetBench_Domain.V2;
using MetBench_Domain.V2.Enums;
using Xunit;

namespace MetBench_SystemMT.Tests.V3MrLibrary;

/// <summary>
/// S8-P5b: LiteDB V3 MR repository CRUD + 5D 维度过滤查询测试。
/// 使用临时 DB（不污染 DbConfig.Instance）。
/// </summary>
[Collection("DbConfigGlobal")]
public sealed class LiteDbMetamorphicRelationV3RepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _conn;
    private readonly DbConfig _dbConfig;

    public LiteDbMetamorphicRelationV3RepositoryTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchV3RepoTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _conn = $"Filename={_dbPath}";
        DbConfig.OverrideConnectionString(_conn);
        _dbConfig = DbConfig.Instance;
    }

    public void Dispose()
    {
        DbConfig.ResetOverride();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }

    private LiteDbMetamorphicRelationV3Repository MakeRepo() =>
        new(_dbConfig, _conn);

    private static MetamorphicRelationV3 MakeMr(
        string code,
        EquationKind eq = EquationKind.Bateman,
        MetaPatternKind mp = MetaPatternKind.Mono)
        => new()
        {
            IdV3 = Guid.NewGuid(),
            MrCode = code,
            Description = $"Test MR {code}",
            Equation = eq,
            ProgramType = ProgramKind.Num,
            MetaPattern = mp,
            SourceLevel = SourceLevelKind.Manual,
            FailureCorrelation = FailureCorrelationKind.None,
            RigorClass = RigorClassKind.B,
            RelationType = RelationKind.Ordinal,
        };

    [Fact]
    public void Add_then_Get_roundtrip_by_id()
    {
        var repo = MakeRepo();
        var mr = MakeMr("a");

        Assert.True(repo.Add(mr));
        var back = repo.Get(mr.IdV3);

        Assert.NotNull(back);
        Assert.Equal("a", back!.MrCode);
        Assert.Equal(EquationKind.Bateman, back.Equation);
    }

    [Fact]
    public void GetByCode_returns_matching_row_or_null()
    {
        var repo = MakeRepo();
        repo.Add(MakeMr("bateman-mass-conservation"));
        repo.Add(MakeMr("fourier-timestep-convergence"));

        var found = repo.GetByCode("bateman-mass-conservation");
        Assert.NotNull(found);
        Assert.Equal("bateman-mass-conservation", found!.MrCode);

        Assert.Null(repo.GetByCode("nonexistent"));
        Assert.Null(repo.GetByCode(""));
    }

    [Fact]
    public void GetByEquation_filters_by_5D_first_dim()
    {
        var repo = MakeRepo();
        repo.Add(MakeMr("a", EquationKind.Bateman));
        repo.Add(MakeMr("b", EquationKind.Fourier));
        repo.Add(MakeMr("c", EquationKind.Bateman));

        var bateman = repo.GetByEquation(EquationKind.Bateman);

        Assert.Equal(2, bateman.Count);
        Assert.All(bateman, m => Assert.Equal(EquationKind.Bateman, m.Equation));
    }

    [Fact]
    public void GetByMetaPattern_filters_by_5D_third_dim()
    {
        var repo = MakeRepo();
        repo.Add(MakeMr("a", EquationKind.Bateman, MetaPatternKind.Inv));
        repo.Add(MakeMr("b", EquationKind.Fourier, MetaPatternKind.Conv));
        repo.Add(MakeMr("c", EquationKind.Bateman, MetaPatternKind.Conv));

        var convergence = repo.GetByMetaPattern(MetaPatternKind.Conv);

        Assert.Equal(2, convergence.Count);
        Assert.All(convergence, m => Assert.Equal(MetaPatternKind.Conv, m.MetaPattern));
    }

    [Fact]
    public void GetByEquationAndPattern_joins_two_5D_dims()
    {
        var repo = MakeRepo();
        repo.Add(MakeMr("a", EquationKind.Bateman, MetaPatternKind.Inv));
        repo.Add(MakeMr("b", EquationKind.Bateman, MetaPatternKind.Conv));
        repo.Add(MakeMr("c", EquationKind.Fourier, MetaPatternKind.Conv));

        var batemanConv = repo.GetByEquationAndPattern(
            EquationKind.Bateman, MetaPatternKind.Conv);

        Assert.Single(batemanConv);
        Assert.Equal("b", batemanConv[0].MrCode);
    }

    [Fact]
    public void Modify_updates_existing_row()
    {
        var repo = MakeRepo();
        var mr = MakeMr("modify-test");
        repo.Add(mr);

        mr.Description = "modified";
        mr.RigorClass = RigorClassKind.A;
        Assert.True(repo.Modify(mr));

        var back = repo.Get(mr.IdV3);
        Assert.NotNull(back);
        Assert.Equal("modified", back!.Description);
        Assert.Equal(RigorClassKind.A, back.RigorClass);
    }

    [Fact]
    public void Remove_deletes_row()
    {
        var repo = MakeRepo();
        var mr = MakeMr("remove-test");
        repo.Add(mr);

        Assert.True(repo.Remove(mr));
        Assert.Null(repo.Get(mr.IdV3));
    }

    [Fact]
    public void Count_reflects_collection_size()
    {
        var repo = MakeRepo();
        Assert.Equal(0, repo.Count());

        repo.Add(MakeMr("a"));
        repo.Add(MakeMr("b"));
        Assert.Equal(2, repo.Count());
    }

    [Fact]
    public void GetAll_returns_every_inserted_row()
    {
        var repo = MakeRepo();
        repo.Add(MakeMr("a"));
        repo.Add(MakeMr("b"));
        repo.Add(MakeMr("c"));

        var all = repo.GetAll();

        Assert.Equal(3, all.Count);
    }
}
