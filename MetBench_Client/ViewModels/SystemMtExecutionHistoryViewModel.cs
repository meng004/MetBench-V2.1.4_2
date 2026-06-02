using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Persistence.Editing;
using MetBench_UI.Localization;
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

        public LocalizedTextProvider Localization { get; }

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
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _selectedEvidenceSummary = string.Empty;

        public string PaginationSummary
        {
            get
            {
                int totalPages = TotalCount == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;
                return totalPages == 0
                    ? Localization["Pagination_History_NoRows"]
                    : string.Format(Localization["Pagination_History_PageOfTotal_Fmt"], PageIndex + 1, totalPages, TotalCount);
            }
        }

        public SystemMtExecutionHistoryViewModel(IExecutionHistoryEditor editor, LocalizedTextProvider localization)
        {
            _editor = editor;
            Localization = localization;
            StatusMessage = Localization["Status_History_Loading"];
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
                StatusMessage = Localization["Status_History_SelectRowBeforeDelete"];
                return;
            }

            var prompt = targets.Count == 1
                ? string.Format(Localization["Prompt_History_DeleteSingle_Fmt"], targets[0].Id, targets[0].MrName)
                : string.Format(Localization["Prompt_History_DeleteMultiple_Fmt"], targets.Count);

            var confirm = System.Windows.MessageBox.Show(
                prompt, Localization["Prompt_History_ConfirmDeleteTitle"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.OK) return;

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
                    ? Localization["Status_History_NoRecordsMatchFilter"]
                    : string.Format(Localization["Status_History_LoadedRecords_Fmt"], Records.Count, page.TotalCount);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["Status_History_ErrorLoadingExecutions_Fmt"], ex.Message);
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
                    ? Localization["Evidence_History_NoEvidenceRow"]
                    : FormatEvidence(ev);
            }
            catch (Exception ex)
            {
                SelectedEvidenceSummary = string.Format(Localization["Evidence_History_ErrorLoadingEvidence_Fmt"], ex.Message);
            }
        }

        private string FormatEvidence(ExecutionEvidence ev)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_ExecutionId_Fmt"], ev.ExecutionId));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_RecordedAt_Fmt"], string.Format(CultureInfo.InvariantCulture, "{0:u}", ev.RecordedAtUtc)));
            if (ev.TypedVerification is { } tv)
            {
                sb.AppendLine();
                sb.AppendLine(Localization["Evidence_History_TypedVerificationHeader"]);
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_Spec_Fmt"], tv.SpecKind, tv.SpecId));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_Predicate_Fmt"], tv.PredicateKind, tv.PredicateId));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_Status_Fmt"], tv.Status));
                if (tv.Diagnostic is { } d)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_Expected_Fmt"], d.Expected));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_Actual_Fmt"], d.Actual));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_Residual_Fmt"], d.Residual));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_Tolerance_Fmt"], d.Tolerance));
                }
                if (!string.IsNullOrWhiteSpace(tv.SkipOrInvalidReason))
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Localization["Evidence_History_Reason_Fmt"], tv.SkipOrInvalidReason));
            }
            if (HasPairQuality(ev.PairQuality))
                AppendPairQuality(sb, ev.PairQuality);
            return sb.ToString();
        }

        // Mirrors HtmlSystemMtResultReportRenderer.HasPairQuality so a default-empty
        // (legacy) PairQuality stays quiet in the UI just as it does in reports.
        private static bool HasPairQuality(PairQualitySummary? pairQuality)
        {
            if (pairQuality is null)
                return false;

            return pairQuality.PlannedPairs != 0
                || pairQuality.ExecutedPairs != 0
                || pairQuality.ValidPairs != 0
                || pairQuality.PassedPairs != 0
                || pairQuality.FailedPairs != 0
                || pairQuality.SkippedPairs != 0
                || pairQuality.InvalidSpecPairs != 0
                || pairQuality.PassRateValid != 0.0
                || pairQuality.PassRateAll != 0.0
                || pairQuality.SkipReasons.Count != 0
                || pairQuality.InvalidSpecReasons.Count != 0;
        }

        private void AppendPairQuality(StringBuilder sb, PairQualitySummary pq)
        {
            sb.AppendLine();
            sb.AppendLine(Localization["Evidence_History_PairQualityHeader"]);
            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                Localization["Evidence_History_PairQualityCounts_Fmt"],
                pq.PlannedPairs, pq.ExecutedPairs, pq.ValidPairs, pq.PassedPairs,
                pq.FailedPairs, pq.SkippedPairs, pq.InvalidSpecPairs));
            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                Localization["Evidence_History_PairQualityRates_Fmt"],
                pq.PassRateValid.ToString("P1", CultureInfo.InvariantCulture),
                pq.PassRateAll.ToString("P1", CultureInfo.InvariantCulture)));
            AppendReasonDistribution(sb, Localization["Evidence_History_PairQualitySkipReasonsHeader"], pq.SkipReasons);
            AppendReasonDistribution(sb, Localization["Evidence_History_PairQualityInvalidSpecReasonsHeader"], pq.InvalidSpecReasons);
        }

        private void AppendReasonDistribution(StringBuilder sb, string header, IReadOnlyList<PairQualityReasonCount> reasons)
        {
            if (reasons is not { Count: > 0 })
                return;

            sb.AppendLine(header);
            foreach (var reason in reasons)
                sb.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    Localization["Evidence_History_PairQualityReason_Fmt"],
                    reason.Status, reason.Reason, reason.Count));
        }

        private string FormatDeleteOutcome(ExecutionHistoryDeleteResult r)
        {
            var parts = new List<string>();
            if (r.Deleted > 0) parts.Add(string.Format(Localization["Status_History_DeleteDeleted_Fmt"], r.Deleted));
            if (r.EvidenceOnly > 0) parts.Add(string.Format(Localization["Status_History_DeleteEvidenceOnly_Fmt"], r.EvidenceOnly));
            if (r.Failed > 0) parts.Add(string.Format(Localization["Status_History_DeleteFailed_Fmt"], r.Failed));
            return parts.Count == 0 ? Localization["Status_History_NothingDeleted"] : string.Join("; ", parts) + ".";
        }

        private bool CanGoPreviousPage() => PageIndex > 0;
        private bool CanGoNextPage()
        {
            int totalPages = TotalCount == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;
            return PageIndex < totalPages - 1;
        }
    }
}
