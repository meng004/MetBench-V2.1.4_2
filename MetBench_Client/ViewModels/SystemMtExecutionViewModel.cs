using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public partial class SystemMtExecutionViewModel : ObservableObject, INavigationAware
    {
        private readonly ISystemMtLauncher _launcher;
        private readonly ISystemMtResultRepository _repository;
        private readonly ISystemMtResultReportRenderer _reportRenderer;
        private bool _isInitialized;

        [ObservableProperty]
        private ObservableCollection<MrSummary> _availableMrs = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunCommand))]
        private MrSummary? _selectedMr;

        [ObservableProperty]
        private string _factorOverride = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunCommand))]
        private bool _isRunning;

        [ObservableProperty]
        private string _statusMessage = "Idle.";

        [ObservableProperty]
        private string _lastResultSummary = string.Empty;

        [ObservableProperty]
        private ObservableCollection<SystemMtResultRecord> _recentRuns = new();

        public SystemMtExecutionViewModel(
            ISystemMtLauncher launcher,
            ISystemMtResultRepository repository,
            ISystemMtResultReportRenderer reportRenderer)
        {
            _launcher = launcher;
            _repository = repository;
            _reportRenderer = reportRenderer;
        }

        public async void OnNavigatedTo()
        {
            if (_isInitialized) return;
            await LoadMrsAsync();
            await LoadRecentRunsAsync();
            _isInitialized = true;
        }

        public void OnNavigatedFrom() { }

        partial void OnSelectedMrChanged(MrSummary? value)
        {
            FactorOverride = value is not null
                && value.DefaultParameters.TryGetValue("factor", out var defaultFactor)
                ? defaultFactor
                : string.Empty;
        }

        private async Task LoadMrsAsync()
        {
            var list = await _launcher.ListAvailableAsync();
            AvailableMrs = new ObservableCollection<MrSummary>(list);
            if (SelectedMr is null && AvailableMrs.Count > 0)
            {
                SelectedMr = AvailableMrs[0];
            }
        }

        private async Task LoadRecentRunsAsync()
        {
            var recent = await _repository.ListRecentAsync(50);
            RecentRuns = new ObservableCollection<SystemMtResultRecord>(recent);
        }

        [RelayCommand(CanExecute = nameof(CanRun))]
        private async Task RunAsync()
        {
            if (SelectedMr is null) return;

            IsRunning = true;
            StatusMessage = $"Running {SelectedMr.DisplayName}…";
            LastResultSummary = string.Empty;

            try
            {
                IReadOnlyDictionary<string, string>? overrides = null;
                if (!string.IsNullOrWhiteSpace(FactorOverride))
                {
                    overrides = new Dictionary<string, string> { ["factor"] = FactorOverride.Trim() };
                }

                var result = await _launcher.RunAsync(SelectedMr.Id, overrides);

                LastResultSummary = result.Passed
                    ? $"PASS — {result.ValueName}: source={result.SourceValue:G}, follow-up={result.FollowUpValue:G}"
                    : $"FAIL — {result.FailureReason}";
                StatusMessage = $"Completed in source={result.SourceElapsed.TotalSeconds:F2}s, follow-up={result.FollowUpElapsed.TotalSeconds:F2}s.";

                await LoadRecentRunsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: {ex.Message}";
                System.Windows.MessageBox.Show(ex.ToString(), "System-MT run failed", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private bool CanRun() => SelectedMr is not null && !IsRunning;

        [RelayCommand]
        private async Task RefreshRecentAsync()
        {
            await LoadRecentRunsAsync();
            StatusMessage = $"Refreshed: {RecentRuns.Count} record(s).";
        }

        [RelayCommand]
        private async Task ExportReportAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "HTML report (*.html)|*.html",
                FileName = $"system-mt-report-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.html",
            };
            if (dialog.ShowDialog() != true) return;

            var records = await _repository.ListRecentAsync(500);
            var html = _reportRenderer.Render(records);
            await File.WriteAllTextAsync(dialog.FileName, html);
            StatusMessage = $"Report exported: {dialog.FileName}";
        }
    }
}
