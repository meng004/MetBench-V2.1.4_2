using MetBench_BLL.SystemMT.Bootstrap;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Metadata;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain.V2.Enums;
using MetBench_SystemMT.Tests.SystemMT;
using MetBench_SystemMT.Tests.SystemMT.Launcher;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;
using V3MrLibrary = MetBench_SystemMT.Tests.V3MrLibrary;

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
        new(new LauncherOptions(
                SutRoot: TestAssetPaths.AssetRoot(),
                SystemPython: TestAssetPaths.PythonExecutable(),
                OpenMocPython: TestAssetPaths.PythonExecutable()),
            new SystemMtPipeline(),
            new SystemMtExecutionRecorder(new FakeExecRepo(), new FakeResultRepo()),
            new RecordingAnomalyService(),
            new ManifestMrCatalogProvider(new LauncherOptions(
                SutRoot: TestAssetPaths.AssetRoot(),
                SystemPython: TestAssetPaths.PythonExecutable(),
                OpenMocPython: TestAssetPaths.PythonExecutable())));

    private LauncherCatalogV2Importer MakeImporter() =>
        new(MakeLauncher(), _apps, _mrs, _bindings, _audit);

    [Fact]
    public async Task SeedCatalogsAsync_seeds_metadata_and_imports_entities()
    {
        var importer = MakeImporter();

        var result = await SystemMtBootstrap.SeedCatalogsAsync(_meta, importer);

        // metadata seed: 13 equations + 30 MRs（T3C-BVP 后 12eq/29MR；
        // PR-A non-JSON I/O adapter synthetic _test_csv equation + csv-roundtrip-identity MR = 13eq/30MR）
        Assert.Equal(13, result.EquationsSeeded);
        Assert.Equal(32, result.MrsSeeded);
        Assert.Equal(13, (await _meta.ListEquationsAsync()).Count);
        Assert.Equal(32, (await _meta.ListMrsAsync()).Count);

        // entity import: 16 SUT + 30 MR + 30 binding
        Assert.NotNull(result.ImportSummary);
        Assert.Equal(16, result.ImportSummary!.ApplicationsCreated);
        Assert.Equal(32, result.ImportSummary.MrsCreated);
        Assert.Equal(32, result.ImportSummary.BindingsCreated);
    }

    [Fact]
    public async Task SeedCatalogsAsync_is_idempotent_on_second_call()
    {
        var importer = MakeImporter();

        await SystemMtBootstrap.SeedCatalogsAsync(_meta, importer);
        var second = await SystemMtBootstrap.SeedCatalogsAsync(_meta, importer);

        // metadata 仍是 13/30（upsert 而非追加）
        Assert.Equal(13, (await _meta.ListEquationsAsync()).Count);
        Assert.Equal(32, (await _meta.ListMrsAsync()).Count);
        // entity 第二次 created=0, existing 显示原有计数
        Assert.NotNull(second.ImportSummary);
        Assert.Equal(0, second.ImportSummary!.ApplicationsCreated);
        Assert.Equal(16, second.ImportSummary.ApplicationsExisting);
        Assert.Equal(0, second.ImportSummary.MrsCreated);
        Assert.Equal(32, second.ImportSummary.MrsExisting);
    }

    [Fact]
    public async Task SeedCatalogsAsync_skips_metadata_when_repo_is_null()
    {
        var importer = MakeImporter();

        var result = await SystemMtBootstrap.SeedCatalogsAsync(metadataRepository: null, importer);

        Assert.Equal(0, result.EquationsSeeded);
        Assert.Equal(0, result.MrsSeeded);
        Assert.NotNull(result.ImportSummary);
        Assert.Equal(16, result.ImportSummary!.ApplicationsCreated);
    }

    [Fact]
    public async Task SeedCatalogsAsync_skips_import_when_importer_is_null()
    {
        var result = await SystemMtBootstrap.SeedCatalogsAsync(_meta, launcherImporter: null);

        Assert.Equal(13, result.EquationsSeeded);
        Assert.Equal(32, result.MrsSeeded);
        Assert.Null(result.ImportSummary);
        Assert.Null(result.V3MigrationSummary);
        // entity 表未被改
        Assert.Empty(_apps.Data);
        Assert.Empty(_mrs.Data);
    }

    [Fact]
    public async Task SeedCatalogsAsync_runs_V3_migration_when_context_provided()
    {
        // S8-P5 review fix: Bootstrap 新增 Phase 3 — V2 entity 表 → V3 5D-tag schema
        var importer = MakeImporter();
        var v3 = new V3MrLibrary.FakeV3Repo();
        var v3Ctx = new V3MigrationContext(_mrs, v3, _bindings, _apps);

        var result = await SystemMtBootstrap.SeedCatalogsAsync(_meta, importer, v3Ctx);

        Assert.NotNull(result.V3MigrationSummary);
        // 全部 30 system-level MR 应投影到 V3
        Assert.Equal(32, result.V3MigrationSummary!.Created);
        Assert.Equal(32, v3.Data.Count);
        // 关键修复验证：EquationKey 现在能正确传到 V3
        Assert.Equal(EquationKind.Bateman,
            v3.Data.Single(m => m.MrCode == "bateman-mass-conservation").Equation);
        // 关键修复验证：MapProgram 通过 binding+app lookup 正确识别 MC
        Assert.Equal(ProgramKind.MC,
            v3.Data.Single(m => m.MrCode == "openmc-pincell-nu-sigma-f").ProgramType);
    }
}
