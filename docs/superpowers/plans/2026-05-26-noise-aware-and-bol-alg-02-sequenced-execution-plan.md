# Noise-Aware Predicate + Bol-Alg-02 Sequenced Execution Plan

> **For agentic workers:** Mandatory execution order for the next two cloud-side PRs after the T1 cross-method differential / non-JSON I/O gate orchestration completed (PR-A merged at `66eb297` → docs alignment merged at `1f20e59`). Mirrors the shape of prior PR-0 / PR-1 / PR-2 / PR-B / PR-A sequenced gates.

## Context

`docs/status/current.md` after PR-A / PR-B / docs alignment now shows two cloud-executable, unstarted, high-ROI / low-friction items:

1. **PR-N1** — typed noise-aware scalar predicate (`NoiseAwareBinaryComparisonPredicate` + kernel + validator + `MapNoiseAwareScalar` overload). Closes the two remaining intentionally-fail-closed legacy assertion codes (`less-noise-aware`, `greater-noise-aware`) and unblocks any future MC / probabilistic SUT m_mono MR that needs a noise-aware directional check. See `2026-05-26-typed-noise-aware-scalar-predicate-plan.md`.

2. **PR-N2** — Bol-Alg-02 MC particle count convergence MR on OpenMC. Adds the MR `openmc-pincell-particle-count-convergence` using `assertion_type_code: variance-ratio` (already typed-mappable since PR #124 — independent of PR-N1 despite the prior active-plan-index "blocked on noise-aware typed predicate" wording, which this gate retracts as overstated). See `2026-05-26-bol-alg-02-mc-particle-count-convergence-plan.md`.

Both plans declare strict cloud-side scope (no Method MT, no WPF, no Python adapter or runner edits).

## Mandatory order

1. **PR-0 (this PR)** — docs-only gate. Registers the two scoped plans + this orchestration plan in the active plan index and adds the two `Open` rows in the status ledger. **Must merge first.**
2. **PR-N1** `feat(verif): add noise-aware typed scalar predicate (NoiseAwareBinaryComparisonPredicate)` — adds the typed spec / kernel / validator / mapper overload. ~25 facts. No new MR catalog row consumes it.
3. **PR-N2** `feat(bol): add openmc-pincell-particle-count-convergence MR (Bol-Alg-02)` — adds the new MR using `variance-ratio` assertion code (typed-mappable already; PR-N1 is **not** strictly required for PR-N2 to merge). PR-N1 still runs first because it ships the lower-risk, smaller-diff change; PR-N2's pinned-count edits touch the same six descriptor-list files PR-N1 leaves alone, so sequencing also avoids a pinned-count merge conflict on those files.

Each subsequent PR must:

- Cite this plan in its description.
- Re-verify preconditions at the start (origin/main reachable, prior PRs merged, no new plan supersedes ours).
- Land its own status-ledger and active-plan-index edits so the ledger never goes stale between PRs.

## Independence clause

PR-N2 does **not** depend on PR-N1 type-wise (Bol-Alg-02 uses `VarianceRatioPredicate`, already typed since PR #124). The sequencing is operational, not semantic: PR-N1 is the cheaper, smaller-blast-radius change and lands first to mechanically reduce merge friction. If PR-N1 is delayed or reverted, PR-N2 can still merge.

## Out of scope for the whole gate

- No Method MT changes anywhere in the gate.
- No WPF / `MetBench_Client/` / `App.xaml.cs` edits anywhere.
- No new SUT directory (PR-N2 reuses `SUT/openmc/`; PR-N1 ships no SUT-side code at all).
- No new EquationMetadata (Boltzmann + 12 existing reactor / PDE equations + 1 synthetic `_test_csv` = 13 equations stays).
- No T4 binder edits.
- No UI MR CRUD — that remains the prior Windows/VM plan.

## Expiry

This plan expires once **both** PR-N1 and PR-N2 have merged AND `docs/status/current.md` records:

- "Noise-aware typed scalar predicate" → Controlled.
- "Legacy assertion code mapping" row updated to remove `less-noise-aware` / `greater-noise-aware` from the intentionally-fail-closed list (and clarify they're mappable via the new `MapNoiseAwareScalar` overload).
- "PR-Bol-3 / Bol-Alg-02 MC particle count convergence" → Controlled.

At that point this file moves to §3 of the active plan index (already-merged section). The only outstanding scoped plan after this orchestration completes is the Windows/VM UI MR CRUD plan from the much earlier gate.
