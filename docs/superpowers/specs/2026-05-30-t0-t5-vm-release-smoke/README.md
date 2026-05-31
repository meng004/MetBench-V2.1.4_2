# T0-T5 VM Release Smoke Evidence

| Field | Value |
|---|---|
| Run id | t0-t5-2026-05-30 |
| VM branch | claude/vm-t0-t5-release-readiness |
| HEAD | 73abaf88ae6cd5442f911a6803cce5735183001b |
| Target production base | b9e917c15683c37466f23e2c4927aecc6cdff8b2 |
| Production-code delta from target base | NO (docs/prompt/hook/evidence only) |
| VM summary | vm-summary.md |
| Status stream | vm-status.jsonl |

## Screenshot Evidence Matrix

| Check ID | Layer | Required evidence | Status | Artifact |
|---|---|---|---|---|
| T0-1 | T0 | MR catalog or run page showing selected MR id | PASS | 02-system-mt-run-or-catalog.png; 03-mr-catalog.png |
| T0-2 | T0 | source/follow-up execution evidence | PASS | 02-system-mt-run-or-catalog.png (Source/Follow-up columns); 01-build-output.png |
| T0-3 | T0 | returned metric | PASS | 02-system-mt-run-or-catalog.png (Value=peak_amplitude); 01-build-output.png |
| T0-4 | T0 | pass/fail assertion | PASS | 02-system-mt-run-or-catalog.png (Passed column); 09-anomaly-list.png; 01-build-output.png |
| T1-1 | T1 | Windows build and runtime command evidence | PASS | 01-build-output.png; full-suite.log |
| T1-2 | T1 | selected SUT execution evidence | PASS | 02-system-mt-run-or-catalog.png; t0t5-test-summary.txt |
| T1-3 | T1 | execution/result persistence evidence | PASS | 07-execution-history.png (3 records persisted) |
| T1-4 | T1 | CRUD/editor pages | PASS | 03-mr-catalog.png; 04-sut-catalog.png; 05-equation-catalog.png; 06-samplecase-catalog.png |
| T1-5 | T1 | WPF user entry pages | PASS | 02..09 + 11-replay.png (10 distinct System-MT pages launched and navigated) |
| T2-1 | T2 | markdown/HTML report evidence | PASS | 08-reporting-or-export.png; 02-system-mt-run-or-catalog.png (Export HTML report); t0t5-test-summary.txt |
| T2-2 | T2 | PDF/Word/Excel report evidence | PASS | 08-reporting-or-export.png; t0t5-test-summary.txt (Pdf/Word/Excel renderer tests) |
| T2-3 | T2 | chart/report projection evidence | PASS | t0t5-test-summary.txt (SystemMT.Reporting.Charts) |
| T3-1 | T3 | Mono/Inv/Conv selected MR evidence | PASS | 01-build-output.png (heat Mono / advection Inv / wave Conv filters) |
| T3-2 | T3 | selected SUT/equation evidence | PASS | 04-sut-catalog.png (16 SUT); 05-equation-catalog.png (13 equations) |
| T3-3 | T3 | catalog denominator command evidence | PASS | 03-mr-catalog.png (16 manifests); t0t5-test-summary.txt (Catalog_MR_id_set_equals_governance_whitelist) |
| T4-1 | T4 | binder/catalog editor surface | PASS | 03-mr-catalog.png; t0t5-test-summary.txt (DiscoveredMrCatalogBinderTests) |
| T4-2 | T4 | invalid candidate fail-closed | PASS | t0t5-test-summary.txt (DiscoveredMrCatalogBinderTests fail-closed cases) |
| T5-1 | T5 | failure-to-anomaly evidence | PASS | 09-anomaly-list.png; t0t5-test-summary.txt (RunAsync_persists_failure_when_assertion_fails) |
| T5-2 | T5 | typed status evidence | PASS | 10-anomaly-status-action.png; t0t5-test-summary.txt (AnomalyStatusTests, AnomalyStatusPersistenceTests) |
| T5-3 | T5 | replay/context evidence | PASS | 11-replay.png; t0t5-test-summary.txt (ReplayServiceTests, ReplayContextBuilderTests) |
| T5-4 | T5 | orphan cleanup evidence | PASS | 09-anomaly-list.png (Sweep orphans control); t0t5-test-summary.txt (AnomalyOrphanSweeperTests) |

## Screenshot Provenance

All 01-11 PNGs were regenerated in this run (2026-05-30) against HEAD `73abaf8`,
each a distinct genuine capture verified by inspection:

- 02-09, 11: live `MetBench_Client` pages captured via UIA navigation
  (`tools/release-readiness/capture_uia.ps1`, PrintWindow PW_RENDERFULLCONTENT).
  Navigation uses a physical mouse-click fallback because Wpf.Ui
  `NavigationViewItem` exposes neither Invoke nor SelectionItem patterns. All eight
  of 02-09 have distinct byte sizes and distinct page content.
- 10: Anomalies page with the status filter restored to `investigating` (both
  persisted anomalies visible: one `major`, one `critical`) and the transition
  target combo set to `confirmed-bug`. Limitation: the DataGrid row-select did not
  latch via UIA, so "Apply transition" remains disabled in the frame; the typed
  status values, the persisted rows, and the chosen transition target are all shown,
  and the status machine itself is command-verified (AnomalyStatusTests,
  AnomalyStatusPersistenceTests both pass).
- 01: real self-run command output (BUILD exit=0; required filters PASS) rendered
  to PNG via GDI from `t0t5-test-summary.txt` + `full-suite.log` tail.

The previous run's `smoke-*.png` set and the byte-identical 02-09 copies it produced
were removed; they were not distinct-page evidence.

## Command Evidence Files

- `t0t5-test-summary.txt` - 22/22 required filters PASS (build exit 0).
- `t0t5-test-run.log` - full per-filter dotnet test output (~200 KB).
- `full-suite.log` - full cloud suite: 1558 passed / 0 failed / 12 env-gated skips.
