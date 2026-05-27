using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting.Charts;
using MetBench_Client.Services.Plotting.SystemMt;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels;

/// <summary>
/// Chart view modes the result page can render. Phase mode is reserved for a
/// follow-up — <see cref="SystemMtResultRecord"/> does not currently carry the
/// per-phase metric dictionary that <c>PhaseConvergenceProjector</c> needs.
/// </summary>
public enum ChartViewMode
{
    Binary,
    Historical
}

public partial class SystemMtResultViewModel : ObservableObject, INavigationAware
{
    private readonly ISystemMtResultRepository _repo;
    private readonly HistoricalTrendProjector _historyProjector;
    private readonly SystemMtChartPlotterFactory _plotterFactory;

    private CancellationTokenSource _projectionCts = new();

    [ObservableProperty]
    private ObservableCollection<SystemMtResultRecord> _records = new();

    [ObservableProperty]
    private SystemMtResultRecord? _selectedRecord;

    [ObservableProperty]
    private ChartViewMode _viewMode = ChartViewMode.Binary;

    [ObservableProperty]
    private bool _isHistoricalView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChartVisibility))]
    [NotifyPropertyChangedFor(nameof(EmptyOverlayVisibility))]
    private SystemMtChartBinding? _chartBinding;

    [ObservableProperty]
    private bool _canShowHistoricalView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BusyVisibility))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public Visibility ChartVisibility =>
        ChartBinding is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility EmptyOverlayVisibility =>
        ChartBinding is null ? Visibility.Visible : Visibility.Collapsed;

    public Visibility BusyVisibility =>
        IsBusy ? Visibility.Visible : Visibility.Collapsed;

    public SystemMtResultViewModel(
        ISystemMtResultRepository repo,
        HistoricalTrendProjector historyProjector,
        SystemMtChartPlotterFactory plotterFactory)
    {
        _repo = repo;
        _historyProjector = historyProjector;
        _plotterFactory = plotterFactory;
    }

    public async void OnNavigatedTo() => await RefreshAsync();

    public void OnNavigatedFrom() { }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "正在加载结果...";
            // ISystemMtResultRepository does not expose "load every record" —
            // use ListRecentAsync (limit 100 is sufficient for a viewer; users
            // wanting more should narrow by MR via ListByMrNameAsync).
            var all = await _repo.ListRecentAsync(limit: 100, CancellationToken.None);
            Records.Clear();
            foreach (var r in all) Records.Add(r);
            SelectedRecord = Records.FirstOrDefault();
            StatusMessage = Records.Count == 0
                ? "暂无 SystemMT 运行结果，请先在执行页运行 MR"
                : $"加载 {Records.Count} 条记录";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedRecordChanged(SystemMtResultRecord? value)
    {
        UpdateViewModeAvailability(value);
        TriggerProjection();
    }

    partial void OnViewModeChanged(ChartViewMode value) => TriggerProjection();

    partial void OnIsHistoricalViewChanged(bool value) =>
        ViewMode = value ? ChartViewMode.Historical : ChartViewMode.Binary;

    private void UpdateViewModeAvailability(SystemMtResultRecord? record)
    {
        if (record is null)
        {
            CanShowHistoricalView = false;
            return;
        }

        var sameMrCount = Records.Count(r =>
            string.Equals(r.MrName, record.MrName, StringComparison.Ordinal));
        CanShowHistoricalView = sameMrCount >= 2;

        if (ViewMode == ChartViewMode.Historical && !CanShowHistoricalView)
        {
            // Auto-fallback (spec §3.2): if user-currently-selected mode becomes
            // unavailable for the new record, drop to Binary and tell the user.
            IsHistoricalView = false;
            StatusMessage = "历史数据点不足 (<2)，已切回 Binary 视图";
        }
    }

    private void TriggerProjection()
    {
        if (SelectedRecord is null)
        {
            ChartBinding = null;
            return;
        }

        _projectionCts.Cancel();
        _projectionCts = new CancellationTokenSource();
        var ct = _projectionCts.Token;
        _ = ProjectAsync(SelectedRecord, ViewMode, ct);
    }

    private async Task ProjectAsync(
        SystemMtResultRecord record,
        ChartViewMode mode,
        CancellationToken ct)
    {
        try
        {
            // BinaryRunPointProjector is `public static class` — call directly.
            // HistoricalTrendProjector is an instance (ctor takes repo);
            // ProjectAsync takes (mrId, lookbackRuns, ct) only — repo is not a param.
            ChartFigure figure = mode switch
            {
                ChartViewMode.Binary => BinaryRunPointProjector.Project(record),
                ChartViewMode.Historical => await _historyProjector.ProjectAsync(
                    record.MrName, lookbackRuns: 20, ct),
                _ => throw new NotSupportedException($"Unknown view mode: {mode}")
            };

            ct.ThrowIfCancellationRequested();

            var binding = _plotterFactory.Build(figure);
            ChartBinding = binding;

            var nanCount = figure.SeriesList
                .SelectMany(s => s.Points)
                .Count(p => double.IsNaN(p.X) || double.IsNaN(p.Y)
                         || double.IsInfinity(p.X) || double.IsInfinity(p.Y));
            if (nanCount > 0)
                StatusMessage = $"提示：{nanCount} 个数据点为 NaN/Inf，已跳过";
        }
        catch (OperationCanceledException)
        {
            // expected on rapid record/view-mode switch — silent
        }
        catch (Exception ex)
        {
            StatusMessage = $"图表投影失败：{ex.Message}";
            ChartBinding = null;
        }
    }
}
