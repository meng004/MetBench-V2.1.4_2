using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting.Charts;

/// <summary>
/// Projects a single <see cref="SystemMtResultRecord"/> into a 2-point scatter
/// (source vs follow-up). Stateless / pure; usable from any thread.
/// </summary>
public static class BinaryRunPointProjector
{
    public static ChartFigure Project(SystemMtResultRecord record)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        var valueName = string.IsNullOrWhiteSpace(record.ValueName) ? "value" : record.ValueName;
        var mrName = string.IsNullOrWhiteSpace(record.MrName) ? "(unknown MR)" : record.MrName;

        var series = new ChartSeries(
            Name: $"{mrName} · {valueName}",
            Points: new[]
            {
                new ChartPoint(0.0, record.SourceValue, Label: "source"),
                new ChartPoint(1.0, record.FollowUpValue, Label: "follow-up"),
            },
            Unit: null);

        return new ChartFigure(
            Title: $"{mrName} — {valueName} (source vs follow-up)",
            XAxisLabel: "side",
            YAxisLabel: valueName,
            SeriesList: new[] { series },
            Kind: ChartFigureKind.BinaryScatter);
    }
}
