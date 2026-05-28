using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Persistence.Editing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public sealed partial class SystemMtExecutionHistoryViewModel : ObservableObject, INavigationAware
    {
        private const int PageSize = 50;

        private readonly IExecutionHistoryEditor _editor;
        private bool _isInitialized;
        private readonly List<SystemMtResultRecord> _selectedRows = new();

        [ObservableProperty]
        private ObservableCollection<SystemMtResultRecord> _records = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedEvidenceSummary))]
        private SystemMtResultRecord? _selectedRecord;

        [ObservableProperty]
        private string _mrNameFilter = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PaginationSummary))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        private int _pageIndex;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PaginationSummary))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        private int _totalCount;

        [ObservableProperty]
        private string _statusMessage = "Loading execution history...";

        [ObservableProperty]
        private string _selectedEvidenceSummary = string.Empty;

        public string PaginationSummary
        {
            get
            {
                int totalPages = TotalCount == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;
                return totalPages == 0
                    ? "No rows"
                    : $"Page {PageIndex + 1} / {totalPages} (total: {TotalCount})";
            }
        }

        public SystemMtExecutionHistoryViewModel(IExecutionHistoryEditor editor)
        {
            _editor = editor;
        }

        public async void OnNavigatedTo()
        {
            if (_isInitialized) return;
            await LoadPageAsync(0).ConfigureAwait(false);
            _isInitialized = true;
        }

        public void OnNavigatedFrom() { }

        partial void OnSelectedRecordChanged(SystemMtResultRecord? value)
        {
            _ = LoadEvidenceAsync(value);
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadPageAsync(0).ConfigureAwait(false);
        }

        [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
        private async Task PreviousPageAsync()
        {
            if (PageIndex > 0)
                await LoadPageAsync(PageIndex - 1).ConfigureAwait(false);
        }

        [RelayCommand(CanExecute = nameof(CanGoNextPage))]
        private async Task NextPageAsync()
        {
            await LoadPageAsync(PageIndex + 1).ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task DeleteSelectedAsync()
        {
            var targets = _selectedRows.Count > 0
                ? _selectedRows.ToList()
                : (SelectedRecord is null ? new List<SystemMtResultRecord>() : new List<SystemMtResultRecord> { SelectedRecord });

            if (targets.Count == 0)
            {
                StatusMessage = "Select at least one row before clicking Delete.";
                return;
            }

            var prompt = targets.Count == 1
                ? $"Delete execution {targets[0].Id} ({targets[0].MrName})? Evidence will be removed first, then the result row. This cannot be undone."
                : $"Delete {targets.Count} execution rows (and their evidence)? This cannot be undone.";

            var confirm = MessageBox.Show(
                prompt, "Confirm delete", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.OK) return;

            var outcome = await _editor.DeleteBatchAsync(targets.Select(r => r.Id)).ConfigureAwait(false);
            StatusMessage = FormatDeleteOutcome(outcome);
            await LoadPageAsync(PageIndex).ConfigureAwait(false);
        }

        internal void UpdateSelectedRowsFromView(IList viewSelectedItems)
        {
            _selectedRows.Clear();
            foreach (var item in viewSelectedItems)
                if (item is SystemMtResultRecord r)
                    _selectedRows.Add(r);
        }

        private async Task LoadPageAsync(int pageIndex)
        {
            try
            {
                var request = new PageRequest(pageIndex, PageSize);
                var page = string.IsNullOrWhiteSpace(MrNameFilter)
                    ? await _editor.ListPagedAsync(request).ConfigureAwait(false)
                    : await _editor.ListPagedByMrNameAsync(MrNameFilter, request).ConfigureAwait(false);

                Records = new ObservableCollection<SystemMtResultRecord>(page.Items);
                PageIndex = page.PageIndex;
                TotalCount = page.TotalCount;
                StatusMessage = page.TotalCount == 0
                    ? "No execution records match the filter."
                    : $"Loaded {Records.Count} of {page.TotalCount} record(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR loading executions: {ex.Message}";
            }
        }

        private async Task LoadEvidenceAsync(SystemMtResultRecord? record)
        {
            if (record is null)
            {
                SelectedEvidenceSummary = string.Empty;
                return;
            }

            try
            {
                var ev = await _editor.GetEvidenceAsync(record.Id).ConfigureAwait(false);
                SelectedEvidenceSummary = ev is null
                    ? "(No evidence row for this execution — legacy or evidence-only-orphan.)"
                    : FormatEvidence(ev);
            }
            catch (Exception ex)
            {
                SelectedEvidenceSummary = $"ERROR loading evidence: {ex.Message}";
            }
        }

        private static string FormatEvidence(ExecutionEvidence ev)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CultureInfo.InvariantCulture, $"ExecutionId: {ev.ExecutionId}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Recorded at: {ev.RecordedAtUtc:u}");
            if (ev.TypedVerification is { } tv)
            {
                sb.AppendLine();
                sb.AppendLine("Typed verification:");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Spec       : {tv.SpecKind} {tv.SpecId}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Predicate  : {tv.PredicateKind} {tv.PredicateId}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Status     : {tv.Status}");
                if (tv.Diagnostic is { } d)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Expected   : {d.Expected}");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Actual     : {d.Actual}");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Residual   : {d.Residual}");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Tolerance  : {d.Tolerance}");
                }
                if (!string.IsNullOrWhiteSpace(tv.SkipOrInvalidReason))
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Reason     : {tv.SkipOrInvalidReason}");
            }
            return sb.ToString();
        }

        private static string FormatDeleteOutcome(ExecutionHistoryDeleteResult r)
        {
            var parts = new List<string>();
            if (r.Deleted > 0) parts.Add($"{r.Deleted} deleted");
            if (r.EvidenceOnly > 0) parts.Add($"{r.EvidenceOnly} evidence-only (result row left for retry)");
            if (r.Failed > 0) parts.Add($"{r.Failed} failed (no rows touched)");
            return parts.Count == 0 ? "Nothing deleted." : string.Join("; ", parts) + ".";
        }

        private bool CanGoPreviousPage() => PageIndex > 0;
        private bool CanGoNextPage()
        {
            int totalPages = TotalCount == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;
            return PageIndex < totalPages - 1;
        }
    }
}
