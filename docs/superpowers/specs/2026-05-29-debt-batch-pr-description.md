# PR Description Draft — Follow-up debts batch (#2 #3 #4 #5)

> Branch `followup/debts-2026-05-29` → `main`. Fill into GitHub PR body. Sections follow
> `docs/superpowers/templates/pr-gate-checklist.md`. **Do not open until WPF (Task 4E) VM-verified.**

---

## Scope

Discharges 4 governance/code debts surfaced in the 2026-05-29 project assessment (Stryker P4 gate deliberately **excluded** — deferred per `docs/status/current.md`):

- **#2 (governance):** Fix the broken Check-5 grep in `dotnet-test.yml` — it expected numbered headers + "Soft Review" and was warning on every compliant PR; now matches the actual un-numbered PR-gate-checklist headers (`Windows Classification`, `AI Review`).
- **#3 (docs):** Disambiguate the two inventory layers (`44 MR + 4 Property` v1.2 typed-migration denominator vs `33 MR / 16 SUT / 13 eq` runtime catalog) that were conflated under an absolute "inventory truth" phrasing.
- **#4 (docs):** Drop the stale `e839214` baseline snapshot from CLAUDE.md's conventions layer (§11.3 boundary — baselines live in the status ledger).
- **#5 (code):** `Anomaly.Status` string → `AnomalyStatus` enum with an explicit state machine (illegal transitions now throw, previously doc-only), LiteDB int serialization + idempotent string→int migration, and a fixed LiteDB LINQ enum-translation bug.

## Facts

- `dotnet test MetBench_SystemMT.Tests` → **1556 passed / 0 failed / 12 skipped** (cloud/mac).
- `dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj` → 0 errors.
- New tests: `AnomalyStatusTests` (pinned int values, kebab round-trip, transition table), `AnomalyStatusPersistenceTests` (int-on-disk, GetByStatus, migration). Existing anomaly/schema/fake-repo tests migrated to the enum.
- Debt-#2 grep self-check against `pr-gate-checklist.md` → 0 missing sections.
- Debt-#4: `grep -c "e839214" CLAUDE.md` == 1 (only the L324 PR-range history line).

## Tests

- **R1/R4 facts:** transition guard proven by `TransitionStatus_illegal_transition_throws_and_does_not_mutate` (asserts throw + no state change + no audit). Persistence contract proven by `AnomalyStatusPersistenceTests` (int on disk, `GetByStatus(AnomalyStatus)` queries by int).
- **Public-contract ↔ fact (R4):** `IAnomalyService.TransitionStatus` enum signature + state-machine claim is backed by the illegal-transition throw fact.
- No Stryker delta run (P4 deferred).

## Cross-PR Consistency (CLAUDE.md §12.4)

- **R1 (parity):** no new multi-projection record field; `AnomalyStatus` kebab↔enum map is single-sourced in `AnomalyStatuses`.
- **R3 (spec retrospective):** N/A — no Phase-K spec recommendation was superseded.
- This branch was reconciled from a 2-way fork (origin debt-#5 `6c1484c` + cloud debt-#2/#3/#4); debt-#5 kept as authoritative, docs/CI debts cherry-picked on top, fast-forward (no force-push, no work lost).

## Windows Classification

- **Cloud-verified (Linux/mac, CI-gated):** #2, #3, #4, and #5 BLL.Core/Domain/DAL/Tests.
- **Windows-VM track (cannot compile on cloud):** #5 WPF (`MetBench_Client`) — 3 call sites per [`2026-05-29-debt5-wpf-vm-plan.md`](../plans/2026-05-29-debt5-wpf-vm-plan.md). VM verification screenshots at `docs/superpowers/specs/2026-05-29-debt5-vm-verification/`. **PR must not merge before these land.**

## Review

- `/code-review high` recommended on `MetBench_BLL.Core/SystemMT/Anomaly/` + `Catalog/Editing/` if touched (per §12.2 module F).
- Not a ≥3-PR chain → no chain-end ritual required (single batch PR).

## Merge

- Required check: `test` green (1556/0/12).
- `governance` job advisory warnings reviewed (Check-5 now aligned → no false section warnings).
- Base up to date with `origin/main` (`84ae500`); fast-forwardable.

## AI Review (advisory, automated)

- Dual AI review retired per §12.3; mechanical guards (grep + Roslyn + parity tests) + author-side `/code-review` cover the scope.
