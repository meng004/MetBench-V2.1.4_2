# Chain-End Holistic Review Checklist

> **When to use**: at the end of any ≥ 3-PR phased delivery (T2/T3 chain, W12 sequence, S8 P1-P5 chain, etc.) per `CLAUDE.md §12.4 R2`. Spawn a **fresh agent session** (not the chain-implementation session). Run the items below over the full set of merged PRs treated as a single diff.

### Step 0 — Generate ultra invocation (P5 automation)

- [ ] Ran `python3 tools/chain_end_ultra_invocation.py --base <chain-base-ref> --head <chain-head-sha> --chain-name <chain-slug>` and captured the output into the working notes of this review session.
- [ ] The generated `/code-review ultra ...` line was invoked (or its stdin-piped variant if the skill's arg form requires that), with output saved alongside this checklist's findings.
- [ ] The suggested review-doc path (`docs/superpowers/specs/YYYY-MM-DD-<chain-name>-post-merge-review.md`) was reconciled against `§2.2` of the P5 plan and §2 chain-naming examples; if the slug differs, document why in the review doc header.

## Scope

- [ ] All chain PRs identified (cite SHA + PR number for each phase).
- [ ] `origin/main` head is the chain-end PR's merge SHA (no later PRs interleaved that would confuse review).
- [ ] Review session is **distinct** from the chain implementation session (fresh context).

## Cross-PR design coherence

- [ ] Public types added in Phase K are consumed correctly in Phase K+1 through K+N (no dead additions).
- [ ] Public method signatures match across phases (no Phase-K added an init-property that Phase-K+1's projection silently drops — the L1 / FromBlueprint / MetaPattern pattern).
- [ ] All projection paths agree (cross-projection parity tests exist per CLAUDE.md §12.4 R1; if they don't, file a parity-test follow-up).
- [ ] Cross-file pairings agree (renderers vs DTOs, schemas vs migrations, runners vs adapters).

## Public contract honesty

- [ ] Every public method XML doc claim has a fact pinning it (CLAUDE.md §12.4 R4). Grep `<summary>` for verbs like "honors", "implements", "supports", "renders", "persists"; for each, confirm a fact asserts the claim is observable.
- [ ] `_ = ctx ?? new Default()` and similar "evaluated but discarded" patterns flagged (B1 / Excel ReportContext pattern).
- [ ] `ApproxEqual` / `Equal` tolerance bands documented with the analytical / FD reason; no tolerance constant without justification (T3 / subchannel strict equality intent).
- [ ] Magic numbers (units / EMU / row strides / DPI factors) traced to either a documented constant or a derivation from `ChartRenderOptions` / similar settable input.

## Spec doc retrospective

- [ ] For any plan-phase whose actual implementation diverged from the original spec recommendation, the spec doc has been re-touched to mark the original "REJECTED / REPLACED" with reason (CLAUDE.md §12.4 R3).
- [ ] All `Tier-A / Tier-B / top-1 candidate / "X is the next gap-fill"` claims in spec docs are re-checked against current main; stale wording corrected.
- [ ] SUT-precondition feasibility checks added to spec docs that recommend candidates (lesson from PR-T3-8 burgers-timestep-convergence failure).

## Test surface

- [ ] Negative-space audit: for each public method added during the chain, the test surface covers its declared contract (not just "tests exist" — tests assert the right thing).
- [ ] Cross-PR parity guards added where the chain introduced ≥ 2 projection paths.
- [ ] No test was relaxed mid-chain to make CI green without follow-up justification.

## Status ledger & projection docs

- [ ] `docs/status/current.md` Stage-N row for this chain reflects the **actual** delivery (cite all phase SHAs).
- [ ] `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` row for the chain plan marks it Completed only after this checklist passes.
- [ ] Cross-link from chain plan to any spec docs the chain produced.
- [ ] `docs/PROJECT-STRUCTURE.md` test matrix reflects new SUT / test classes added by the chain (if any).

## Output

- [ ] Findings categorized A (single-PR visible, AI-review-catchable) vs B (cross-PR / retrospective, structurally hard).
- [ ] Each finding has a priority (P0 / P1 / P2 / P3) and a proposed fix path.
- [ ] **Cleanup PR(s) opened and merged BEFORE the chain is declared "Controlled" in the status ledger** (CLAUDE.md §12.4 R2).
- [ ] For every B-category finding, the cleanup PR adds a guard test (parity / contract / paired-fact) per CLAUDE.md §12.4 R1+R4, NOT only a per-instance fix.
- [ ] Chain-end review session creates a `docs/superpowers/specs/<date>-<chain-name>-post-merge-review.md` summarizing categories, findings, follow-up PRs, and rules-of-thumb learned.
- [ ] Step 0 ultra-invocation output is cross-linked from the review doc as an "Auxiliary artifacts" section (immediately after §1 chain phases table), with the exact `/code-review ultra ...` command, full base + head SHAs, and finding-vs-ultra-finding reconciliation notes (which ultra findings the human ritual confirmed / dismissed / extended).

---

## Reference

- `CLAUDE.md §12.4` — Third-Layer Discipline (cross-PR consistency rules)
- `CLAUDE.md §12.5` — Fourth-Layer Discipline (guard-test compilation)
- `docs/superpowers/specs/2026-05-26-pr-soft-review-via-claude-code-action.md` — what AI review at PR-time can / cannot catch (**spec retired ~2026-05-27, dual AI review removed per CLAUDE.md §12; historical context only**)
- Example chain-end review session: [PR #195](https://github.com/meng004/MetBench-V2.1.4_2/pull/195) + [PR #199](https://github.com/meng004/MetBench-V2.1.4_2/pull/199) — the cleanup PRs that closed the T2/T3 6-phase chain's 11-finding post-merge review
