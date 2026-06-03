using System;
using MetBench_BLL.SystemMT.Reporting.Charts;

namespace MetBench_Client.Services.Plotting.SystemMt;

/// <summary>
/// Dispatches a <see cref="ChartFigure"/> to the appropriate plotter by its
/// <see cref="ChartFigureKind"/>. Constructor-injected through DI so each
/// plotter can be independently substituted in tests.
/// </summary>
public sealed class SystemMtChartPlotterFactory
{
    private readonly BinaryRunPlotter _binary;
    private readonly PhaseConvergencePlotter _phase;
    private readonly HistoricalTrendPlotter _history;

    public SystemMtChartPlotterFactory(
        BinaryRunPlotter binary,
        PhaseConvergencePlotter phase,
        HistoricalTrendPlotter history)
    {
        _binary = binary;
        _phase = phase;
        _history = history;
    }

    public SystemMtChartBinding Build(ChartFigure figure) => figure.Kind switch
    {
        ChartFigureKind.BinaryScatter => _binary.Build(figure),
        ChartFigureKind.PhaseLine => _phase.Build(figure),
        ChartFigureKind.HistoricalTrend => _history.Build(figure),
        _ => throw new NotSupportedException(
            $"Unknown ChartFigureKind: {figure.Kind}")
    };
}
