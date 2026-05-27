using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting.Charts;

/// <summary>
/// Projects the last N runs of a single MR (via
/// <see cref="ISystemMtResultRepository.ListByMrNameAsync"/>) into a time-series
/// line chart of <c>FollowUpValue</c>. Useful for spotting drift across runs.
///
/// Repository returns most-recent first; this projector reverses the order so
/// X axis runs oldest → newest left-to-right.
/// </summary>
public sealed class HistoricalTrendProjector
{
    private readonly ISystemMtResultRepository _repository;

    public HistoricalTrendProjector(ISystemMtResultRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ChartFigure> ProjectAsync(
        string mrId,
        int lookbackRuns,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mrId)) throw new ArgumentException("mrId is required", nameof(mrId));
        if (lookbackRuns <= 0) throw new ArgumentOutOfRangeException(nameof(lookbackRuns), lookbackRuns, "lookbackRuns must be > 0");

        var rawRecent = await _repository.ListByMrNameAsync(mrId, lookbackRuns, cancellationToken).ConfigureAwait(false);

        // Reverse to chronological order; tolerate empty / null repository response.
        var oldestFirst = new List<SystemMtResultRecord>(rawRecent ?? Array.Empty<SystemMtResultRecord>());
        oldestFirst.Reverse();

        var metric = oldestFirst.Count > 0 && !string.IsNullOrWhiteSpace(oldestFirst[0].ValueName)
            ? oldestFirst[0].ValueName
            : "follow_up_value";

        var points = new List<ChartPoint>(oldestFirst.Count);
        for (var i = 0; i < oldestFirst.Count; i++)
        {
            var record = oldestFirst[i];
            var label = record.RunAt.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
            points.Add(new ChartPoint(i, record.FollowUpValue, Label: label));
        }

        var series = new ChartSeries(
            Name: $"{mrId} · {metric}",
            Points: points,
            Unit: null);

        return new ChartFigure(
            Title: $"{mrId} — {metric} historical trend (last {oldestFirst.Count} runs)",
            XAxisLabel: "run index (oldest → newest)",
            YAxisLabel: metric,
            SeriesList: new[] { series },
            Kind: ChartFigureKind.HistoricalTrend);
    }
}
