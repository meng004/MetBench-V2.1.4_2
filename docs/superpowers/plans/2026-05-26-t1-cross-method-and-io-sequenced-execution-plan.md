# T1 Cross-Method + Non-JSON I/O Sequenced Execution Plan

> **For agentic workers:** Mandatory execution order for the next two cloud-side T1 PRs. Mirrors the shape of the previous PR-0 / PR-1 / PR-2 sequenced gate (`2026-05-26-t1-t4-ui-sequenced-execution-plan.md`).

## Context

`docs/status/current.md` §3 currently lists two remaining cloud-executable T1 work items:

1. **T1 same-equation cross-method differential** — implementation in `MetBench_BLL.Core/SystemMT/Differential/`. Plan: `2026-05-26-t1-cross-method-differential-runner-plan.md`.
2. **T1 non-JSON I/O adapter** — Python helper + test SUT. Plan: `2026-05-26-t1-non-json-io-adapter-plan.md`.

Both plans declare strict cloud-side scope (no Method MT, no WPF, no SUT execution edits beyond a small synthetic `_test_csv` test SUT in plan 2).

## Mandatory order

1. **PR-0 (this PR)** — docs-only gate. Registers the two scoped plans + this orchestration plan in the active plan index and adds the two `Open` rows in the status ledger. **Must merge first.**
2. **PR-B `feat(t1): add same-equation cross-method differential runner`** — implements plan 1. Adds `IDifferentialTestRunner` + DTOs + sealed default implementation + ~14 facts. No new SUT, no MR, no pinned-count bump.
3. **PR-A `feat(t1): add cross-format SUT I/O helper + CSV roundtrip test SUT`** — implements plan 2. Adds `SUT/_shared/metbench_io/`, `SUT/_test_csv/` synthetic SUT, one MR (`csv-roundtrip-identity`), and bumps pinned counts 29 → 30 / 15 → 16. **Must NOT start until PR-B is merged** because PR-A's pinned-count bump and the differential runner's test surface both touch the same descriptor list — sequencing avoids a merge conflict / pinned-count drift on the same files.

Each subsequent PR must:

- Cite this plan in its description.
- Re-verify preconditions at the start (origin/main reachable, prior PRs merged, no new T1 plan supersedes ours).
- Land its own status-ledger and active-plan-index edits so the ledger never goes stale between PRs.

## Out of scope for the whole gate

- No Method MT changes anywhere in the gate.
- No WPF / `MetBench_Client/` / `App.xaml.cs` edits anywhere.
- No new external Python venv (PR-A uses the existing `system` runtime — no FEniCS / FiPy / torch-surrogate work).
- No new EquationMetadata except the synthetic `_test_csv` row in PR-A (clearly leading-underscore marked).
- No T4 binder edits.
- No UI MR CRUD — that remains the Windows/VM plan from the prior orchestration.

## Expiry

This plan expires once **both** PR-B and PR-A have merged AND `docs/status/current.md` shows both rows as Controlled. At that point this file moves to §3 of the active plan index (already-merged section), and the only outstanding T1 row is the Windows/VM UI MR CRUD plan from the prior gate.
