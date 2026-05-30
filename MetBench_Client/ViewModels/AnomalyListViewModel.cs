using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_Client.Services;
using MetBench_Domain;
using MetBench_IDAL;
using MetBench_UI.Localization;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    /// <summary>
    /// v2 P6 配套 — Anomaly 调查列表页 ViewModel。
    /// 继承通用 <see cref="PagingViewModel{T}"/>，只实现 <c>LoadPageAsync</c>。
    /// </summary>
    public sealed partial class AnomalyListViewModel
        : PagingViewModel<Anomaly>, INavigationAware
    {
        private readonly IAnomalyService _service;
        private readonly IAnomalyRepository _repo;
        private readonly IAnomalyOrphanSweeper _orphanSweeper;
        private readonly ReplayInbox _replayInbox;
        private readonly INavigationService _navigationService;

        public LocalizedTextProvider Localization { get; }

        [ObservableProperty]
        private string? _severityFilter;

        [ObservableProperty]
        private string? _statusFilter;

        [ObservableProperty]
        private Anomaly? _selectedAnomaly;

        [ObservableProperty]
        private string? _transitionTarget;

        [ObservableProperty]
        private CommonalityReport? _commonality;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _sweepStatus;

        public AnomalyListViewModel(
            IAnomalyService service,
            IAnomalyRepository repo,
            IAnomalyOrphanSweeper orphanSweeper,
            ReplayInbox replayInbox,
            INavigationService navigationService,
            LocalizedTextProvider localization)
        {
            _service = service;
            _repo = repo;
            _orphanSweeper = orphanSweeper;
            _replayInbox = replayInbox;
            _navigationService = navigationService;
            Localization = localization;
            PageSize = 25;
        }

        [RelayCommand]
        private async Task SweepOrphansAsync()
        {
            try
            {
                var result = await _orphanSweeper.SweepAsync().ConfigureAwait(true);
                SweepStatus = $"Swept {result.SweptCount}, retained {result.RetainedCount}, failed {result.FailedCount}.";
                ErrorMessage = null;
                await LoadAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                SweepStatus = $"Sweep failed: {ex.Message}";
            }
        }

        public async void OnNavigatedTo()
        {
            try
            {
                await LoadAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public void OnNavigatedFrom() { }

        protected override Task<PagedResult<Anomaly>> LoadPageAsync(PageRequest req, CancellationToken ct)
        {
            try
            {
                ErrorMessage = null;

                // Path A: 无过滤 — 走仓库分页（DB-side skip/limit）
                if (string.IsNullOrEmpty(SeverityFilter) && string.IsNullOrEmpty(StatusFilter))
                {
                    var pageItems = _repo.GetPage(req.PageIndex, req.PageSize);
                    var total = _repo.Count();
                    return Task.FromResult(new PagedResult<Anomaly>(
                        pageItems.ToList(), total, req.PageIndex, req.PageSize));
                }

                // Path B: 有过滤 — 走 BLL service，内存切片
                var filter = new AnomalyFilter(
                    Severity: string.IsNullOrEmpty(SeverityFilter) ? null : SeverityFilter,
                    Status: AnomalyStatuses.TryParseKebab(StatusFilter, out var statusFilter) ? statusFilter : (AnomalyStatus?)null);
                var all = _service.List(filter);
                var slice = all.Skip(req.Skip).Take(req.PageSize).ToList();
                return Task.FromResult(new PagedResult<Anomaly>(
                    slice, all.Count, req.PageIndex, req.PageSize));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Task.FromResult(PagedResult<Anomaly>.Empty(req.PageIndex, req.PageSize));
            }
        }

        partial void OnSeverityFilterChanged(string? value) =>
            _ = LoadAsync(PageRequest.First(PageSize));

        partial void OnStatusFilterChanged(string? value) =>
            _ = LoadAsync(PageRequest.First(PageSize));

        [RelayCommand]
        private void AnalyzeCommonality()
        {
            try
            {
                Commonality = _service.AnalyzeCommonalities(Items.ToList());
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        [RelayCommand(CanExecute = nameof(CanTransition))]
        private async Task TransitionAsync()
        {
            if (SelectedAnomaly is null || string.IsNullOrEmpty(TransitionTarget))
                return;

            try
            {
                if (!AnomalyStatuses.TryParseKebab(TransitionTarget, out var target))
                {
                    ErrorMessage = $"Unknown status '{TransitionTarget}'.";
                    return;
                }

                var ok = _service.TransitionStatus(
                    SelectedAnomaly.IdAnomaly,
                    target,
                    notes: null,
                    actor: "wpf-user");

                if (!ok)
                {
                    ErrorMessage = $"TransitionStatus returned false for anomaly {SelectedAnomaly.IdAnomaly}.";
                    return;
                }

                ErrorMessage = null;
                await LoadAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private bool CanTransition() =>
            !IsLoading && SelectedAnomaly is not null && !string.IsNullOrEmpty(TransitionTarget);

        [RelayCommand(CanExecute = nameof(CanReplay))]
        private void Replay()
        {
            if (SelectedAnomaly is null) return;
            try
            {
                _replayInbox.PendingAnomaly = SelectedAnomaly;
                _replayInbox.LastResult = null;
                _navigationService.Navigate(typeof(Views.Pages.ReplayResultPage));
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private bool CanReplay() => SelectedAnomaly is not null;

        partial void OnSelectedAnomalyChanged(Anomaly? value)
        {
            TransitionCommand.NotifyCanExecuteChanged();
            ReplayCommand.NotifyCanExecuteChanged();
        }

        partial void OnTransitionTargetChanged(string? value) => TransitionCommand.NotifyCanExecuteChanged();
    }
}
