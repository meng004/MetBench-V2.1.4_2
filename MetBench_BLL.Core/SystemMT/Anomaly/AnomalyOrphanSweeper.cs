using MetBench_BLL.SystemMT.Persistence;
using MetBench_IDAL;

namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// Default <see cref="IAnomalyOrphanSweeper"/> backed by
/// <see cref="IAnomalyRepository"/> and <see cref="ISystemMtResultRepository"/>.
/// </summary>
/// <remarks>
/// Implements F4 scoped plan Route B (independent sweeper, preserves Anomaly
/// lifecycle independence). Single-row failures are caught + counted; the
/// sweep does not abort on the first failure so all reachable orphans are
/// removed in one pass.
///
/// Report-only anomalies (<see cref="ReportOnlyCategories"/>) are <b>exempt</b>
/// from sweeping: they were never produced by a live MT pipeline run and
/// therefore never had a backing <see cref="MetBench_Domain.Result"/> row, so
/// a missing result is by-design, not orphanhood. The cross-program
/// disagreement findings seeded by <c>tools/SeedCrossProgramAnomalies</c>
/// (PR-2) carry synthetic <c>ResultId</c> GUIDs purely to satisfy the unique
/// <c>ResultId</c> index — those GUIDs intentionally do not resolve, and this
/// exemption prevents the sweeper from deleting legitimate report-only findings.
/// </remarks>
public sealed class AnomalyOrphanSweeper : IAnomalyOrphanSweeper
{
    /// <summary>
    /// Anomaly categories that are report-only (no backing Result by design)
    /// and therefore must never be treated as orphans. Matched case-insensitively.
    /// </summary>
    private static readonly HashSet<string> ReportOnlyCategories =
        new(StringComparer.OrdinalIgnoreCase) { "cross-program-disagreement" };

    private readonly IAnomalyRepository _anomalies;
    private readonly ISystemMtResultRepository _results;

    public AnomalyOrphanSweeper(IAnomalyRepository anomalies, ISystemMtResultRepository results)
    {
        _anomalies = anomalies ?? throw new ArgumentNullException(nameof(anomalies));
        _results = results ?? throw new ArgumentNullException(nameof(results));
    }

    public async Task<AnomalyOrphanSweepResult> SweepAsync(CancellationToken cancellationToken = default)
    {
        int swept = 0;
        int retained = 0;
        int failed = 0;

        var all = _anomalies.GetAll();
        foreach (var anomaly in all)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Report-only categories (e.g. cross-program disagreement) never had
            // a backing Result row; their unresolvable ResultId is by-design, not
            // orphanhood. Retain them unconditionally.
            if (anomaly.Category is { } category && ReportOnlyCategories.Contains(category))
            {
                retained++;
                continue;
            }

            SystemMtResultRecord? result;
            try
            {
                result = await _results.GetAsync(anomaly.ResultId.ToString(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // Cannot resolve — treat as inconclusive; do not delete the
                // anomaly (data-safety) but count as failed so caller knows
                // sweep was partial.
                failed++;
                continue;
            }

            if (result is not null)
            {
                retained++;
                continue;
            }

            // Result is gone → anomaly is orphan; remove it.
            try
            {
                if (_anomalies.Remove(anomaly))
                {
                    swept++;
                }
                else
                {
                    failed++;
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                failed++;
            }
        }

        return new AnomalyOrphanSweepResult(swept, retained, failed);
    }
}
