using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MetBench_BLL.Trend;
using SkiaSharp;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels;

public sealed partial class TrendDashboardViewModel : ObservableObject, INavigationAware
{
    private readonly TrendAnalysisService _service;

    [ObservableProperty]
    private DateTime _weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

    [ObservableProperty]
    private WeeklyReport? _report;

    [ObservableProperty]
    private ISeries[] _chartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ObservableCollection<AnomalyBurst> _bursts = new();

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public TrendDashboardViewModel(TrendAnalysisService service)
    {
        _service = service;
    }

    public void OnNavigatedTo()
    {
        if (Report is null)
            _ = ComputeAsync();
    }

    public void OnNavigatedFrom() { }

    partial void OnWeekStartChanged(DateTime value)
        => _ = ComputeAsync();

    [RelayCommand]
    private async Task ComputeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var weekStart = WeekStart.Date;
            Report = await Task.Run(() => _service.ComputeWeekly(weekStart));
            BuildChart(weekStart);
            Bursts = new ObservableCollection<AnomalyBurst>(Report.Bursts);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Compute error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildChart(DateTime weekStart)
    {
        // Build 5-week execution + anomaly counts (4 prev + current)
        var execValues = new int[5];
        var anomValues = new int[5];
        var labels = new string[5];

        for (int i = 0; i < 5; i++)
        {
            var ws = weekStart.AddDays(-28 + i * 7);
            var r = _service.ComputeWeekly(ws);
            execValues[i] = r.Executions;
            anomValues[i] = r.Anomalies;
            labels[i] = ws.ToString("MM/dd");
        }

        ChartSeries = new ISeries[]
        {
            new LineSeries<int>
            {
                Values = execValues,
                Name = "Executions",
                Stroke = new SolidColorPaint(new SKColor(52, 152, 219), 2),
                Fill = null,
                GeometryStroke = new SolidColorPaint(new SKColor(52, 152, 219), 2),
                GeometrySize = 6,
            },
            new LineSeries<int>
            {
                Values = anomValues,
                Name = "Anomalies",
                Stroke = new SolidColorPaint(new SKColor(231, 76, 60), 2),
                Fill = null,
                GeometryStroke = new SolidColorPaint(new SKColor(231, 76, 60), 2),
                GeometrySize = 6,
            },
        };
    }
}
