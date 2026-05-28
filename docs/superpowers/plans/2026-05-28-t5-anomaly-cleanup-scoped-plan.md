# T5 Anomaly Cleanup — Scoped Plan (F4 follow-up)

> **Date**: 2026-05-28
> **Status**: Deferred — interface defined, awaits T5 kickoff
> **Source**: T1 非 MR CRUD 链路 4 项 follow-up 计划 §6 (`docs/superpowers/plans/2026-05-28-t1-followups-plan.md`)
> **Driver**: PR #224 (PR-4 ExecHistory R/D) §4 documented Anomaly orphan risk per CLAUDE.md §2.2 T5 ownership

decision-record: docs/superpowers/specs/2026-05-28-sutroot-bin-debug-decision.md (linked sibling decision)

---

## §1 Anomaly orphan scenario

PR-4 (#224) shipped `IExecutionHistoryEditor.DeleteAsync(executionId)` with cross-collection
delete sequencing:

```
1. IExecutionEvidenceRepository.DeleteByExecutionIdAsync(executionId)
2. ISystemMtResultRepository.DeleteAsync(executionId.ToString())
```

`IAnomalyRepository` (`MetBench_IDAL/V2/IAnomalyRepository.cs:7-18`) does **not** participate
in this sequence. `SystemMtLauncher.RecordAnomalyIfFailedAsync` writes Anomaly rows via
`IAnomalyService.RecordAnomalyAsync(resultId, ...)` (see `MetBench_BLL.Core/SystemMT/Anomaly/AnomalyService.cs`).

When the user deletes an Execution via the History page:
- `SystemMtResults` row removed ✓
- `ExecutionEvidence` row removed ✓
- `Anomaly` row that referenced `Result.IdResult = executionId` becomes a **dangling orphan**

Orphan symptoms:
- Anomaly list page (`AnomalyListPage`) still surfaces the orphan row
- Anomaly drill-down (e.g. RCaseReproductionService) tries to resolve linked `Result` → null
- Coverage / commonality analysis pulls in orphans as data noise

---

## §2 T5 接入面 — two candidate routes

### Route A — Cascade delete (extend `IExecutionHistoryEditor`)

Add `IAnomalyRepository.DeleteByResultIdAsync(Guid resultId, CT)` (or similar) and extend
`ExecutionHistoryEditor.DeleteAsync` to 3-step sequencing:

```
1. IExecutionEvidenceRepository.DeleteByExecutionIdAsync(executionId)
2. IAnomalyRepository.DeleteByResultIdAsync(executionId)  // NEW
3. ISystemMtResultRepository.DeleteAsync(executionId.ToString())
```

`ExecutionHistoryDeleteResult` extends from 3-segment `(Deleted, EvidenceOnly, Failed)` to
4-segment `(Deleted, EvidenceOnly, AnomalyOrphan, Failed)` so partial-failure modes remain
exposed to the UI.

**Pro**: Synchronous, no temporal-window orphan. UI delete = clean delete across all 3 collections.

**Con**: Steals T5's design freedom — Anomaly lifecycle now coupled to ExecutionHistory.

### Route B — Orphan sweep service (standalone)

T5 anomaly investigation workflow adds an `IAnomalyOrphanSweeper` (or in-process scheduled
task / on-demand "cleanup" UI button) that:

1. Lists all Anomaly rows
2. For each Anomaly, checks `ISystemMtResultRepository.GetAsync(ResultId)`; if null, mark
   as orphan candidate
3. Bulk-delete confirmed orphans (with audit log)

**Pro**: Preserves Anomaly lifecycle independence. T5 owns Anomaly cleanup policy
(maybe orphans should retain for X days before sweep, or be archived rather than deleted).

**Con**: Temporal window — between ExecutionHistory delete and next sweep, orphans visible
in Anomaly list UI. UI must filter or label orphans.

### Route C — Soft-delete + cascade view

Mark Anomaly rows referencing missing Results as `Status="orphaned"` (a new enum value)
rather than physical delete. AnomalyListPage filters them out by default. Add explicit
"purge orphans" action.

**Pro**: Reversible (if Execution is restored from backup, Anomaly reactivates).
Audit trail intact.

**Con**: Schema migration on `Anomaly.Status` field; UI filter wiring; T5 has more design
surface to define.

---

## §3 Decision deferred to T5

T5 plan author picks Route A / B / C based on:
- Whether T5 deems Anomaly lifecycle should be tightly or loosely coupled to Execution lifecycle
- Whether the project needs Anomaly auditability across deleted Executions (Route C only)
- Whether the existing 3-segment `ExecutionHistoryDeleteResult` API is acceptable to extend or whether T5 prefers an out-of-band sweep

**Recommendation**: Route B (sweep service) for first T5 iteration — preserves API stability,
defers Anomaly lifecycle policy decisions, and matches the existing pattern of standalone
investigation workflow services.

---

## §4 Pre-T5 placeholders (no code changes needed now)

- `IExecutionHistoryEditor.DeleteAsync` and `DeleteBatchAsync` XML doc updated **at T5
  kickoff** to either (Route A) document new 4-segment counter, (Route B) document orphan
  window for caller awareness, or (Route C) document soft-delete semantics
- `IAnomalyRepository` extension: depends on chosen Route
- `MetBench_Client/Views/Pages/AnomalyListPage.xaml.cs` filter wiring: depends on Route B/C
- `AnomalyListViewModel` "purge orphans" command: depends on Route B/C

---

## §5 Inventory: callers that would be impacted

Path-by-path audit at the time of T5 kickoff:

| Caller | File | Today's behavior |
|---|---|---|
| `SystemMtLauncher.RecordAnomalyIfFailedAsync` | `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs` | Writes Anomaly when typed verification fails; no cleanup hook |
| `AnomalyListViewModel` | `MetBench_Client/ViewModels/AnomalyListViewModel.cs` | Reads all Anomalies via `IAnomalyService.ListAnomaliesAsync` |
| `CoverageService` | `MetBench_BLL.Core/Coverage/CoverageService.cs` | Treats every Anomaly as a coverage data point (orphans inflate count) |
| `RCaseReproductionService` | `MetBench_BLL.Core/RCaseRepro/RCaseReproductionService.cs` | Resolves `Anomaly.ResultId` → `Result`; orphan → reproducibility check fails |
| `SystemMtReportService` | `MetBench_BLL.Core/Reporting/SystemMtReportService.cs` | Reports include Anomaly counts; orphans skew metrics |

T5 plan author runs `grep -rln "IAnomalyRepository\|IAnomalyService" MetBench_BLL.Core/ MetBench_Client/` to
re-confirm this inventory at kickoff (codebase may have moved).

---

## §6 Acceptance (when T5 ships)

- [ ] Chosen Route documented in T5 implementation plan
- [ ] If Route A: 4-segment `ExecutionHistoryDeleteResult` + 3-step cascade with regression test
- [ ] If Route B: `IAnomalyOrphanSweeper` + UI integration + audit log per sweep
- [ ] If Route C: `Anomaly.Status="orphaned"` schema migration + UI filter + purge action
- [ ] PR-4 §4 wiring-fix comment updated to note Anomaly orphan no longer happens (link to T5 PR)
- [ ] `docs/status/current.md` §3 PR-4 row update: Anomaly orphan note transitioned from "deferred to T5" to "Controlled via PR-T5-X"

---

## §7 Out of scope for this spec

- Implementing any of the 3 Routes
- T5 anomaly investigation workflow itself (state machine / commonality analysis)
- OpenMOC×OpenMC suspected defect confirmation (separate T5 workstream)
