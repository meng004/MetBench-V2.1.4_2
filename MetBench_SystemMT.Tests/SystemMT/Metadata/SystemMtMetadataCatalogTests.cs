using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Metadata;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_DAL;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Metadata;

public sealed class SystemMtMetadataCatalogTests : IDisposable
{
    private readonly string _metaDbPath;
    private readonly SystemMtLauncher _launcher;

    public SystemMtMetadataCatalogTests()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchMetaCatalogTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _metaDbPath = Path.Combine(dir, "metadata.db");
        var launcherOptions = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable());
        _launcher = new SystemMtLauncher(
            launcherOptions,
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            new RecordingAnomalyService(),
            new ManifestMrCatalogProvider(launcherOptions));
    }

    public void Dispose()
    {
        foreach (var p in new[] { _metaDbPath, _metaDbPath + "-log" })
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
            Assert.NotEmpty(mr.MetaPatternRationale);
            Assert.NotEmpty(mr.TransformationSemantics);
            Assert.NotEmpty(mr.ObservableSummary);
            Assert.NotEmpty(mr.PredicateSummary);
            Assert.NotEmpty(mr.ToleranceSummary);
            Assert.NotEmpty(mr.Applicability);
            Assert.NotEmpty(mr.FailureMeaning);
            Assert.NotEmpty(mr.Parameters);
        }
    }

    [Fact]
    public void Seeded_runtime_MR_profiles_cover_mono_inv_and_conv_examples()
    {
        var mono = SystemMtMetadataCatalog.MetamorphicRelations.Single(m => m.MrId == "poisson-source-superposition");
        var inv = SystemMtMetadataCatalog.MetamorphicRelations.Single(m => m.MrId == "subchannel-friction-invariance");
        var conv = SystemMtMetadataCatalog.MetamorphicRelations.Single(m => m.MrId == "poisson-mesh-richardson");

        foreach (var mr in new[] { mono, inv, conv })
        {
            Assert.False(string.IsNullOrWhiteSpace(mr.MetaPatternRationale));
            Assert.False(string.IsNullOrWhiteSpace(mr.TransformationSemantics));
            Assert.False(string.IsNullOrWhiteSpace(mr.ObservableSummary));
            Assert.False(string.IsNullOrWhiteSpace(mr.PredicateSummary));
            Assert.False(string.IsNullOrWhiteSpace(mr.ToleranceSummary));
            Assert.False(string.IsNullOrWhiteSpace(mr.Applicability));
            Assert.False(string.IsNullOrWhiteSpace(mr.FailureMeaning));
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
