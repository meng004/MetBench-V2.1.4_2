# T0-T5 VM Release Smoke Summary

| Field | Value |
|---|---|
| Run id | t0-t5-2026-05-30 |
| Branch | claude/vm-t0-t5-release-readiness |
| HEAD | 5f9e71247f6beb43ef43c85787943577720f566e |
| Target production base | b9e917c15683c37466f23e2c4927aecc6cdff8b2 |
| Production-code delta from target base | NO |
| Command checks | 22/22 filtered commands passed |
| Filtered test count | 255 passed / 0 failed |
| Full suite | 1558 passed / 0 failed / 12 env-gated OpenMC-OpenMOC skips |
| Screenshot matrix | complete: 21/21 PASS |
| Final VM decision | PASS |

## Evidence Notes

- T0/T1/T3 selected SUT-MR smoke passed for heat-equation/amplitude, advection-1d/mesh-conservation, and wave-1d/mesh-energy-convergence.
- T1 catalog/editor/history, T2 reporting, T4 binder/governance, and T5 anomaly/replay command groups all exited 0.
- UIA smokeshot artifacts cover WPF catalog, execution/history, reporting, and anomaly surfaces.
- 10-anomaly-status-action.png is command-backed status/action evidence because the reusable UIA harness does not yet navigate a dedicated anomaly-status-action page.

## Blockers

None for this scoped release-readiness smoke.
