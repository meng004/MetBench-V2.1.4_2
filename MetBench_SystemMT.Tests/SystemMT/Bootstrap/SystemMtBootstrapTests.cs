using MetBench_BLL.SystemMT.Bootstrap;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Metadata;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.SystemMT.Launcher;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Bootstrap;

/// <summary>
/// G-08：启动期 catalog → entity 表自动 seed bootstrap helper。
/// </summary>
public sealed class SystemMtBootstrapTests
{
    private readonly LauncherCatalogV2ImporterTests.FakeImporterAppRepo _apps = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterMrRepo _mrs = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterBindingRepo _bindings = new();
    private readonly LauncherCatalogV2ImporterTests.FakeImporterAuditRepo _audit = new();
    private readonly Catalog.P3CatalogExtensionTests.FakeMetadataRepo _meta = new();

    private SystemMtLauncher MakeLauncher() =>
        new(new LauncherOptions("/tmp", "python3", "python3"),
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            new RecordingAnomalyService());

    private LauncherCatalogV2Importer MakeImporter() =>
        new(MakeLauncher(), _apps, _mrs, _bindings, _audit);

    [Fact]
    public async Task SeedCatalogsAsync_seeds_metadata_and_imports_entities()
    {
        var importer = MakeImporter();

        var result = await SystemMtBootstrap.SeedCatalogsAsync(_meta, importer);

        // metadata seed: 7 equations + 15 MRs（S8-P3 后 13 + 2 MR + 1 navier-stokes 方程 = 7eq/15MR）
        Assert.Equal(7, result.EquationsSeeded);
        Assert.Equal(15, result.MrsSeeded);
        Assert.Equal(7, (await _meta.ListEquationsAsync()).Count);
        Assert.Equal(15, (await _meta.ListMrsAsync()).Count);

        // entity import: 8 SUT + 15 MR + 15 binding
        Assert.NotNull(result.ImportSummary);
        Assert.Equal(8, result.ImportSummary!.ApplicationsCreated);
        Assert.Equal(15, result.ImportSummary.MrsCreated);
        Assert.Equal(15, result.ImportSummary.BindingsCreated);
    }

    [Fact]
    public async Task SeedCatalogsAsync_is_idempotent_on_second_call()
    {
        var importer = MakeImporter();

        await SystemMtBootstrap.SeedCatalogsAsync(_meta, importer);
        var second = await SystemMtBootstrap.SeedCatalogsAsync(_meta, importer);

        // metadata 仍是 7/15（upsert 而非追加）
        Assert.Equal(7, (await _meta.ListEquationsAsync()).Count);
        Assert.Equal(15, (await _meta.ListMrsAsync()).Count);
        // entity 第二次 created=0, existing 显示原有计数
        Assert.NotNull(second.ImportSummary);
        Assert.Equal(0, second.ImportSummary!.ApplicationsCreated);
        Assert.Equal(8, second.ImportSummary.ApplicationsExisting);
        Assert.Equal(0, second.ImportSummary.MrsCreated);
        Assert.Equal(15, second.ImportSummary.MrsExisting);
    }

    [Fact]
    public async Task SeedCatalogsAsync_skips_metadata_when_repo_is_null()
    {
        var importer = MakeImporter();

        var result = await SystemMtBootstrap.SeedCatalogsAsync(metadataRepository: null, importer);

        Assert.Equal(0, result.EquationsSeeded);
        Assert.Equal(0, result.MrsSeeded);
        Assert.NotNull(result.ImportSummary);
        Assert.Equal(8, result.ImportSummary!.ApplicationsCreated);
    }

    [Fact]
    public async Task SeedCatalogsAsync_skips_import_when_importer_is_null()
    {
        var result = await SystemMtBootstrap.SeedCatalogsAsync(_meta, launcherImporter: null);

        Assert.Equal(7, result.EquationsSeeded);
        Assert.Equal(15, result.MrsSeeded);
        Assert.Null(result.ImportSummary);
        // entity 表未被改
        Assert.Empty(_apps.Data);
        Assert.Empty(_mrs.Data);
    }
}
