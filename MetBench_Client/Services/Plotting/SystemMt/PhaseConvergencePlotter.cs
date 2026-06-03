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
/// Plotter for <see cref="ChartFigureKind.PhaseLine"/>. Note: PR-W1 ships the
/// plotter but the ViewModel does NOT currently route any UI flow into it —
/// <see cref="MetBench_BLL.SystemMT.Persistence.SystemMtResultRecord"/> does
/// not carry per-phase metrics, so there is no record-side data source for a
/// phase chart yet. Kept here so the factory dispatch is exhaustive and a
/// later follow-up can wire phase data (e.g. via <c>ExecutionEvidence</c>
/// lookup) without changing the plotter surface.
/// </summary>
public sealed class PhaseConvergencePlotter : ISystemMtChartPlotter
{
    private static readonly SKColor[] _palette =
    {
        SKColors.SteelBlue, SKColors.DarkOrange, SKColors.MediumSeaGreen,
        SKColors.IndianRed, SKColors.MediumPurple, SKColors.Teal
    };

    public SystemMtChartBinding Build(ChartFigure figure)
    {
        var phaseLabels = figure.SeriesList.FirstOrDefault()?.Points
            .Select(p => p.Label ?? p.X.ToString("G3", CultureInfo.InvariantCulture))
            .ToList()
            ?? new List<string>();

        var series = new List<ISeries>(figure.SeriesList.Count);
        for (var i = 0; i < figure.SeriesList.Count; i++)
        {
            var s = figure.SeriesList[i];
            var color = _palette[i % _palette.Length];
            var points = s.Points.Select(p => new ObservablePoint(p.X, p.Y)).ToList();
            series.Add(new LineSeries<ObservablePoint>
            {
                Values = points,
                Name = s.Name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = null,
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(SKColors.White)
            });
        }

        var xAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Name = figure.XAxisLabel,
                Labels = phaseLabels,
                LabelsRotation = 0,
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
