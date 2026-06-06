using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels;

public sealed partial class SystemMtAsyncJobViewModel : ObservableObject, INavigationAware
{
    private readonly ISystemMtJobService _jobs;
    private readonly ISystemMtLauncher _launcher;
    private readonly DispatcherTimer _pollTimer;
    private Guid? _currentJobId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _selectedMrId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunMrSelected))]
    [NotifyPropertyChangedFor(nameof(IsRunBatchSelected))]
    [NotifyPropertyChangedFor(nameof(IsPackageRootSelected))]
    [NotifyPropertyChangedFor(nameof(IsImportAssetsSelected))]
    [NotifyPropertyChangedFor(nameof(IsExportRootSelected))]
    [NotifyPropertyChangedFor(nameof(IsExportAssetsSelected))]
    [NotifyPropertyChangedFor(nameof(IsExportExecutionArtifactsSelected))]
    [NotifyPropertyChangedFor(nameof(IsExportReportSelected))]
    [NotifyPropertyChangedFor(nameof(IsExecutionIdSelected))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private SystemMtJobKind _selectedOperationKind = SystemMtJobKind.RunMr;

    [ObservableProperty]
    private ObservableCollection<SystemMtJobKind> _availableOperationKinds = new(
        new[]
        {
            SystemMtJobKind.RunMr,
            SystemMtJobKind.RunBatch,
            SystemMtJobKind.ImportAssets,
            SystemMtJobKind.ExportAssets,
            SystemMtJobKind.ExportExecutionArtifacts,
            SystemMtJobKind.ExportReport,
        });

    [ObservableProperty]
    private ObservableCollection<string> _availableMrIds = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _batchMrIdsText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _packageRoot = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _stagingRoot = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _exportRoot = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _executionIdText = string.Empty;

    [ObservableProperty]
    private string _jobIdDisplay = "-";

    [ObservableProperty]
    private string _operationKindDisplay = "-";

    [ObservableProperty]
    private string _stateDisplay = "-";

    [ObservableProperty]
    private string _sutNameDisplay = "-";

    [ObservableProperty]
    private string _phaseDisplay = "-";

    [ObservableProperty]
    private int _progressPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFailureReason))]
    private string? _failureReason;

    [ObservableProperty]
    private string _resultSummary = string.Empty;

    [ObservableProperty]
    private string _artifactPathDisplay = "-";

    [ObservableProperty]
    private ObservableCollection<string> _pollLog = new();

    [ObservableProperty]
    private ObservableCollection<string> _batchItemsDisplay = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRunning))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isRunning;

    public bool IsNotRunning => !IsRunning;

    public bool HasFailureReason => !string.IsNullOrWhiteSpace(FailureReason);

    public bool IsRunMrSelected => SelectedOperationKind == SystemMtJobKind.RunMr;

    public bool IsRunBatchSelected => SelectedOperationKind == SystemMtJobKind.RunBatch;

    public bool IsPackageRootSelected => IsImportAssetsSelected || IsExportAssetsSelected;

    public bool IsImportAssetsSelected => SelectedOperationKind == SystemMtJobKind.ImportAssets;

    public bool IsExportRootSelected => IsExportAssetsSelected || IsExportExecutionArtifactsSelected || IsExportReportSelected;

    public bool IsExportAssetsSelected => SelectedOperationKind == SystemMtJobKind.ExportAssets;

    public bool IsExportExecutionArtifactsSelected => SelectedOperationKind == SystemMtJobKind.ExportExecutionArtifacts;

    public bool IsExportReportSelected => SelectedOperationKind == SystemMtJobKind.ExportReport;

    public bool IsExecutionIdSelected => IsExportExecutionArtifactsSelected || IsExportReportSelected;

    public SystemMtAsyncJobViewModel(ISystemMtJobService jobs, ISystemMtLauncher launcher)
    {
        _jobs = jobs;
        _launcher = launcher;
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pollTimer.Tick += async (_, _) => await PollOnceAsync().ConfigureAwait(true);
    }

    public async void OnNavigatedTo()
    {
        await LoadMrIdsAsync().ConfigureAwait(true);
    }

    public void OnNavigatedFrom()
    {
        _pollTimer.Stop();
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        if (!CanSubmit())
        {
            return;
        }

        ResetForNewJob();
        var request = BuildOperationRequest();
        var handle = await _jobs.SubmitOperationAsync(request).ConfigureAwait(true);
        _currentJobId = handle.JobId;
        JobIdDisplay = handle.JobId.ToString();
        OperationKindDisplay = request.Kind.ToString();
        StateDisplay = SystemMtJobState.Queued.ToString();
        SutNameDisplay = "-";
        PhaseDisplay = "queued";
        ProgressPercent = 0;
        PollLog.Add($"{DateTime.UtcNow:HH:mm:ss.fff} {request.Kind} / {SystemMtJobState.Queued} / queued / 0%");
        IsRunning = true;

        _pollTimer.Start();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await PollOnceAsync().ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private async Task CancelAsync()
    {
        if (_currentJobId is not { } id)
        {
            return;
        }

        await _jobs.CancelAsync(id).ConfigureAwait(true);
        await PollOnceAsync().ConfigureAwait(true);
    }

    private bool CanSubmit()
    {
        if (!IsNotRunning)
        {
            return false;
        }

        return SelectedOperationKind switch
        {
            SystemMtJobKind.RunMr => !string.IsNullOrWhiteSpace(SelectedMrId),
            SystemMtJobKind.RunBatch => ParseBatchMrIds().Count > 0,
            SystemMtJobKind.ImportAssets => !string.IsNullOrWhiteSpace(PackageRoot)
                && !string.IsNullOrWhiteSpace(StagingRoot),
            SystemMtJobKind.ExportAssets => !string.IsNullOrWhiteSpace(PackageRoot)
                && !string.IsNullOrWhiteSpace(ExportRoot),
            SystemMtJobKind.ExportExecutionArtifacts => Guid.TryParse(ExecutionIdText, out var id)
                && id != Guid.Empty
                && !string.IsNullOrWhiteSpace(ExportRoot),
            SystemMtJobKind.ExportReport => Guid.TryParse(ExecutionIdText, out var id)
                && id != Guid.Empty
                && !string.IsNullOrWhiteSpace(ExportRoot),
            _ => false,
        };
    }

    private bool CanCancel()
    {
        return IsRunning && _currentJobId.HasValue;
    }

    private async Task LoadMrIdsAsync()
    {
        var summaries = await _launcher.ListAvailableAsync().ConfigureAwait(true);
        var ids = summaries.Select(s => s.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();
        AvailableMrIds = new ObservableCollection<string>(ids);
        if (AvailableMrIds.Count > 0 && string.IsNullOrWhiteSpace(SelectedMrId))
        {
            SelectedMrId = AvailableMrIds[0];
        }

        if (AvailableMrIds.Count > 0 && string.IsNullOrWhiteSpace(BatchMrIdsText))
        {
            BatchMrIdsText = string.Join(", ", AvailableMrIds.Take(2));
        }
    }

    private async Task PollOnceAsync()
    {
        if (_currentJobId is not { } id)
        {
            return;
        }

        var status = await _jobs.GetStatusAsync(id).ConfigureAwait(true);
        if (status is null)
        {
            return;
        }

        OperationKindDisplay = status.Kind.ToString();
        StateDisplay = status.State.ToString();
        SutNameDisplay = string.IsNullOrWhiteSpace(status.SutName) ? "-" : status.SutName;
        PhaseDisplay = string.IsNullOrWhiteSpace(status.CurrentPhase) ? "-" : status.CurrentPhase;
        ProgressPercent = status.ProgressPercent;
        FailureReason = status.FailureReason;
        ArtifactPathDisplay = string.IsNullOrWhiteSpace(status.ArtifactPath) ? "-" : status.ArtifactPath;
        ApplyBatchItems(status.BatchItems);
        var batchSuffix = BatchItemsDisplay.Count == 0 ? string.Empty : $" / batch: {string.Join(", ", BatchItemsDisplay)}";
        var artifactSuffix = string.IsNullOrWhiteSpace(status.ArtifactPath) ? string.Empty : $" / artifact: {status.ArtifactPath}";
        PollLog.Add($"{status.UpdatedAtUtc:HH:mm:ss.fff} {status.Kind} / {status.State} / {PhaseDisplay} / {status.ProgressPercent}%{batchSuffix}{artifactSuffix}");

        if (status.State.IsTerminal())
        {
            _pollTimer.Stop();
            IsRunning = false;
            await LoadResultAsync(id, status).ConfigureAwait(true);
        }
    }

    private async Task LoadResultAsync(Guid id, SystemMtJobStatus status)
    {
        if (status.State == SystemMtJobState.Succeeded)
        {
            if (status.Kind != SystemMtJobKind.RunMr)
            {
                ResultSummary = status.Kind == SystemMtJobKind.RunBatch
                    ? DescribeBatchOperationResult(status)
                    : DescribeOperationResult(status);
                return;
            }

            var result = await _jobs.GetResultAsync(id).ConfigureAwait(true);
            ResultSummary = result is null ? "(no result)" : DescribeResult(result);
            return;
        }

        ResultSummary = $"{status.Kind} {status.State}: {FailureReason ?? "(no reason)"}";
    }

    private static string DescribeResult(MrRunResult result)
    {
        var verdict = result.Passed ? "MR assertion passed" : "MR assertion failed";
        return string.Join(
            Environment.NewLine,
            verdict,
            $"MR: {result.MrId}",
            $"Record: {result.RecordId}",
            $"Value: {result.ValueName}",
            $"Source: {result.SourceValue:G17}",
            $"Follow-up: {result.FollowUpValue:G17}",
            $"Source elapsed: {result.SourceElapsed}",
            $"Follow-up elapsed: {result.FollowUpElapsed}",
            string.IsNullOrWhiteSpace(result.FailureReason) ? "Failure reason: -" : $"Failure reason: {result.FailureReason}");
    }

    private static string DescribeOperationResult(SystemMtJobStatus status)
    {
        var lines = new List<string>
        {
            $"{status.Kind} succeeded",
            $"Job: {status.JobId}",
            $"Artifact path: {Readable(status.ArtifactPath)}",
        };

        if (!string.IsNullOrWhiteSpace(status.PackageRoot))
        {
            lines.Add($"Package root: {status.PackageRoot}");
        }

        if (!string.IsNullOrWhiteSpace(status.StagingRoot))
        {
            lines.Add($"Staging root: {status.StagingRoot}");
        }

        if (!string.IsNullOrWhiteSpace(status.ExportRoot))
        {
            lines.Add($"Export root: {status.ExportRoot}");
        }

        if (status.ExecutionId is { } executionId)
        {
            lines.Add($"Execution id: {executionId}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string DescribeBatchOperationResult(SystemMtJobStatus status)
    {
        var items = status.BatchItems ?? Array.Empty<SystemMtBatchJobItem>();
        var passed = items.Count(item => item.State == SystemMtBatchItemState.Succeeded);
        var failed = items.Count(item => item.State == SystemMtBatchItemState.Failed);
        var cancelled = items.Count(item => item.State == SystemMtBatchItemState.Cancelled);
        var pending = items.Count(item => item.State is SystemMtBatchItemState.Pending or SystemMtBatchItemState.Running);
        var lines = new List<string>
        {
            "RunBatch completed",
            $"Job: {status.JobId}",
            $"Batch MR assertions: total={items.Count}; passed={passed}; failed={failed}; cancelled={cancelled}; pending={pending}",
            failed == 0 && cancelled == 0 && pending == 0
                ? "All completed MR assertions passed."
                : "Batch contains non-passing MR items.",
        };

        foreach (var item in items)
        {
            var reason = string.IsNullOrWhiteSpace(item.FailureReason) ? string.Empty : $" ({item.FailureReason})";
            lines.Add($"{item.MrId}: {item.State}{reason}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void ApplyBatchItems(IReadOnlyList<SystemMtBatchJobItem>? items)
    {
        BatchItemsDisplay.Clear();
        if (items is null || items.Count == 0)
        {
            return;
        }

        foreach (var item in items)
        {
            var reason = string.IsNullOrWhiteSpace(item.FailureReason) ? string.Empty : $" ({item.FailureReason})";
            BatchItemsDisplay.Add($"{item.MrId}: {item.State}{reason}");
        }
    }

    private SystemMtOperationJobRequest BuildOperationRequest()
    {
        return SelectedOperationKind switch
        {
            SystemMtJobKind.RunMr => new SystemMtOperationJobRequest(
                SystemMtJobKind.RunMr,
                MrId: SelectedMrId),
            SystemMtJobKind.RunBatch => new SystemMtOperationJobRequest(
                SystemMtJobKind.RunBatch,
                MrIds: ParseBatchMrIds()),
            SystemMtJobKind.ImportAssets => new SystemMtOperationJobRequest(
                SystemMtJobKind.ImportAssets,
                PackageRoot: PackageRoot.Trim(),
                StagingRoot: StagingRoot.Trim()),
            SystemMtJobKind.ExportAssets => new SystemMtOperationJobRequest(
                SystemMtJobKind.ExportAssets,
                PackageRoot: PackageRoot.Trim(),
                ExportRoot: ExportRoot.Trim()),
            SystemMtJobKind.ExportExecutionArtifacts => new SystemMtOperationJobRequest(
                SystemMtJobKind.ExportExecutionArtifacts,
                ExportRoot: ExportRoot.Trim(),
                ExecutionId: Guid.Parse(ExecutionIdText.Trim())),
            SystemMtJobKind.ExportReport => new SystemMtOperationJobRequest(
                SystemMtJobKind.ExportReport,
                ExportRoot: ExportRoot.Trim(),
                ExecutionId: Guid.Parse(ExecutionIdText.Trim())),
            _ => throw new InvalidOperationException($"Unsupported async operation: {SelectedOperationKind}"),
        };
    }

    private IReadOnlyList<string> ParseBatchMrIds()
    {
        return BatchMrIdsText
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private void ResetForNewJob()
    {
        _pollTimer.Stop();
        PollLog.Clear();
        BatchItemsDisplay.Clear();
        FailureReason = null;
        ResultSummary = string.Empty;
        ProgressPercent = 0;
        StateDisplay = "-";
        OperationKindDisplay = "-";
        SutNameDisplay = "-";
        PhaseDisplay = "-";
        ArtifactPathDisplay = "-";
    }

    private static string Readable(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
}
