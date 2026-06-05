using MetBench_BLL.Core.SystemMT.ImportExport.Put;
using MetBench_BLL.SystemMT.Jobs;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public sealed class AssetImportExportJobTests
{
    [Fact]
    public async Task ImportAssets_job_stages_valid_package_and_persists_manifest_artifact()
    {
        var unit = AGroupPutFixtures.Create("P5");
        using var temp = TempDirectory.Create();
        var packageRoot = temp.PathOf("package");
        var stagingRoot = temp.PathOf("staging");
        SutImportPackageExporter.Export(unit, packageRoot);
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var worker = BuildWorker(store);

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ImportAssets,
            PackageRoot: packageRoot,
            StagingRoot: stagingRoot));
        await worker.RunJobAsync(await queue.DequeueAsync(default), default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);
        Assert.False(string.IsNullOrWhiteSpace(status.ArtifactPath));
        Assert.True(File.Exists(status.ArtifactPath));
        Assert.EndsWith("staging-manifest.json", status.ArtifactPath, StringComparison.Ordinal);
        Assert.Contains(Path.Combine(stagingRoot, "P5"), status.ArtifactPath, StringComparison.Ordinal);

        var stagedPackage = Path.Combine(Path.GetDirectoryName(status.ArtifactPath)!, "sut-import-unit.json");
        Assert.True(File.Exists(stagedPackage));
        var staged = SutImportPackageExporter.Import(Path.GetDirectoryName(status.ArtifactPath)!);
        Assert.Equal(unit.Sut.SutId, staged.Sut.SutId);
        Assert.False(Directory.Exists(Path.Combine(temp.Root, "SUT")));
        Assert.False(Directory.Exists(Path.Combine(temp.Root, "MetBench_DataBase")));
    }

    [Fact]
    public async Task ExportAssets_job_validates_package_and_persists_exported_unit_artifact()
    {
        var unit = AGroupPutFixtures.Create("P4");
        using var temp = TempDirectory.Create();
        var packageRoot = temp.PathOf("package");
        var exportRoot = temp.PathOf("export");
        SutImportPackageExporter.Export(unit, packageRoot);
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var worker = BuildWorker(store);

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportAssets,
            PackageRoot: packageRoot,
            ExportRoot: exportRoot));
        await worker.RunJobAsync(await queue.DequeueAsync(default), default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);
        Assert.Equal(Path.Combine(exportRoot, "sut-import-unit.json"), status.ArtifactPath);
        Assert.True(File.Exists(status.ArtifactPath));
        var exported = SutImportPackageExporter.Import(exportRoot);
        Assert.Equal(unit.Sut.SutId, exported.Sut.SutId);
        Assert.True(SutImportValidator.Validate(exported).IsValid);
    }

    [Fact]
    public async Task ImportAssets_job_records_validation_failure_as_failed_status_without_staging()
    {
        var unit = AGroupPutFixtures.Create("P5") with { Provenance = Provenance.Empty };
        using var temp = TempDirectory.Create();
        var packageRoot = temp.PathOf("package");
        var stagingRoot = temp.PathOf("staging");
        WriteUncheckedPackage(unit, packageRoot);
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var worker = BuildWorker(store);

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ImportAssets,
            PackageRoot: packageRoot,
            StagingRoot: stagingRoot));
        await worker.RunJobAsync(await queue.DequeueAsync(default), default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Failed, status!.State);
        Assert.Contains("invalid", status.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Complete provenance is required", status.FailureReason, StringComparison.Ordinal);
        Assert.False(Directory.Exists(stagingRoot));
    }

    [Fact]
    public void Staging_service_does_not_overwrite_same_sut_same_timestamp_package()
    {
        var unit = AGroupPutFixtures.Create("P5");
        using var temp = TempDirectory.Create();
        var packageRoot = temp.PathOf("package");
        var stagingRoot = temp.PathOf("staging");
        SutImportPackageExporter.Export(unit, packageRoot);
        var service = new SutImportStagingService(() => new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc));

        var first = service.Stage(unit, packageRoot, stagingRoot);
        var second = service.Stage(unit, packageRoot, stagingRoot);

        Assert.NotEqual(first, second);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
    }

    private static SystemMtJobWorker BuildWorker(InMemoryJobStore store)
    {
        var dispatcher = new SystemMtJobOperationDispatcher(new ISystemMtJobOperationHandler[]
        {
            new ImportAssetsJobOperationHandler(new SutImportStagingService(() => new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc))),
            new ExportAssetsJobOperationHandler(),
        });

        return new SystemMtJobWorker(
            store,
            FakeAsyncPipeline.Succeeds("unused", "sut"),
            operationDispatcher: dispatcher);
    }

    private static void WriteUncheckedPackage(SutImportUnit unit, string packageRoot)
    {
        Directory.CreateDirectory(packageRoot);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        File.WriteAllText(Path.Combine(packageRoot, "sut-import-unit.json"), JsonSerializer.Serialize(unit, options));
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Root { get; }

        private TempDirectory(string root) => Root = root;

        public static TempDirectory Create() =>
            new(Path.Combine(Path.GetTempPath(), "MetBenchAssetJobTests", Guid.NewGuid().ToString("N")));

        public string PathOf(string name) => Path.Combine(Root, name);

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
