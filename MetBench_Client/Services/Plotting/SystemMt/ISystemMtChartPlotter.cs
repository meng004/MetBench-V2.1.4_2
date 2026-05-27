using MetBench_BLL.SystemMT.Reporting.Charts;

namespace MetBench_Client.Services.Plotting.SystemMt;

/// <summary>
/// Converts a renderer-agnostic <see cref="ChartFigure"/> (produced by the
/// Linux-side projectors in <c>MetBench_BLL.Core/SystemMT/Reporting/Charts/</c>)
/// into a LiveCharts-WPF bundle for on-screen rendering. One implementation
/// per <see cref="ChartFigureKind"/>.
/// </summary>
public interface ISystemMtChartPlotter
{
    SystemMtChartBinding Build(ChartFigure figure);
}
