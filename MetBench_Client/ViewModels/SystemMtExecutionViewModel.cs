using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using MetBench_UI.Localization;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace MetBench_Client.ViewModels
{
    public partial class SystemMtExecutionViewModel : ObservableObject, INavigationAware
    {
        private readonly ISystemMtLauncher _launcher;
        private readonly ISystemMtResultRepository _repository;
        private readonly ISystemMtResultReportRenderer _reportRenderer;
        private bool _isInitialized;

        public LocalizedTextProvider Localization { get; }

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
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _lastResultSummary = string.Empty;

        [ObservableProperty]
        private ObservableCollection<SystemMtResultRecord> _recentRuns = new();

        public SystemMtExecutionViewModel(
            ISystemMtLauncher launcher,
            ISystemMtResultRepository repository,
            ISystemMtResultReportRenderer reportRenderer,
            LocalizedTextProvider localization)
        {
            _launcher = launcher;
            _repository = repository;
            _reportRenderer = reportRenderer;
            Localization = localization;
            StatusMessage = Localization["Status_Execution_Idle"];
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
            StatusMessage = string.Format(Localization["Status_Execution_Running_Fmt"], SelectedMr.DisplayName);
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
                    ? string.Format(Localization["Status_Execution_ResultPass_Fmt"], result.ValueName, result.SourceValue, result.FollowUpValue)
                    : string.Format(Localization["Status_Execution_ResultFail_Fmt"], result.FailureReason);
                StatusMessage = string.Format(Localization["Status_Execution_Completed_Fmt"], result.SourceElapsed.TotalSeconds, result.FollowUpElapsed.TotalSeconds);

                await LoadRecentRunsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["Status_Execution_Error_Fmt"], ex.Message);
                System.Windows.MessageBox.Show(ex.ToString(), Localization["Status_Execution_RunFailed_Title"], System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
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
            StatusMessage = string.Format(Localization["Status_Execution_Refreshed_Fmt"], RecentRuns.Count);
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
            StatusMessage = string.Format(Localization["Status_Execution_ReportExported_Fmt"], dialog.FileName);
        }
    }
}
