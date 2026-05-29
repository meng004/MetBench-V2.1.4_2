namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// 3-segment counter returned by <see cref="IAnomalyOrphanSweeper.SweepAsync"/>.
/// </summary>
/// <param name="SweptCount">Anomaly rows whose ResultId resolved to null and were successfully deleted.</param>
/// <param name="RetainedCount">Anomaly rows whose ResultId still resolves to a live result row; left untouched.</param>
/// <param name="FailedCount">Anomaly rows that should have been deleted but the repository call threw; recorded so the UI can surface partial-progress sweeps.</param>
public sealed record AnomalyOrphanSweepResult(
    int SweptCount,
    int RetainedCount,
    int FailedCount);
