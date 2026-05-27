using System;
using System.Collections.Generic;

namespace MetBench_BLL.SystemMT.Reporting.Charts;

/// <summary>
/// Projects the per-phase metric dictionary produced by
/// <c>SystemMtPipeline.ExecuteMultiPhaseAsync</c> (PR-Bol-2A) into an ordered
/// line chart. Phase X-axis is the integer index (0..N-1) — the role name is
/// carried in <see cref="ChartPoint.Label"/> so the renderer can label ticks.
///
/// Input shape matches <c>PipelineOutcome.PhaseMetrics</c> exactly:
/// outer key = role, inner dict = metric name → numeric value.
/// </summary>
public static class PhaseConvergenceProjector
{
    public static ChartFigure Project(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> phaseMetrics,
        string mrId,
        string metric)
    {
        if (phaseMetrics is null) throw new ArgumentNullException(nameof(phaseMetrics));
        if (string.IsNullOrWhiteSpace(mrId)) throw new ArgumentException("mrId is required", nameof(mrId));
        if (string.IsNullOrWhiteSpace(metric)) throw new ArgumentException("metric is required", nameof(metric));

        // ErrorMonotonicPredicateValidator demands OrderedRoles ≥ 2 (= total phases ≥ 3 with last
        // being the ReferenceRole). Anything less is meaningless for convergence rendering.
        if (phaseMetrics.Count < 2)
        {
            throw new ArgumentException(
                $"PhaseConvergenceProjector requires at least 2 phases; got {phaseMetrics.Count}",
                nameof(phaseMetrics));
        }

        var points = new List<ChartPoint>(phaseMetrics.Count);
        var x = 0;
        foreach (var (role, metrics) in phaseMetrics)
        {
            if (!metrics.TryGetValue(metric, out var value))
            {
                throw new ArgumentException(
                    $"Phase '{role}' is missing metric '{metric}'; available keys: {string.Join(", ", metrics.Keys)}",
                    nameof(phaseMetrics));
            }
            points.Add(new ChartPoint(x, value, Label: role));
            x++;
        }

        var series = new ChartSeries(
            Name: $"{mrId} · {metric}",
            Points: points,
            Unit: null);

        return new ChartFigure(
            Title: $"{mrId} — {metric} convergence across phases",
            XAxisLabel: "phase",
            YAxisLabel: metric,
            SeriesList: new[] { series },
            Kind: ChartFigureKind.PhaseLine);
    }
}
