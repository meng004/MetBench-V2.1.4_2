# Smoke Summary

_Generated: 2026-05-30 14:15:02 +08:00_
_Total: 40.4s_

| # | Flow | Args | Status | Seconds |
|---|------|------|--------|---------|
| 1 | nav-all (PR #29 steps 4-10 nav) | `nav-all --out C:\Users\codex\metbench-t0-t5-release-readiness\docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke` | [OK] | 16.3 |
| 2 | Step 1: Application Management add | `app-add openmoc-smoke --out C:\Users\codex\metbench-t0-t5-release-readiness\docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke` | [OK] | 4 |
| 3 | Step 2: MR Management add | `mr-add MR-smoke-test --out C:\Users\codex\metbench-t0-t5-release-readiness\docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke` | [OK] | 4 |
| 4 | Step 3: MT Execution (OpenMOC gated) | `mt-exec --out C:\Users\codex\metbench-t0-t5-release-readiness\docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke` | [SKIP] | 3.5 |
| 5 | MetaPatterns CRUD: list / page2 / toggle | `metapatterns --out C:\Users\codex\metbench-t0-t5-release-readiness\docs\superpowers\specs\2026-05-30-t0-t5-vm-release-smoke` | [FAIL] | 4.3 |

## Screenshots captured

- `smoke-01-app-management.png` 鈥?231 KB
- `smoke-02-mr-management.png` 鈥?232 KB
- `smoke-03-mt-page-no-openmoc.png` 鈥?233 KB
- `smoke-04-anomalies.png` 鈥?230 KB
- `smoke-06-discovery.png` 鈥?230 KB
- `smoke-08-mutation.png` 鈥?230 KB
- `smoke-09-coverage.png` 鈥?230 KB
- `smoke-meta-01-list.png` 鈥?233 KB

## Exit code legend
- `[OK]` 0   鈥?flow completed all steps
- `[FAIL]` 1 鈥?flow ran but a step failed or element not found
- `[SKIP]` 2 鈥?flow intentionally skipped (e.g., OpenMOC venv missing for Step 3)
- `[APP-NOT-RUNNING]` 3 鈥?MetBench_Client.exe not running
