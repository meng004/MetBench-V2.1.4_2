# Stage 5 — Phase 1: Mutation-Based Empirical Validation of MetBench MRs (v2)

> **For agentic + human collaborators**: implement task-by-task; each task is
> TDD-shaped (failing test or measurement-without-data first, then
> implementation/mutation, then result capture). Two-phase review (spec
> compliance + scientific honesty) at the end. The deliverable is an
> empirical paper-grade artifact, not just code.

**Goal**: produce the first empirical evidence that MetBench's existing MR
suite **detects realistic faults** in the systems under test (SUTs). Quantify
detection rate (with 95% CI), per-MR sensitivity, cross-solver agreement
(Cohen's κ), and false-positive rate (identity mutant + equivalent mutants
must not be reported as "detected").

**Date**: 2026-05-12
**Status**: Active
**Supersedes**: v1 of this plan (closed unmerged as PR #24 on 2026-05-11
because the catalogue mixed equivalent mutants in with semantic ones,
producing a misleading "missed" count).
**Predecessors**: PRs #10 – #23 (Stage 1-4 complete). Cloud sandbox has
both OpenMOC venv (`/opt/openmoc-venv`) and OpenMC venv
(`/opt/miniconda3/envs/openmc-env`).
**Successors** (not in this plan): reproducibility Docker container; geometry-
and symmetry-based MRs; CI extension to run OpenMOC scenarios end-to-end.

---

## Why this is the highest-ROI next step

MetBench's Stage 1-4 work proves the *framework* exists, is well-typed, runs
end-to-end across two different physics solvers, and reaches its acceptance
criteria. **None of that demonstrates the framework finds bugs.** A thesis or
paper that claims "metamorphic testing for system-level scientific computing"
needs a Results section that says: *"on N **semantically-changed** faults,
our MR suite detected M (95% CI [L, U]), missed K, and produced 0 false
alarms on identity/equivalent mutants. Cross-solver agreement κ = ..."*.
Without those numbers the contribution is structural, not empirical.

Every other candidate next step (Docker, more MRs, CI extension, WPF polish)
either depends on this evidence existing or is engineering-only. This phase
produces the citable result; the rest follows.

---

## What changed since v1

v1 (PR #24) hand-picked 10 mutations and ran them through 4 cross-program
MR scenarios. The user reviewer pointed out the methodology gap:

> *"You can either filter real defects from the OpenMC/OpenMOC libraries
> for semantic changes, then evaluate MRs; or mutate, keep only the mutants
> with semantic changes, then evaluate MRs."*

Concretely, the v1 catalogue contained mutations that are **equivalent
mutants** by construction:

- `M3` ("scale moderator nu_sigma_f instead of fuel"): moderator
  `nu_sigma_f = [0, 0]` in every case file, so multiplying by any factor
  is a no-op. The "MR missed it" classification was meaningless — the
  output didn't change because no fault was injected.
- `M5` ("forget to update sigma_t when sigma_a changes"): the runner reads
  `sigma_t` and `sigma_a` independently, so this is an inconsistent-input
  case, not a code fault.

Equivalent mutants inflate the "missed" count without measuring any MR
weakness. **The fix is a baseline-screening step that filters them out
before the MRs are scored.** v2 implements that step explicitly, reports
the screening discard rate as a first-class result, and tightens the
detection-rate gate so it can't paper over the gap.

The structural pieces of v1 (catalogue → harness → matrix → discussion)
carry over. The methodological pieces (catalogue size, gates, what counts
as "missed", real-bug supplement) are all revised below.

---

## Methodology (v2)

### Two parallel evidence sources

| Source | Quantity | Purpose |
|--------|----------|---------|
| **A. Filtered mutation** | ~25–30 candidates, expect ~15–20 to survive screening | Main quantitative result. Standard MT-literature methodology. |
| **B. Real historical bugs** | 2–3 case studies | Qualitative validation that mutation results generalise. Anchors the paper claim. |

A is the bulk of the work and the source of the headline number. B is a
case-study supplement: pick 2–3 fix commits from `openmc-dev/openmc` and
`mit-crpg/OpenMOC` that touch the cross-section reading / multi-group
solver path, reproduce the buggy state in a sandbox copy, run the MR
scenarios, report detected/missed/error per case in prose form (no big
matrix).

### Mutation pipeline (Source A)

```
candidates  ──▶  baseline screening  ──▶  semantic mutants  ──▶  MR matrix
   ~25-30                              ~15-20 (estimate)            scored
                  discard "equivalent" (Δk_eff ≤ threshold)
                  reported as a result, not hidden
```

**Step 1 — Generate ~25–30 candidate mutations.** Distributed roughly:

- ~10 in `SUT/openmoc/openmoc_runner.py` / adapters (chi-zero, sign-flip,
  swap fuel↔moderator, index off-by-one, missing per-group normalisation,
  drop scattering matrix transpose, etc.).
- ~10 in `SUT/openmc/openmc_runner.py` / adapters (scatter-matrix shape
  swap, energy-group order reversal, drop set_chi, particles=1 minimum,
  hardcode k_eff, etc.).
- ~5 in shared transformation logic (`MrTransformation` parameter parsing
  in adapters): apply factor twice, ignore factor, sign-flip factor,
  swap source and follow-up adapter outputs.

Mutations target **OpenMC and OpenMOC** SUTs per user direction; heat-equation
is intentionally not in scope for this study.

**Step 2 — Baseline screening.** For each candidate, run the **source**
case (no MR transformation) once with the mutation staged. Compare against
the unmutated source baseline (already captured in PR #23's comparison
report) using:

```
keep_as_semantic_mutant := |k_mut - k_baseline| > max(3 * σ_MC, 0.005 * k_baseline)
```

where `σ_MC` is the OpenMC statepoint's reported standard deviation (for
OpenMOC scenarios use 0.005 × k_baseline as the sole threshold since OpenMOC
is deterministic). Mutants below the threshold are classified as
**equivalent or near-equivalent** and removed from the MR matrix.

Rationale for the threshold:
- 3 σ_MC: standard MC discrimination threshold; below it the change is
  indistinguishable from statistical noise at default `batches=60,
  particles=5000`.
- 0.5% k_eff: covers the deterministic case and provides a physics-level
  floor (PWR k_eff is reported to 3 sig figs in textbooks; sub-0.5%
  effects are not the kind of bug MetBench is positioned to catch).

The threshold is a methodological choice, not a calibrated number. The
report must state it openly and discuss what changes if it's moved.

**Step 3 — Run the MR matrix on the surviving mutants.** For each
non-equivalent mutant `M_i`, run all 4 cross-program scenarios:

- `openmoc-pincell-nu-sigma-f`
- `openmc-pincell-nu-sigma-f`
- `openmoc-pincell-sigma-a`
- `openmc-pincell-sigma-a`

Record `(mutation_id, scenario_id) → {detected, missed, error}`:

- **detected** = MR assertion failed (`Passed=false`, no infra error).
- **missed** = MR assertion passed despite the semantic fault.
- **error** = scenario could not run (Python exception, parse failure,
  timeout).

**Step 4 — Score and report.**

- Per-MR detection rate **with Wilson 95% CI** (since N is small, normal
  approximation is wrong).
- Cross-solver agreement: Cohen's κ on the per-mutant detected/missed
  vector restricted to mutants visible to both solvers (i.e., mutations
  in shared logic + matched-pair mutations in solver-specific code).
- Per-mutation row: how many MRs caught it, qualitative description.
- Discard rate from baseline screening (the equivalent-mutant rate is a
  result, not a number to hide).
- False-positive rate on identity mutant `M0` (must be 0; this is a
  sanity gate, not a finding).

### Real-bug supplement (Source B)

Pick 2–3 fix commits from each upstream:

- `openmc-dev/openmc`: look for "Fixes #" in commit history touching
  `openmc/data/`, `openmc/material.py`, `src/finalize.cpp` multi-group
  paths. Reproduce the pre-fix state on a small branch.
- `mit-crpg/OpenMOC`: same drill for `src/Solver.cpp`,
  `openmoc/materialize.py`.

For each:

1. One-paragraph bug description (what was wrong, what symptom users saw).
2. Apply the pre-fix state to a sandbox copy of the SUT.
3. Run the 4 MR scenarios.
4. Report whether MetBench's MR caught it. If yes, the MR works on a
   real bug. If no, document why and add to the "future MRs" hand-off.

This is qualitative — 2–3 cases is not a statistical sample. The paper
phrasing should be: *"On 3 historical bug-fix commits we sampled, our MR
suite detected 2 / 3."* Not "our framework detects N% of real bugs".

---

## Acceptance criteria for the phase

The v1 gate ("≥60% detection rate") was a number with no statistical
backing. The v2 gates focus on **what makes the result publishable**.

Required:

- **Catalogue defined**: ≥ 25 candidate mutations, each with target file,
  patch, predicted-detector column, rationale (≤ 3 lines).
- **Screening report**: every candidate classified equivalent or semantic
  with the threshold rule applied. Discard rate explicitly stated.
- **Matrix populated**: every (surviving mutant) × 4 scenarios cell filled
  (detected / missed / error).
- **Statistics reported**:
  - Per-MR detection rate **with Wilson 95% CI**.
  - Cross-solver Cohen's κ + verbal interpretation.
  - Identity false-positive rate = 0.
- **Real-bug supplement**: 2–3 documented historical bugs with detected /
  missed / error labels and per-case prose.
- **Honest discussion** (≤ 600 words) covering:
  - What worked (uniformly-detected mutations).
  - What surprised us (split MR detection).
  - Coverage gaps motivating Stage 5 Phase 2 MR additions.
  - Threshold sensitivity (how the matrix changes if the screening
    threshold moves to 1% or to 2σ).

Not required (deliberately removed from v1):

- ~~"≥60% detection rate"~~ — replaced by reporting the rate **with CI**
  and letting reviewers judge. A high rate is good news; a low rate
  motivates new MRs. Either is publishable; neither blocks merge.
- ~~"At least one MR catches every realistic fault"~~ — coverage gaps are
  the point of the study; turning them into a hard gate creates an
  incentive to cherry-pick mutations.

---

## Tasks

### Task 1 — Define the candidate catalogue (no code yet)

Write `docs/experiments/mutation-catalogue.md` with ≥ 25 candidates. Each
row: `id | target_file | exact_patch | predicted_detector(s) | rationale`.
Mark predicted-equivalent rows openly (do not silently drop them; the
screening run is what removes them).

Acceptance: the catalogue is reproducible from the spec alone — another
contributor could implement the patches without reading any code review.

### Task 2 — TDD: implement the screening harness on one candidate

Implement `tools/mutation_study.py` with **only the identity mutant M0 and
one clearly-semantic mutant (e.g., chi-zero in `openmoc_runner.py`)
hard-coded**. The harness must:

1. Make a temp copy of `SUT/` under `/tmp/`.
2. Stage the mutation in the copy.
3. Run the **source** case (no MR transformation) for both OpenMOC and
   OpenMC.
4. Compute `|k_mut - k_baseline|` and the threshold; classify the candidate
   as `equivalent` or `semantic`.
5. If `semantic`, run all 4 MR scenarios and classify each as `detected`,
   `missed`, or `error`.

Acceptance:
- `python3 tools/mutation_study.py --candidate M0` reports `equivalent`
  (control passes).
- The chi-zero mutant reports `semantic` and `detected` on at least one
  MR (the chi-zero candidate is included precisely because it's an
  obvious win; if even this one is missed, the study has bigger problems).
- Exits non-zero on infrastructure error.

### Task 3 — Run the full candidate pool through screening

Apply Task 2's harness to every candidate. Output:

- `docs/experiments/screening-results.csv`: `candidate_id, k_baseline,
  k_mut, |delta|, threshold, classification`.
- `docs/experiments/screening-results.md`: per-row prose for the discarded
  ones (why is each one equivalent?). For surviving rows, just the table.

Acceptance: every candidate is classified. Discard rate is reported as a
first-class number in the report ("of 28 candidates, 11 were equivalent
under our threshold").

### Task 4 — Run the MR matrix on the surviving mutants

For every semantic mutant × 4 scenarios, populate the matrix.

Output:

- `docs/experiments/mutation-detection-matrix.csv`: raw cells.
- `docs/experiments/mutation-detection-matrix.md`: formatted matrix +
  per-MR detection rate with Wilson 95% CI + per-mutation summary +
  identity FP rate.

Compute Wilson CI inline in the orchestrator (small helper, ~10 LoC; no
new dependency).

Acceptance: every cell populated; CIs reported; FP rate = 0.

### Task 5 — Cross-solver Cohen's κ + threshold sensitivity

Add two analyses to `mutation-detection-matrix.md`:

1. **Cohen's κ**: for the subset of mutants whose target file is shared
   logic or whose patch has a structural mirror in the other solver
   (e.g., chi-zero on OpenMOC and chi-zero on OpenMC are a matched pair),
   compute κ on the detected/missed vector. Report κ + interpretation
   (Landis-Koch).
2. **Threshold sensitivity**: re-run the classification with the screening
   threshold at 2σ and at 1% k_eff; report how many candidates flip
   equivalent ↔ semantic, and how the matrix changes. This is a one-table
   robustness section.

Acceptance: κ reported; sensitivity table present.

### Task 6 — Real-bug supplement

For each of 2–3 historical bug commits from upstream OpenMC / OpenMOC:

1. Identify the commit (`git log` on upstream, prefer fixes touching
   cross-section / multi-group code paths).
2. Document the bug in prose.
3. Reproduce the pre-fix state in `tools/mutations/historical/<bug-id>.patch`.
4. Run the 4 MR scenarios on it; report detected / missed / error per
   scenario.

Output: `docs/experiments/historical-bugs.md`, one section per bug, prose
+ small per-bug table. **This is not part of the matrix.** It supplements
the matrix's statistical claim with a "and here's what it does on the
real thing" anecdote.

Acceptance: 2–3 bugs documented, each with patch + MR result + prose.

### Task 7 — Discussion + Stage 5 hand-off

Write the discussion section of `mutation-detection-matrix.md` (≤ 600
words) covering:

1. Headline number with CI (one sentence).
2. What worked uniformly.
3. What split per-MR or per-solver.
4. Coverage gaps → 2–3 named candidate MRs for Phase 2.
5. Threshold sensitivity — does the headline survive the threshold moves?
6. Real-bug agreement (Task 6 result vs matrix expectation).

Acceptance: discussion makes only claims supported by the matrix or the
real-bug supplement. No phrase like "high detection rate" without the
specific number it refers to.

---

## File structure

Create:

- `tools/mutation_study.py` — screening + matrix orchestrator.
- `tools/mutations/` — one Python file per mutation (or one `mutations.py`
  module with a list of `Mutation` dataclasses; ~25 entries).
- `tools/mutations/historical/` — `.patch` files for Task 6.
- `docs/experiments/mutation-catalogue.md`
- `docs/experiments/screening-results.csv`
- `docs/experiments/screening-results.md`
- `docs/experiments/mutation-detection-matrix.csv`
- `docs/experiments/mutation-detection-matrix.md`
- `docs/experiments/historical-bugs.md`

Modify:

- `docs/superpowers/plans/2026-05-10-stage4-remaining-acs.md` — append a
  one-line "Followups landed in Stage 5 Phase 1" pointer.
- `README.md` — small "experiments" section pointing at
  `docs/experiments/`.

---

## Tech stack

- Python 3.11+ for the orchestrator (stdlib + `subprocess` + `math` for
  Wilson CI / Cohen's κ; no new pip packages).
- Existing `.NET 8` test infrastructure unchanged.
- The study runs outside `dotnet test` so per-run mutation staging never
  touches tracked source files.

---

## Scope guard

This plan adds:

- One orchestrator script.
- ~25 mutation patches + 2–3 historical-bug patches.
- Five markdown experiment reports + two CSVs.

This plan does NOT:

- Add new MRs (motivated as Phase 2 hand-off).
- Change the launcher / facade / `MrTransformation` IR.
- Modify any production SUT script committed to `main` (mutations are
  applied inside a temp copy).
- Touch WPF / `MetBench_BLL` / `MetBench_Client`.
- Build a Docker container (separate Stage 5 phase).
- Run on heat-equation (only the two cross-program MRs are studied;
  mutations target OpenMC + OpenMOC per user direction).
- Claim a generalisation from 2–3 real bugs.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Screening threshold mis-classifies a borderline mutant | Task 5 sensitivity sweep at 2σ and 1% surfaces flips explicitly. |
| MC statistical noise causes a `semantic` mutant to be re-classified `equivalent` between runs | Run the screening case 3× per candidate; use the mean and largest σ. |
| A mutation breaks the script so badly the runner can't start | Classify as `error` (not `detected`); discussion treats errors separately. |
| Upstream historical bugs are too hard to reproduce (depend on old API / build) | Reduce supplement to 1 case study, or replace one bug with a documentation-only "would-be-detected" walk-through. Note the substitution in the report. |
| OpenMC venv missing on a future agent's machine | Orchestrator skips OpenMC cells with explicit `skipped` marker (distinct from `missed`). |
| Mutations leak past temp dir | `tempfile.TemporaryDirectory`; orchestrator never writes inside repo `SUT/`. |

---

## Out of scope (for this phase)

- Reproducibility Docker container (next phase).
- New MRs. The discussion names 2–3 candidates motivated by gaps the
  study finds.
- CI integration of OpenMC.
- Sensitivity analysis on the MR factor itself (one factor per scenario).
- Performance / wallclock profiling.
- Method-level (Stage 0) MR comparison.
