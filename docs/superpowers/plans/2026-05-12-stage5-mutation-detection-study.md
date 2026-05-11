# Stage 5 — Phase 1: Mutation-Based Empirical Validation of MetBench MRs

> **For agentic + human collaborators**: implement task-by-task; each task is
> TDD-shaped (failing test or measurement-without-data first, then
> implementation/mutation, then result capture). Two-phase review (spec
> compliance + scientific honesty) at the end. The deliverable is an
> empirical paper-grade artifact, not just code.

**Goal**: produce the first empirical evidence that MetBench's existing MR
suite **detects realistic faults** in the systems under test (SUTs). Quantify
detection rate, per-MR sensitivity, per-SUT consistency, and false-positive
rate (mutation = identity → all MRs must still pass).

**Date**: 2026-05-12
**Status**: Active
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
needs a Results section that says: *"on N realistic faults, our MR suite
detected M, missed K, and produced 0 false alarms on identity."* Without
those numbers the contribution is structural, not empirical.

Every other candidate next step (Docker container, additional MRs, CI
extension, WPF polish) either depends on this evidence existing (Docker
ships it; new MRs are motivated by what current ones missed) or is
engineering-only (CI, WPF). This phase produces the citable result; the rest
follows.

---

## Background

### What we have today

Three SUTs × three MRs are wired through the framework, but only two MRs are
actually shared across solvers:

| MR family | OpenMOC | OpenMC | heat-equation |
|-----------|---------|--------|---------------|
| `NeutronTransport.Scaling.NuSigmaF` (k_eff ↑) | ✅ | ✅ | — |
| `NeutronTransport.Scaling.SigmaA` (k_eff ↓) | ✅ | ✅ | — |
| `Diffusion.Scaling.Amplitude` (max_u ↑ proportionally) | — | — | ✅ |

PR #23's cross-program comparison shows OpenMOC and OpenMC agree on the MR
ratio to 3 significant figures. That's solver-vs-solver consistency under
**no fault**. We have not measured what happens when a fault is introduced.

### What a "fault" is here

A fault is a code-level change to a SUT runner or input/output adapter that
**violates one of the MRs**. Three categories worth distinguishing:

1. **Realistic-physics faults**: an indexing slip, a missing per-group
   normalisation, a wrong sign — the kind of mistake a developer might
   actually commit. These should be detected by at least one MR.
2. **MR-orthogonal faults**: a printf change, a logging tweak — these should
   **not** trigger any MR (false-positive control).
3. **Stress faults**: faults that violate physics in subtle ways one MR can
   see but another cannot. These reveal MR coverage gaps.

The study deliberately mixes all three.

### Why use mutation, not real historical bugs

Real historical bugs in OpenMOC or OpenMC require git archaeology on those
projects and reproducing fault-revealing inputs. That is its own
multi-month research task. Mutation testing is the standard accepted proxy
in MT literature (Jia & Harman 2011 survey) and gives an answerable yes/no
per MR within a few hours.

---

## Methodology

### Mutation catalogue

Define ~10 mutations across the OpenMOC + OpenMC adapter files. Each
mutation is implemented as a callable `apply(file_text) -> file_text` so
the harness can stage / unstage cleanly.

Suggested mutations (mix of categories above):

| ID | Target file | Mutation | Expected detector |
|----|------------|----------|-------------------|
| `M1` | `openmoc_runner.py` | drop chi (set chi to zeros) | NuSigmaF, SigmaA |
| `M2` | `openmoc_runner.py` | swap fuel and moderator material mapping | both MRs |
| `M3` | `openmoc_input_adapter.py` | scale **moderator** nu_sigma_f instead of fuel | NuSigmaF (should now pass trivially since moderator nu_sigma_f = 0; expect MR to MISS) |
| `M4` | `openmoc_input_adapter.py` | scale by `1/factor` instead of `factor` | NuSigmaF (k decreases instead of increases) |
| `M5` | `openmoc_input_adapter_sigma_a.py` | forget to update sigma_t (only sigma_a changes) | SigmaA (physically inconsistent input; behaviour depends on solver) |
| `M6` | `openmc_runner.py` | reduce batches from 60 to 5 (high statistical noise) | possibly NuSigmaF if MC ratio drifts > 1.95 / 2.05 band |
| `M7` | `openmc_runner.py` | hardcode k_eff = 1.0 regardless of input | both MRs |
| `M8` | `openmc_input_adapter.py` | apply factor twice (square the scaling) | both MRs (huge over-prediction) |
| `M9` | `openmoc_output_adapter.py` | swap k_eff and iterations fields | both MRs (k_eff replaced by iteration count, fails magnitude) |
| `M10` | `openmoc_output_adapter.py` | print a banner before JSON (corrupts stdout parse) | both MRs via adapter-failure path |
| `M0` | (identity — no change) | none | none (control) |

The catalogue is intentionally biased toward realistic faults but includes
`M3` and `M5` to probe MR coverage limits.

### Run protocol

For each mutation `M_i`:

1. Stage the mutation by replacing the file in a sandbox copy of the repo.
2. Run all 4 applicable cross-program scenarios:
   `openmoc-pincell-nu-sigma-f`, `openmc-pincell-nu-sigma-f`,
   `openmoc-pincell-sigma-a`, `openmc-pincell-sigma-a`.
3. Capture per-scenario `ScenarioRunResult` (passed, k_source, k_followup,
   ratio, error reason if any).
4. Unstage; restore file.

Record `(mutation_id, scenario_id) → {detected, missed, error}` where:

- **detected** = MR assertion failed (`Passed=false`, no infra error).
- **missed** = MR assertion passed despite the fault (`Passed=true`).
- **error** = scenario could not run (Python exception, parse failure, timeout).

### Output

- `tools/mutation_study.py` — the orchestrator (Python or C# console app;
  Python is lighter since we already shell out for adapters).
- `docs/experiments/mutation-detection-matrix.csv` — raw cell-level data.
- `docs/experiments/mutation-detection-matrix.md` — formatted detection
  matrix + per-MR detection rate + discussion.
- `docs/experiments/mutation-catalogue.md` — what each mutation does and
  why we picked it.

---

## Tech stack

- Python 3.11+ for the orchestrator (uses only stdlib + subprocess to call
  the existing adapter scripts and runners).
- Existing `.NET 8` test infrastructure not modified (the study is
  intentionally executed outside `dotnet test` so we can stage mutations
  per-run without touching tracked source files).
- No new NuGet / pip packages.

---

## Scope guard

This plan adds:

- One orchestrator script (`tools/mutation_study.py`).
- One mutation catalogue with ≥ 10 mutations.
- One markdown experiment report + raw CSV.
- One `docs/experiments/mutation-catalogue.md` describing each mutation's
  rationale and predicted detector.

This plan does NOT:

- Add new MRs (separate Stage 5 phase).
- Change the launcher / facade / `MrTransformation` IR.
- Modify any production SUT script committed to main (mutations are applied
  inside a temp copy).
- Touch WPF / `MetBench_BLL` / `MetBench_Client`.
- Build a Docker container (separate Stage 5 phase).
- Run on heat-equation (only the two cross-program MRs are studied here).

---

## File structure

Create:

- `tools/mutation_study.py` — orchestrator.
- `tools/mutations/` — one Python file per mutation OR a single
  `mutations.py` module with a list of `Mutation` dataclasses. Pick whichever
  keeps the catalogue under ~300 lines.
- `docs/experiments/mutation-catalogue.md`
- `docs/experiments/mutation-detection-matrix.csv`
- `docs/experiments/mutation-detection-matrix.md`

Modify:

- `docs/superpowers/plans/2026-05-10-stage4-remaining-acs.md` — append a
  "Followups landed in Stage 5" pointer to this file (1-line link).
- `README.md` — small "experiments" section pointing at `docs/experiments/`.

---

## Tasks

### Task 1 — Define the mutation catalogue (no code yet, just a Markdown spec)

Write `docs/experiments/mutation-catalogue.md` with the full table of ≥ 10
mutations, each entry containing:

- ID, target file, exact patch description, rationale, predicted detector(s).

Acceptance: mutations are well-defined enough that another developer could
implement them from the spec alone. The catalogue is the contract; the
orchestrator implements against it.

### Task 2 — TDD: write the mutation harness against one mutation first

Implement `tools/mutation_study.py` with **only mutation M0 (identity)
plus M4 (scale by 1/factor instead of factor) hard-coded**. The harness
must:

1. Make a temp copy of `SUT/` somewhere under `/tmp/`.
2. Stage the mutation in the copy (M0 = no change).
3. Run `openmoc-pincell-nu-sigma-f` scenario end-to-end via subprocess
   (same subprocess pattern as `tools/cross_program_comparison.py`).
4. Parse the runner output JSON, apply the assertion in-process, classify
   as detected/missed/error.

Acceptance:
- `python3 tools/mutation_study.py --mutation M0 --scenario openmoc-pincell-nu-sigma-f`
  reports "missed" (control passes).
- `python3 tools/mutation_study.py --mutation M4 --scenario openmoc-pincell-nu-sigma-f`
  reports "detected" (MR catches the inverted scaling).
- Exits non-zero on infrastructure error (so a stray test failure does not
  pollute the matrix).

### Task 3 — Implement the remaining mutations

For each of M1, M2, M3, M5, M6, M7, M8, M9, M10:

1. Add the mutation patch under `tools/mutations/`.
2. Run it against the two NuSigmaF + two SigmaA scenarios.
3. Cross-check the result with the predicted detector from the catalogue.
4. If actual ≠ predicted, **update the catalogue's "predicted detector"
   column with a note**. The catalogue documents what actually happens,
   not what we hoped.

Acceptance: all 10 mutations × 4 scenarios = 40 cells are filled.

### Task 4 — Aggregate into the detection matrix

Once `tools/mutation_study.py --all` populates a CSV, write:

- `docs/experiments/mutation-detection-matrix.csv` — raw cells.
- `docs/experiments/mutation-detection-matrix.md` — formatted matrix +
  summary stats:
  - Per-MR detection rate (over the 10 non-identity mutations).
  - Per-mutation detection count (how many MRs caught each).
  - Mutations no MR caught — these are the "MR coverage gaps" worth a
    follow-up Stage 5 phase.
  - False-positive rate on M0 (must be 0).

Acceptance: the report is publishable in the thesis Results chapter
verbatim. Tables, no prose stretching, no over-claiming.

### Task 5 — Discussion + Stage 5 hand-off

Write the discussion section of `mutation-detection-matrix.md`:

1. What worked (which mutations were uniformly detected).
2. What surprised us (mutations one MR caught but another missed).
3. Coverage gaps (mutations no MR caught — these motivate the next Stage 5
   phase: adding MRs).
4. Cross-solver consistency (did the same mutation get detected on
   OpenMOC and OpenMC?). This is the core "MR is solver-independent"
   empirical claim.

Acceptance: discussion is ≤ 500 words, makes only claims supported by the
matrix, names concrete follow-up MRs to add.

---

## Acceptance criteria for the phase

- 10 mutations defined and implemented.
- All 40 cells in the detection matrix populated.
- False-positive rate on identity = 0%.
- Detection rate on realistic mutations (i.e. excluding the deliberately
  marginal M3 and M5) ≥ 60%.
- Cross-solver consistency: for any mutation that affects shared logic,
  detection happens on **both** OpenMOC and OpenMC scenarios (or on
  neither). Document the few that differ as known interpretation gaps.
- All artifacts committed to main via a single PR.
- The PR description embeds the detection matrix table inline (not just a
  link), so reviewers see results without leaving the PR.

---

## Risks

| Risk | Mitigation |
|------|------------|
| MC statistical noise mis-classifies a mutation (M6 in particular) | Run M6 ≥ 5 times; classify based on majority of runs; document the variance |
| A mutation breaks the script so badly the runner can't even start | Classify as "error", not detected; discussion explains the distinction |
| OpenMC venv not on a future agent's machine | Orchestrator skips OpenMC cells with explicit "skipped" marker (not "missed") |
| Mutations leak past temp dir | Use `tempfile.TemporaryDirectory`; orchestrator never writes inside repo `SUT/` |

---

## Out of scope (for this phase)

- Reproducibility Docker container (next phase).
- New MRs (rotation / mesh refinement / density scaling). The discussion
  section will name 2-3 candidates motivated by the gaps this study finds.
- CI integration of OpenMC. The study runs once on a dev machine; CI can
  be extended in a later phase if the results justify it.
- Sensitivity analysis (factor sweeps, batch-count sweeps). One MR factor
  per scenario for now.
- Performance / wallclock profiling.
- Method-level (Stage 0) MR comparison.
