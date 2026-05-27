namespace MetBench_BLL.SystemMT.Reporting.Charts;

/// <summary>
/// One (X, Y) datapoint with an optional human-readable label
/// (phase role name, run timestamp, etc.) for tooltip / axis-tick rendering.
/// </summary>
public sealed record ChartPoint(double X, double Y, string? Label = null);
