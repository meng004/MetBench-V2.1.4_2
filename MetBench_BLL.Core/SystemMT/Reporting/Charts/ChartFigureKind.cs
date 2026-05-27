namespace MetBench_BLL.SystemMT.Reporting.Charts;

/// <summary>
/// Discriminates which downstream renderer / layout the figure expects.
/// Drives the dispatch inside <c>SkiaChartRenderer</c> (PR-T2-2).
/// </summary>
public enum ChartFigureKind
{
    /// <summary>Two-point scatter: source vs follow-up for a 2-side MR.</summary>
    BinaryScatter,

    /// <summary>Ordered line chart over phase roles (multi-phase MR k_eff convergence).</summary>
    PhaseLine,

    /// <summary>Time-series line over N historical runs of the same MR.</summary>
    HistoricalTrend,
}
