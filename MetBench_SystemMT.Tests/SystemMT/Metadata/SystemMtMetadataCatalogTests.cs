using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Metadata;
using MetBench_DAL;
using MetBench_SystemMT.Tests.V2Anomaly;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Metadata;

public sealed class SystemMtMetadataCatalogTests : IDisposable
{
    private readonly string _resultDbPath;
    private readonly string _metaDbPath;
    private readonly LiteDbSystemMtResultRepository _resultRepo;
    private readonly SystemMtMrLauncher _launcher;

    public SystemMtMetadataCatalogTests()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchMetaCatalogTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _resultDbPath = Path.Combine(dir, "results.db");
        _metaDbPath = Path.Combine(dir, "metadata.db");
        _resultRepo = new LiteDbSystemMtResultRepository(_resultDbPath);
        _launcher = new SystemMtMrLauncher(
            new LauncherOptions("/tmp", "python3", "python3"),
            _resultRepo,
            new RecordingAnomalyService());
    }

    public void Dispose()
    {
        _resultRepo.Dispose();
        foreach (var p in new[] { _resultDbPath, _resultDbPath + "-log", _metaDbPath, _metaDbPath + "-log" })
        {
            if (File.Exists(p)) File.Delete(p);
        }
    }

    [Fact]
    public async Task Catalog_seeds_metadata_for_exactly_the_launcher_MR_set()
    {
        var launcherIds = (await _launcher.ListAvailableAsync())
            .Select(m => m.Id)
            .ToHashSet();
        var seededIds = SystemMtMetadataCatalog.MetamorphicRelations
            .Select(m => m.MrId)
            .ToHashSet();

        Assert.Equal(launcherIds, seededIds);
    }

    [Fact]
    public void Every_seeded_MR_is_complete_and_references_a_seeded_equation()
    {
        var equationKeys = SystemMtMetadataCatalog.Equations
            .Select(e => e.EquationKey)
            .ToHashSet();

        foreach (var mr in SystemMtMetadataCatalog.MetamorphicRelations)
        {
            Assert.Contains(mr.EquationKey, equationKeys);
            Assert.NotEmpty(mr.PhysicalMeaning);
            Assert.NotEmpty(mr.InputTransformation);
            Assert.NotEmpty(mr.OutputRelation);
            Assert.NotEmpty(mr.Parameters);
        }
    }

    [Fact]
    public void Every_seeded_equation_is_complete()
    {
        foreach (var eq in SystemMtMetadataCatalog.Equations)
        {
            Assert.NotEmpty(eq.EquationKey);
            Assert.NotEmpty(eq.Name);
            Assert.NotEmpty(eq.CanonicalForm);
            Assert.NotEmpty(eq.SymbolSystem);
            Assert.NotEmpty(eq.Parameters);
        }
    }

    [Fact]
    public async Task SeedAsync_persists_all_equations_and_MRs_and_is_idempotent()
    {
        using (var repo = new LiteDbSystemMtMetadataRepository(_metaDbPath))
        {
            await SystemMtMetadataCatalog.SeedAsync(repo);
            await SystemMtMetadataCatalog.SeedAsync(repo);
        }

        using (var repo = new LiteDbSystemMtMetadataRepository(_metaDbPath))
        {
            var equations = await repo.ListEquationsAsync();
            var mrs = await repo.ListMrsAsync();

            Assert.Equal(SystemMtMetadataCatalog.Equations.Count, equations.Count);
            Assert.Equal(SystemMtMetadataCatalog.MetamorphicRelations.Count, mrs.Count);
        }
    }
}
