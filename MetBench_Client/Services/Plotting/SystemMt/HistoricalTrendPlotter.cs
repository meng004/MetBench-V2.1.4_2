using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MetBench_BLL.SystemMT.Reporting.Charts;
using SkiaSharp;

namespace MetBench_Client.Services.Plotting.SystemMt;

/// <summary>
/// Plotter for <see cref="ChartFigureKind.HistoricalTrend"/> — line chart of
/// <c>FollowUpValue</c> (or other metric) across N most-recent runs of the
/// same MR. Produced by <c>HistoricalTrendProjector.ProjectAsync</c>.
/// </summary>
public sealed class HistoricalTrendPlotter : ISystemMtChartPlotter
{
    public SystemMtChartBinding Build(ChartFigure figure)
    {
        var series = new List<ISeries>(figure.SeriesList.Count);
        foreach (var s in figure.SeriesList)
        {
            var points = s.Points.Select(p => new ObservablePoint(p.X, p.Y)).ToList();
            series.Add(new LineSeries<ObservablePoint>
            {
                Values = points,
                Name = s.Name,
                Stroke = new SolidColorPaint(SKColors.SteelBlue, 2),
                Fill = null,
                GeometrySize = 6,
                GeometryStroke = new SolidColorPaint(SKColors.SteelBlue, 2),
                GeometryFill = new SolidColorPaint(SKColors.White),
                LineSmoothness = 0
            });
        }

        var xAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Name = figure.XAxisLabel,
                Labeler = v => v.ToString("G3", CultureInfo.InvariantCulture),
                NameTextSize = 12,
                TextSize = 10
            }
        };
        var yAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Name = figure.YAxisLabel,
                Labeler = v => v.ToString("G3", CultureInfo.InvariantCulture),
                NameTextSize = 12,
                TextSize = 10
            }
        };

        return new SystemMtChartBinding(figure.Title, series, xAxes, yAxes);
    }
}
