using System;
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
/// follow-up because <see cref="SystemMtResultRecord"/> does not currently carry
/// the per-phase metric dictionary that <c>PhaseConvergenceProjector</c> needs.
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

    public bool IsBinaryView
    {
        get => !IsHistoricalView;
        set
        {
            if (value)
            {
                IsHistoricalView = false;
            }
        }
    }

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

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading SystemMT results...";
            // ISystemMtResultRepository does not expose "load every record";
            // use ListRecentAsync for the viewer's initial page.
            var all = await _repo.ListRecentAsync(limit: 100, CancellationToken.None);
            Records.Clear();
            foreach (var result in all)
            {
                Records.Add(result);
            }

            SelectedRecord = Records.FirstOrDefault();
            StatusMessage = Records.Count == 0
                ? "No SystemMT results yet. Run an MR from the execution page first."
                : $"Loaded {Records.Count} result records.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load results: {ex.Message}";
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

    partial void OnIsHistoricalViewChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBinaryView));
        ViewMode = value ? ChartViewMode.Historical : ChartViewMode.Binary;
    }

    private void UpdateViewModeAvailability(SystemMtResultRecord? record)
    {
        if (record is null)
        {
            CanShowHistoricalView = false;
            return;
        }

        var sameMrCount = Records.Count(candidate =>
            string.Equals(candidate.MrName, record.MrName, StringComparison.Ordinal));
        CanShowHistoricalView = sameMrCount >= 2;

        if (ViewMode == ChartViewMode.Historical && !CanShowHistoricalView)
        {
            IsHistoricalView = false;
            StatusMessage = "Historical data has fewer than 2 points; switched back to Binary view.";
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
            ChartFigure figure = mode switch
            {
                ChartViewMode.Binary => BinaryRunPointProjector.Project(record),
                ChartViewMode.Historical => await _historyProjector.ProjectAsync(
                    record.MrName, lookbackRuns: 20, ct),
                _ => throw new NotSupportedException($"Unknown view mode: {mode}")
            };

            ct.ThrowIfCancellationRequested();

            ChartBinding = _plotterFactory.Build(figure);

            var nanCount = figure.SeriesList
                .SelectMany(series => series.Points)
                .Count(point => double.IsNaN(point.X)
                             || double.IsNaN(point.Y)
                             || double.IsInfinity(point.X)
                             || double.IsInfinity(point.Y));
            if (nanCount > 0)
            {
                StatusMessage = $"Skipped {nanCount} NaN/Inf chart points.";
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on rapid record/view-mode switches.
        }
        catch (Exception ex)
        {
            StatusMessage = $"Chart projection failed: {ex.Message}";
            ChartBinding = null;
        }
    }
}
