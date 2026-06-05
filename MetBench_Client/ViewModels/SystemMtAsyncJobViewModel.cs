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
    private ObservableCollection<string> _availableMrIds = new();

    [ObservableProperty]
    private string _jobIdDisplay = "-";

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
        if (!CanSubmit()) return;

        ResetForNewJob();
        var handle = await _jobs.SubmitAsync(new SystemMtJobRequest(SelectedMrId)).ConfigureAwait(true);
        _currentJobId = handle.JobId;
        JobIdDisplay = handle.JobId.ToString();
        StateDisplay = SystemMtJobState.Queued.ToString();
        SutNameDisplay = "-";
        PhaseDisplay = "queued";
        ProgressPercent = 0;
        PollLog.Add($"{DateTime.UtcNow:HH:mm:ss.fff} {SystemMtJobState.Queued} / queued / 0%");
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
        if (_currentJobId is not { } id) return;

        await _jobs.CancelAsync(id).ConfigureAwait(true);
        await PollOnceAsync().ConfigureAwait(true);
    }

    private bool CanSubmit()
    {
        return IsNotRunning && !string.IsNullOrWhiteSpace(SelectedMrId);
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
    }

    private async Task PollOnceAsync()
    {
        if (_currentJobId is not { } id) return;

        var status = await _jobs.GetStatusAsync(id).ConfigureAwait(true);
        if (status is null) return;

        StateDisplay = status.State.ToString();
        SutNameDisplay = string.IsNullOrWhiteSpace(status.SutName) ? "-" : status.SutName;
        PhaseDisplay = string.IsNullOrWhiteSpace(status.CurrentPhase) ? "-" : status.CurrentPhase;
        ProgressPercent = status.ProgressPercent;
        FailureReason = status.FailureReason;
        ApplyBatchItems(status.BatchItems);
        var batchSuffix = BatchItemsDisplay.Count == 0 ? string.Empty : $" / batch: {string.Join(", ", BatchItemsDisplay)}";
        PollLog.Add($"{status.UpdatedAtUtc:HH:mm:ss.fff} {status.State} / {PhaseDisplay} / {status.ProgressPercent}%{batchSuffix}");

        if (status.State.IsTerminal())
        {
            _pollTimer.Stop();
            IsRunning = false;
            await LoadResultAsync(id, status.State).ConfigureAwait(true);
        }
    }

    private async Task LoadResultAsync(Guid id, SystemMtJobState finalState)
    {
        if (finalState == SystemMtJobState.Succeeded)
        {
            var result = await _jobs.GetResultAsync(id).ConfigureAwait(true);
            ResultSummary = result is null ? "(no result)" : DescribeResult(result);
            return;
        }

        ResultSummary = $"{finalState}: {FailureReason ?? "(no reason)"}";
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

    private void ApplyBatchItems(IReadOnlyList<SystemMtBatchJobItem>? items)
    {
        BatchItemsDisplay.Clear();
        if (items is null || items.Count == 0)
            return;

        foreach (var item in items)
        {
            var reason = string.IsNullOrWhiteSpace(item.FailureReason) ? string.Empty : $" ({item.FailureReason})";
            BatchItemsDisplay.Add($"{item.MrId}: {item.State}{reason}");
        }
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
        SutNameDisplay = "-";
        PhaseDisplay = "-";
    }
}
