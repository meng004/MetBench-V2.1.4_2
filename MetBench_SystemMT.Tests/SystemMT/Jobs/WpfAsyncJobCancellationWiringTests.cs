using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

/// <summary>
/// Cloud-safe source guard for the Windows-only async job consumer.
/// Linux CI cannot compile the WPF project, so this pins the cancellation
/// registry wiring that makes UI Cancel interrupt a running System MT job
/// instead of only marking the persisted record Cancelled.
/// </summary>
public sealed class WpfAsyncJobCancellationWiringTests
{
    [Fact]
    public void App_di_registers_singleton_job_cancellation_registry()
    {
        var app = ReadRepoFile("MetBench_Client", "App.xaml.cs");

        Assert.Contains(
            "services.AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>();",
            app);
    }

    [Fact]
    public void Hosted_worker_passes_registered_cancellation_registry_to_worker()
    {
        var hosted = ReadRepoFile("MetBench_Client", "Hosting", "SystemMtJobWorkerHostedService.cs");

        Assert.Contains("IJobCancellationRegistry", hosted);
        Assert.Contains("new SystemMtJobWorker(", hosted);
        Assert.Contains("_cancellation", hosted);
    }

    [Fact]
    public void Hosted_worker_passes_batch_operation_dispatcher_to_worker()
    {
        var hosted = ReadRepoFile("MetBench_Client", "Hosting", "SystemMtJobWorkerHostedService.cs");

        Assert.Contains("new SystemMtJobOperationDispatcher", hosted);
        Assert.Contains("new RunBatchJobOperationHandler(launcher, evidenceRepository)", hosted);
        Assert.Contains("new ImportAssetsJobOperationHandler(new SutImportStagingService())", hosted);
        Assert.Contains("new ExportAssetsJobOperationHandler()", hosted);
        Assert.Contains("var artifactExporter = new ExecutionArtifactExporter", hosted);
        Assert.Contains("new ExportExecutionArtifactsJobOperationHandler(artifactExporter)", hosted);
        Assert.Contains("new WordSystemMtResultReportRenderer(chartRenderer)", hosted);
        Assert.Contains("new ExcelSystemMtResultReportRenderer(chartRenderer)", hosted);
        Assert.Contains("new PdfSystemMtResultReportRenderer(chartRenderer)", hosted);
        Assert.Contains("new ExportReportJobOperationHandler(artifactExporter)", hosted);
        Assert.Contains("GetRequiredService<ISystemMtResultRepository>()", hosted);
        Assert.Contains("GetRequiredService<ISystemMtResultReportRenderer>()", hosted);
        Assert.Contains("operationDispatcher: operationDispatcher", hosted);
    }

    [Fact]
    public void Hosted_worker_passes_execution_evidence_repository_to_async_pipeline()
    {
        var hosted = ReadRepoFile("MetBench_Client", "Hosting", "SystemMtJobWorkerHostedService.cs");

        Assert.Contains("IExecutionEvidenceRepository", hosted);
        Assert.Contains("GetService<IExecutionEvidenceRepository>()", hosted);
        Assert.Contains("new SystemMtAsyncPipeline(launcher, evidenceRepository)", hosted);
    }

    [Fact]
    public void Async_job_view_model_projects_batch_items_from_polling_status()
    {
        var viewModel = ReadRepoFile("MetBench_Client", "ViewModels", "SystemMtAsyncJobViewModel.cs");

        Assert.Contains("using System.Collections.Generic;", viewModel);
        Assert.Contains("ObservableCollection<string> _batchItemsDisplay", viewModel);
        Assert.Contains("ApplyBatchItems(status.BatchItems)", viewModel);
        Assert.Contains("batch:", viewModel);
    }

    [Fact]
    public void Async_job_view_model_summarizes_batch_assertion_outcomes_without_plain_success()
    {
        var viewModel = ReadRepoFile("MetBench_Client", "ViewModels", "SystemMtAsyncJobViewModel.cs");

        Assert.Contains("DescribeBatchOperationResult(status)", viewModel);
        Assert.Contains("Batch MR assertions:", viewModel);
        Assert.Contains("failed=", viewModel);
        Assert.DoesNotContain("\"RunBatch succeeded\"", viewModel);
    }

    [Fact]
    public void Async_job_view_model_submits_all_release_closure_operation_kinds()
    {
        var viewModel = ReadRepoFile("MetBench_Client", "ViewModels", "SystemMtAsyncJobViewModel.cs");

        Assert.Contains("SubmitOperationAsync", viewModel);
        Assert.Contains("SystemMtOperationJobRequest", viewModel);
        Assert.Contains("SystemMtJobKind.RunMr", viewModel);
        Assert.Contains("SystemMtJobKind.RunBatch", viewModel);
        Assert.Contains("SystemMtJobKind.ImportAssets", viewModel);
        Assert.Contains("SystemMtJobKind.ExportAssets", viewModel);
        Assert.Contains("SystemMtJobKind.ExportExecutionArtifacts", viewModel);
        Assert.Contains("SystemMtJobKind.ExportReport", viewModel);
        Assert.Contains("IsExecutionIdSelected", viewModel);
        Assert.Contains("ArtifactPathDisplay", viewModel);
        Assert.Contains("status.ArtifactPath", viewModel);
    }

    [Fact]
    public void Async_job_page_exposes_operation_selector_and_release_closure_inputs()
    {
        var xaml = ReadRepoFile("MetBench_Client", "Views", "Pages", "SystemMtAsyncJobPage.xaml");

        Assert.Contains("AsyncOperationCombo", xaml);
        Assert.Contains("AsyncMrCombo", xaml);
        Assert.Contains("AsyncBatchMrIdsBox", xaml);
        Assert.Contains("AsyncPackageRootBox", xaml);
        Assert.Contains("AsyncStagingRootBox", xaml);
        Assert.Contains("AsyncExportRootBox", xaml);
        Assert.Contains("AsyncExecutionIdBox", xaml);
        Assert.Contains("AsyncArtifactPath", xaml);
        Assert.Contains("ViewModel.IsExecutionIdSelected", xaml);
    }

    [Fact]
    public void Async_job_page_hides_operation_specific_inputs_when_not_selected()
    {
        var xaml = ReadRepoFile("MetBench_Client", "Views", "Pages", "SystemMtAsyncJobPage.xaml");

        Assert.Contains("ViewModel.IsRunMrSelected, Converter={StaticResource BooleanToVisibilityConverter}", xaml);
        Assert.Contains("ViewModel.IsRunBatchSelected, Converter={StaticResource BooleanToVisibilityConverter}", xaml);
        Assert.Contains("ViewModel.IsPackageRootSelected, Converter={StaticResource BooleanToVisibilityConverter}", xaml);
        Assert.Contains("ViewModel.IsImportAssetsSelected, Converter={StaticResource BooleanToVisibilityConverter}", xaml);
        Assert.Contains("ViewModel.IsExportRootSelected, Converter={StaticResource BooleanToVisibilityConverter}", xaml);
        Assert.Contains("ViewModel.IsExecutionIdSelected, Converter={StaticResource BooleanToVisibilityConverter}", xaml);
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate repository file {Path.Combine(parts)} from {AppContext.BaseDirectory}.");
    }
}
