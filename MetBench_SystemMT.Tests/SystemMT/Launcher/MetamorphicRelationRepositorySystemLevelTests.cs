using MetBench_DAL;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Launcher;

[Collection("DbConfigGlobal")]
public sealed class MetamorphicRelationRepositorySystemLevelTests : IDisposable
{
    private readonly string _dbPath;

    public MetamorphicRelationRepositorySystemLevelTests()
    {
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            "MetBenchSystemMrRepositoryTests",
            Guid.NewGuid().ToString("N") + ".db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        DbConfig.OverrideConnectionString($"Filename={_dbPath}");
    }

    [Fact]
    public void Add_accepts_multiple_system_level_mrs_without_legacy_application_name()
    {
        var repo = new MetamorphicRelationRepository();

        Assert.True(repo.Add(SystemMr("system-mr-a")));
        Assert.True(repo.Add(SystemMr("system-mr-b")));

        var rows = repo.GetAll().Where(mr => mr.Kind == "system-level").ToList();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row =>
        {
#pragma warning disable CS0618
            Assert.True(string.IsNullOrEmpty(row.ApplicationName));
#pragma warning restore CS0618
        });
    }

    [Fact]
    public void Add_system_level_mrs_are_not_exposed_by_legacy_method_mix_query()
    {
        var repo = new MetamorphicRelationRepository();

        Assert.True(repo.Add(SystemMr("system-hidden")));

        Assert.Empty(repo.GetAll_MIX());
        Assert.Empty(repo.GetAll_MIXTwoTable());
    }

    private static MetamorphicRelation SystemMr(string code) => new()
    {
        Code = code,
        Description = code,
        Kind = "system-level",
        TransformationName = "ScaleField",
        AssertionTypeCode = "greater",
        ValueName = "k_eff",
        MetaPatternCode = "m_mono",
        DiscoveryMethod = "manual",
        InputPattern = $"systemmt:{code}:source",
        OutputPattern = $"systemmt:{code}:followup",
        EquationKey = "boltzmann",
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test",
    };

    public void Dispose()
    {
        DbConfig.ResetOverride();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        var log = _dbPath + "-log";
        if (File.Exists(log)) File.Delete(log);
    }
}
