namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// Cleans up Anomaly rows whose <c>ResultId</c> no longer resolves to a row
/// in <see cref="MetBench_BLL.SystemMT.Persistence.ISystemMtResultRepository"/>.
/// </summary>
/// <remarks>
/// Background: <c>IExecutionHistoryEditor.DeleteAsync</c> (PR-4 #224) removes
/// <see cref="MetBench_BLL.SystemMT.Persistence.SystemMtResultRecord"/> and
/// <see cref="MetBench_BLL.SystemMT.Persistence.ExecutionEvidence"/> in lockstep
/// but does <b>not</b> cascade-delete <see cref="MetBench_Domain.Anomaly"/>
/// rows that reference the deleted result. PR-4 §4 explicitly documented this
/// as orphan risk and deferred resolution to T5 (F4 scoped plan
/// <c>docs/superpowers/plans/2026-05-28-t5-anomaly-cleanup-scoped-plan.md</c>
/// recommended <b>Route B</b>: an independent sweeper service that lets the
/// Anomaly lifecycle remain orthogonal to ExecutionHistory deletion).
///
/// Implementations should be idempotent — running sweep twice in a row when
/// nothing changes between calls must return <c>SweptCount = 0</c> on the
/// second pass. Single-row failures during sweep do not abort the run;
/// failure counts surface in <see cref="AnomalyOrphanSweepResult.FailedCount"/>
/// so the UI can report partial progress to the user.
/// </remarks>
public interface IAnomalyOrphanSweeper
{
    /// <summary>
    /// Scan all <see cref="MetBench_Domain.Anomaly"/> rows. For each one whose
    /// <c>ResultId</c> no longer resolves to a row in the result repository,
    /// delete it. Returns 3-segment counters describing the outcome.
    /// </summary>
    Task<AnomalyOrphanSweepResult> SweepAsync(CancellationToken cancellationToken = default);
}
