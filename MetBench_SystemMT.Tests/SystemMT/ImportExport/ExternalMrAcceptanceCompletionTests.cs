using MetBench_BLL.Core.SystemMT.ImportExport.Put;
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Anomaly;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.ImportExport;

public sealed class ExternalMrAcceptanceCompletionTests
{
    [Theory]
    [InlineData("toy")]
    [InlineData("p1")]
    [InlineData("sciml")]
    [InlineData("batch-b")]
    [InlineData("batch-c")]
    public async Task External_acceptance_packages_stage_through_import_and_export_asset_jobs(string package)
    {
        using var temp = TempDirectory.Create();
        var packageRoot = Path.Combine(temp.Root, "package");
        var stagingRoot = Path.Combine(temp.Root, "staging");
        var exportRoot = Path.Combine(temp.Root, "export");
        SutImportPackageExporter.Export(CreatePackage(package), packageRoot);

        var importHandler = new ImportAssetsJobOperationHandler();
        var importOutcome = await importHandler.ExecuteAsync(
            Guid.NewGuid(),
            new SystemMtJobRecord
            {
                Kind = SystemMtJobKind.ImportAssets,
                PackageRoot = packageRoot,
                StagingRoot = stagingRoot,
            },
            progress: null,
            CancellationToken.None);

        Assert.Equal(SystemMtJobState.Succeeded, importOutcome.FinalState);
        Assert.NotNull(importOutcome.ArtifactPath);
        Assert.True(File.Exists(importOutcome.ArtifactPath));
        var stagedUnit = Path.Combine(Path.GetDirectoryName(importOutcome.ArtifactPath)!, "sut-import-unit.json");
        Assert.True(File.Exists(stagedUnit));

        var exportHandler = new ExportAssetsJobOperationHandler();
        var exportOutcome = await exportHandler.ExecuteAsync(
            Guid.NewGuid(),
            new SystemMtJobRecord
            {
                Kind = SystemMtJobKind.ExportAssets,
                PackageRoot = Path.GetDirectoryName(importOutcome.ArtifactPath)!,
                ExportRoot = exportRoot,
            },
            progress: null,
            CancellationToken.None);

        Assert.Equal(SystemMtJobState.Succeeded, exportOutcome.FinalState);
        Assert.True(File.Exists(Path.Combine(exportRoot, "sut-import-unit.json")));
        Assert.True(SutImportValidator.Validate(SutImportPackageExporter.Import(exportRoot)).IsValid);
    }

    [Fact]
    public async Task Batch_A_acceptance_catalog_runs_one_toy_and_three_p1_mrs_through_launcher()
    {
        var launcher = CreateAcceptanceLauncher(out var execs, out var results, out var anomalies);

        var run = await launcher.RunBatchAsync(new[]
        {
            new BatchMrRunRequest("minmr-toy-sort-permutation"),
            new BatchMrRunRequest("minmr-p1-heat-alpha-monotonic"),
            new BatchMrRunRequest("minmr-p1-heat-timestep-convergence"),
            new BatchMrRunRequest("minmr-p1-heat-mesh-convergence"),
        });

        Assert.All(run, r => Assert.True(r.Passed, $"{r.MrId}: {r.FailureReason}"));
        Assert.Equal(4, execs.Data.Count);
        Assert.Equal(4, results.Data.Count);
        Assert.All(execs.Data, e => Assert.Equal("ok", e.Status));
        Assert.Empty(anomalies.Recorded);
    }

    [Fact]
    public void Batch_A_p1_detection_matrix_projects_detected_records_to_anomaly_candidates()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchAP1Heat();

        var report = ExternalMrAcceptanceEvidenceProjector.Project(unit);

        Assert.Equal("minimum-mr-subset-p1-heat", report.SutId);
        Assert.Equal(50, report.TotalDetectionRecords);
        Assert.Equal(10, report.DetectedRecords);
        Assert.Equal(10, report.AnomalyCandidates.Count);
        Assert.Contains(report.AnomalyCandidates, c =>
            c.MrId == "p1-heat-MR01"
            && c.MutationId == "p1-heat-mut_C"
            && c.Limitation.Contains("imported research evidence", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Batch_C_acceptance_catalog_runs_four_remaining_local_put_smoke_mrs_through_launcher()
    {
        var launcher = CreateAcceptanceLauncher(out var execs, out var results, out var anomalies);

        var run = await launcher.RunBatchAsync(new[]
        {
            new BatchMrRunRequest("minmr-p2-wave-amplitude-linearity"),
            new BatchMrRunRequest("minmr-p6-poisson-source-linearity"),
            new BatchMrRunRequest("minmr-p7-burgers-viscosity-damping"),
            new BatchMrRunRequest("minmr-p10-pinn-hnn-loss-smoke"),
        });

        Assert.All(run, r => Assert.True(r.Passed, $"{r.MrId}: {r.FailureReason}"));
        Assert.Equal(4, execs.Data.Count);
        Assert.Equal(4, results.Data.Count);
        Assert.All(execs.Data, e => Assert.Equal("ok", e.Status));
        Assert.Empty(anomalies.Recorded);
    }

    [Fact]
    public void Batch_B_report_records_dependency_gates_and_np_trapz_risk()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchBExistingRuntimeReconcile();

        var report = ExternalMrAcceptanceEvidenceProjector.Project(unit);
        var markdown = ExternalMrAcceptanceEvidenceProjector.RenderMarkdown(report);

        Assert.Equal("minimum-mr-subset-existing-runtime-reconcile", report.SutId);
        Assert.Contains("dependency gates", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("np.trapz", markdown, StringComparison.Ordinal);
        Assert.Contains("minimum-mr-subset-p3", markdown, StringComparison.Ordinal);
        Assert.Contains("minimum-mr-subset-p8", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Batch_D_seeded_fault_report_preserves_limitations_and_deferred_divergence()
    {
        var unit = ExternalMrAcceptancePutFixtures.CreateBatchDScimlDomainValidity();

        var report = ExternalMrAcceptanceEvidenceProjector.Project(unit);
        var markdown = ExternalMrAcceptanceEvidenceProjector.RenderMarkdown(report);

        Assert.Equal("sciml-domain-validity-mgn", report.SutId);
        Assert.Equal(30, report.TotalDetectionRecords);
        Assert.Equal(5, report.DetectedRecords);
        Assert.Contains("one-SUT / one-checkpoint", markdown, StringComparison.Ordinal);
        Assert.Contains("mgn-discrete-divergence-boundedness", markdown, StringComparison.Ordinal);
        Assert.Contains("deferred", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("diagnostic", markdown, StringComparison.OrdinalIgnoreCase);
    }

    private static SutImportUnit CreatePackage(string package) => package switch
    {
        "toy" => ExternalMrAcceptancePutFixtures.CreateBatchAToyClassic(),
        "p1" => ExternalMrAcceptancePutFixtures.CreateBatchAP1Heat(),
        "sciml" => ExternalMrAcceptancePutFixtures.CreateBatchDScimlDomainValidity(),
        "batch-b" => ExternalMrAcceptancePutFixtures.CreateBatchBExistingRuntimeReconcile(),
        "batch-c" => ExternalMrAcceptancePutFixtures.CreateBatchCLocalRemaining(),
        _ => throw new ArgumentOutOfRangeException(nameof(package), package, null)
    };

    private static SystemMtLauncher CreateAcceptanceLauncher(
        out FakeExecRepo execs,
        out FakeResultRepo results,
        out RecordingAnomalyService anomalies)
    {
        execs = new FakeExecRepo();
        results = new FakeResultRepo();
        anomalies = new RecordingAnomalyService();
        var recorder = new SystemMtExecutionRecorder(execs, results);
        var options = new LauncherOptions(
            SutRoot: TestAssetPaths.AssetRoot(),
            SystemPython: TestAssetPaths.PythonExecutable(),
            OpenMocPython: TestAssetPaths.PythonExecutable());
        var manifest = Path.Combine(TestAssetPaths.AssetRoot(), "external_acceptance_minmr", "acceptance-catalog.json");
        return new SystemMtLauncher(
            options,
            new SystemMtPipeline(),
            recorder,
            anomalies,
            new ManifestMrCatalogProvider(options, new[] { manifest }));
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Root { get; }

        private TempDirectory(string root) => Root = root;

        public static TempDirectory Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "MetBenchExternalMrAcceptanceCompletion", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TempDirectory(root);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
