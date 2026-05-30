# T0-T5 VM Release Smoke Evidence

| Field | Value |
|---|---|
| Run id | t0-t5-2026-05-30 |
| VM branch | claude/vm-t0-t5-release-readiness |
| Target production base | b9e917c15683c37466f23e2c4927aecc6cdff8b2 |
| VM summary | vm-summary.md |
| Status stream | vm-status.jsonl |

## Screenshot Evidence Matrix

| Check ID | Layer | Required evidence | Status | Artifact |
|---|---|---|---|---|
| T0-1 | T0 | MR catalog or run page showing selected MR id | PENDING | 02-system-mt-run-or-catalog.png |
| T0-2 | T0 | terminal/UI screenshot showing source/follow-up execution evidence | PENDING | 02-system-mt-run-or-catalog.png |
| T0-3 | T0 | command or UI evidence showing returned metric | PENDING | 02-system-mt-run-or-catalog.png; vm-summary.md |
| T0-4 | T0 | command or UI evidence showing pass/fail assertion | PENDING | 02-system-mt-run-or-catalog.png; 09-anomaly-list.png; vm-summary.md |
| T1-1 | T1 | Windows build and runtime command evidence | PENDING | 01-build-output.png |
| T1-2 | T1 | selected SUT execution evidence | PENDING | 02-system-mt-run-or-catalog.png; vm-summary.md |
| T1-3 | T1 | execution/result persistence evidence | PENDING | 07-execution-history.png |
| T1-4 | T1 | CRUD/editor pages | PENDING | 03-mr-catalog.png; 04-sut-catalog.png; 05-equation-catalog.png; 06-samplecase-catalog.png |
| T1-5 | T1 | WPF user entry pages | PENDING | 03-mr-catalog.png; 04-sut-catalog.png; 05-equation-catalog.png; 06-samplecase-catalog.png; 07-execution-history.png |
| T2-1 | T2 | markdown/HTML report evidence | PENDING | 08-reporting-or-export.png |
| T2-2 | T2 | PDF/Word/Excel report evidence | PENDING | 08-reporting-or-export.png; vm-summary.md |
| T2-3 | T2 | chart/report projection evidence | PENDING | 08-reporting-or-export.png; vm-summary.md |
| T3-1 | T3 | Mono/Inv/Conv selected MR evidence | PENDING | 02-system-mt-run-or-catalog.png |
| T3-2 | T3 | selected SUT/equation evidence | PENDING | 02-system-mt-run-or-catalog.png |
| T3-3 | T3 | catalog denominator command evidence | PENDING | 02-system-mt-run-or-catalog.png; vm-summary.md |
| T4-1 | T4 | binder command evidence or catalog editor surface | PENDING | 03-mr-catalog.png; vm-summary.md |
| T4-2 | T4 | invalid candidate fail-closed command evidence | PENDING | 03-mr-catalog.png; vm-summary.md |
| T5-1 | T5 | failure-to-anomaly evidence | PENDING | 09-anomaly-list.png; vm-summary.md |
| T5-2 | T5 | typed status evidence | PENDING | 10-anomaly-status-action.png |
| T5-3 | T5 | replay/context command evidence | PENDING | 09-anomaly-list.png; 10-anomaly-status-action.png; vm-summary.md |
| T5-4 | T5 | orphan cleanup command/UI evidence | PENDING | 09-anomaly-list.png; 10-anomaly-status-action.png; vm-summary.md |

## Notes

This scaffold is intentionally incomplete until the Windows VM worker fills it
with target-head command results and screenshots.
