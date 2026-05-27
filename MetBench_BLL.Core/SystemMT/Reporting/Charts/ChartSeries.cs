namespace MetBench_BLL.SystemMT.Reporting.Charts;

/// <summary>
/// One named line / scatter series. <c>Unit</c> is optional ("cm", "s", "k_eff")
/// for axis-label decoration; downstream renderers may concatenate it onto
/// <see cref="ChartFigure.YAxisLabel"/>.
/// </summary>
public sealed record ChartSeries(
    string Name,
    IReadOnlyList<ChartPoint> Points,
    string? Unit = null);
