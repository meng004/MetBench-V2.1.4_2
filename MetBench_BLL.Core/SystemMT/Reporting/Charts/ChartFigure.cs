namespace MetBench_BLL.SystemMT.Reporting.Charts;

/// <summary>
/// Pure-data description of a chart. Renderer-agnostic: consumed by
/// <c>SkiaChartRenderer</c> in PR-T2-2 and the PDF / Word / Excel embedders in
/// PR-T2-3a/3b/3c. No SkiaSharp / LiveCharts type leaks into this DTO so it
/// stays in <c>MetBench_BLL.Core</c>.
/// </summary>
public sealed record ChartFigure(
    string Title,
    string XAxisLabel,
    string YAxisLabel,
    IReadOnlyList<ChartSeries> SeriesList,
    ChartFigureKind Kind);
