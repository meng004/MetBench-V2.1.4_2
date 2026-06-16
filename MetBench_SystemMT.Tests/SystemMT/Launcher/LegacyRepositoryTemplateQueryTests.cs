using MetBench_DAL;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

#pragma warning disable CS0618 // Legacy repository template query coverage intentionally uses v1 ApplicationName.

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

[Collection("DbConfigGlobal")]
public sealed class LegacyRepositoryTemplateQueryTests : IDisposable
{
    private readonly string _dbPath;

    public LegacyRepositoryTemplateQueryTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchLegacyRepositoryTemplateQueryTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        DbConfig.OverrideConnectionString($"Filename={_dbPath}");
    }

    [Fact]
    public void Application_template_get_filters_by_populated_fields()
    {
        var repo = new ApplicationRepository();
        Assert.True(repo.Add(new Application
        {
            Name = "solver-alpha",
            Description = "neutron diffusion baseline",
            ProgrammingLanguage = "C#",
            LinesOfCode = 120,
            Kind = "method-level",
        }));
        Assert.True(repo.Add(new Application
        {
            Name = "solver-beta",
            Description = "thermal hydraulics",
            ProgrammingLanguage = "Python",
            LinesOfCode = 80,
            Kind = "system-level",
        }));

        var results = repo.Get(new Application
        {
            Name = "solver",
            ProgrammingLanguage = "C#",
            Kind = "method-level",
        });

        var result = Assert.Single(results);
        Assert.Equal("solver-alpha", result.Name);
    }

    [Fact]
    public void Metamorphic_relation_template_get_filters_by_populated_fields()
    {
        var appRepo = new ApplicationRepository();
        Assert.True(appRepo.Add(new Application
        {
            Name = "solver-alpha",
            ProgrammingLanguage = "C#",
            LinesOfCode = 120,
        }));

        var repo = new MetamorphicRelationRepository();
        Assert.True(repo.Add(new MetamorphicRelation
        {
            Description = "scale source preserves normalized flux",
            OrderOfMR = "unary",
            InputPattern = "scale-source",
            OutputPattern = "same-normalized-flux",
            DimensionOfInputPattern = "scalar",
            DimensionOfOutputPattern = "scalar",
            ApplicationName = "solver-alpha",
            Kind = "method-level",
            MetaPatternCode = "m_inv",
        }));
        Assert.True(repo.Add(new MetamorphicRelation
        {
            Description = "translation changes output",
            OrderOfMR = "binary",
            InputPattern = "translate",
            OutputPattern = "changed-output",
            DimensionOfInputPattern = "vector",
            DimensionOfOutputPattern = "vector",
            ApplicationName = "solver-alpha",
            Kind = "method-level",
            MetaPatternCode = "m_mono",
        }));

        var results = ((IRepository<MetamorphicRelation>)repo).Get(new MetamorphicRelation
        {
            OrderOfMR = "unary",
            MetaPatternCode = "m_inv",
            ApplicationName = "solver-alpha",
        });

        var result = Assert.Single(results);
        Assert.Equal("scale source preserves normalized flux", result.Description);
    }

    public void Dispose()
    {
        DbConfig.ResetOverride();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }
}
