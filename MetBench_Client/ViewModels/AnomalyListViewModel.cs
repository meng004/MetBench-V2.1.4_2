using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_Domain;
using MetBench_IDAL;
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

        public AnomalyListViewModel(IAnomalyService service, IAnomalyRepository repo)
        {
            _service = service;
            _repo = repo;
            PageSize = 25;
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
                    Status: string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter);
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
                var ok = _service.TransitionStatus(
                    SelectedAnomaly.IdAnomaly,
                    TransitionTarget!,
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

        partial void OnSelectedAnomalyChanged(Anomaly? value) => TransitionCommand.NotifyCanExecuteChanged();
        partial void OnTransitionTargetChanged(string? value) => TransitionCommand.NotifyCanExecuteChanged();
    }
}
