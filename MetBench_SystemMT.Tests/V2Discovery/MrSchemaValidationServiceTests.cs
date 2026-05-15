using System.Collections.ObjectModel;
using MetBench_BLL.Discovery;
using MetBench_DAL.V2;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

/// <summary>
/// TDD for T-B MrSchemaValidationService —— MetamorphicRelation.MetaPatternCode 软外键校验。
/// 闭环 cold-start C4 命名漂移问题。
/// </summary>
public sealed class MrSchemaValidationServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDbMetaPatternRepository _mpRepo;
    private readonly FakeMrRepo _mrRepo;
    private readonly MrSchemaValidationService _svc;

    public MrSchemaValidationServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(),
            "MetBenchMrValidation", Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _mpRepo = new LiteDbMetaPatternRepository(_dbPath);
        MetaPatternSeed.EnsureSeeded(_mpRepo);
        _mrRepo = new FakeMrRepo();
        _svc = new MrSchemaValidationService(_mpRepo, _mrRepo);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }

    [Fact]
    public void ValidateAndAdd_active_metapattern_succeeds()
    {
        var mr = MakeMr("MR-X", "m_mono");
        var id = _svc.ValidateAndAdd(mr);
        Assert.True(id > 0);
        Assert.Single(_mrRepo.Data);
    }

    [Fact]
    public void ValidateAndAdd_unknown_metapattern_throws_with_active_list_hint()
    {
        var mr = MakeMr("MR-X", "m_bogus");
        var ex = Assert.Throws<InvalidOperationException>(() => _svc.ValidateAndAdd(mr));
        Assert.Contains("m_bogus", ex.Message);
        Assert.Contains("Active MetaPatterns", ex.Message);
        Assert.Contains("m_inv", ex.Message);
    }

    [Fact]
    public void ValidateAndAdd_out_of_scope_metapattern_refused()
    {
        var mr = MakeMr("MR-Adj", "m_adj");   // seeded out-of-scope
        var ex = Assert.Throws<InvalidOperationException>(() => _svc.ValidateAndAdd(mr));
        Assert.Contains("out-of-scope", ex.Message);
    }

    [Fact]
    public void ValidateAndAdd_empty_metapattern_throws()
    {
        var mr = MakeMr("MR-X", "");
        var ex = Assert.Throws<InvalidOperationException>(() => _svc.ValidateAndAdd(mr));
        Assert.Contains("empty MetaPatternCode", ex.Message);
    }

    [Fact]
    public void ValidateAndAdd_duplicate_code_throws()
    {
        _svc.ValidateAndAdd(MakeMr("MR-X", "m_mono"));
        var ex = Assert.Throws<InvalidOperationException>(() => _svc.ValidateAndAdd(MakeMr("MR-X", "m_mono")));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void ValidateAndAdd_case_sensitive_metapattern_code_rejected()
    {
        // cold-start C4 复发场景：用户写 "M_INV" 而不是 "m_inv"
        var mr = MakeMr("MR-X", "M_INV");
        Assert.Throws<InvalidOperationException>(() => _svc.ValidateAndAdd(mr));
    }

    [Fact]
    public void ValidateAndAdd_deprecated_metapattern_refused()
    {
        _mpRepo.Add(new MetaPattern { Code = "m_old", Status = "deprecated" });
        var mr = MakeMr("MR-X", "m_old");
        var ex = Assert.Throws<InvalidOperationException>(() => _svc.ValidateAndAdd(mr));
        Assert.Contains("deprecated", ex.Message);
    }

    private static MetamorphicRelation MakeMr(string code, string metaPatternCode) => new()
    {
        Code = code,
        MetaPatternCode = metaPatternCode,
        Description = "test", Context = "", Constraint = "", OrderOfMR = "",
        InputPattern = "", OutputPattern = "",
        InputPatterntosympy = "", OutputPatterntosympy = "",
        DimensionOfInputPattern = "", DimensionOfOutputPattern = "",
        Granularity = "", Hierarchy = "", Operator = "", Expression = "",
        InputPatternImageData = Array.Empty<byte>(),
        OutputPatternImageData = Array.Empty<byte>(),
    };
}

internal sealed class FakeMrRepo : IMetamorphicRelationRepository
{
    public List<MetamorphicRelation> Data { get; } = new();
    private int _nextId = 1;
    public DatatoImage datatoImage { get; set; } = null!;

    public ObservableCollection<MetamorphicRelation> GetAll() => new(Data);
    public MetamorphicRelation Get(int id) => Data.First(m => m.IdMR == id);
    public ObservableCollection<MetamorphicRelation> Get(MetamorphicRelation e) => new(Data);
    public bool Add(MetamorphicRelation e) { e.IdMR = _nextId++; Data.Add(e); return true; }
    public bool Modify(MetamorphicRelation e) => false;
    public bool Remove(MetamorphicRelation e) => false;
    public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIX() => new();
    public ObservableCollection<MetamorphicRelations_QueryResultData> Get(MetamorphicRelation m, string d) => new();
    public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIXTwoTable() => new();
    public bool IsDuplicate(MetamorphicRelation m, bool excludeSelf = false)
        => Data.Any(d => d.Code == m.Code && (!excludeSelf || d.IdMR != m.IdMR));
}
