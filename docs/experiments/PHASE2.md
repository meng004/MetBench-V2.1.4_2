# Stage 5 Phase 2 — NOETHER MR catalogue + matrix

> Top-level summary of Phase-2 work on the MetBench MR catalogue.
> Companion to [`discussion-phase2.md`](discussion-phase2.md) (analysis),
> [`mutation-detection-matrix.md`](mutation-detection-matrix.md) (auto-generated
> per-cell results), [`historical-bugs.md`](historical-bugs.md) (real-bug
> walkthrough), and [`cross-program-report.md`](cross-program-report.md)
> (MR14 disagreement table).

## Goal

Apply the NOETHER framework (Pham et al., 2026) to extend MetBench's
metamorphic-relation coverage beyond the four Phase-1 single-direction
monotonicity MRs (`ScaleNuSigmaF`, `ScaleFuelSigmaA` × OpenMOC, OpenMC).
NOETHER organises MRs by MetaPattern (MP) — operator-algebra
invariants of the program family — and we restrict to the four MPs
the 2D pin-cell + static eigenvalue SUT actually exercises:

* `m_inv` (B1 symmetry): rotation, reflection, energy-group permutation
* `m_mono` (B2 order): single-parameter monotonicity
* `m_conv` (B5 limit): discretisation convergence
* `m_cmp` (B7 method-comparison): cross-solver agreement

`m_adj`, `m_rev`, `m_dyn`, `m_rel` are out of scope (no adjoint solver,
dissipative scattering, static eigenvalue, no idempotent-semiring
rewriting).

## Deliverables (what ships)

### Catalogue + filter

* **`tools/noether_candidates.py`** — 15 candidate MRs (MR01-MR15)
  enumerated from the Cartesian product MetaPattern × {parameters,
  generators}. Each entry has hypothesis, suggested assertion,
  physical rationale.
* **`tools/noether_llm_filter.py`** — Anthropic-format filter with
  prompt caching, .env loading, resume cache. Verdicts in
  `_data/noether/llm-verdicts.json`. Result on the catalogue: 14
  valid + 1 uncertain (MR10 unverifiable) + 0 invalid.
* **`tools/noether_adversarial.py`** + **`tools/noether_filter_calibration.py`** —
  five hand-crafted bogus candidates (direction-inverted, vacuous
  identity, impossible budget, …) feed back through the same prompt.
  Filter rejects 3-4 / 5; the lone genuine blind spot is "vacuous-
  but-true" MRs.

### Adapters (16 new SUT input adapters)

| MR | Adapter (OpenMOC / OpenMC) | Source case | Notes |
|----|----------------------------|-------------|-------|
| MR01 Rotate90 | `..._rotate_90.py` | `pincell-asymmetric.json` | swap x/y extents |
| MR02 MirrorX | `..._mirror_x.py` | `pincell-offcentre.json` | flip y_offset |
| MR03 MirrorY | `..._mirror_y.py` | `pincell-offcentre.json` | flip x_offset |
| MR04 PermuteEnergyGroups | `..._group_permute.py` | `pincell.json` | swap groups 0↔1 across all per-group arrays |
| MR05 ScaleFuelSigmaT | `..._fuel_sigma_t.py` | `pincell.json` | factor=1.5 |
| MR06 ScaleFuelSigmaS | `..._fuel_sigma_s.py` | `pincell.json` | factor=0.5; recompute sigma_t to keep sigma_a fixed |
| MR07 ScaleModeratorSigmaA | `..._moderator_sigma_a.py` | `pincell.json` | factor=1.5 |
| MR08 ScaleFuelRadius | `..._fuel_radius.py` | `pincell.json` | factor=1.05 (small perturbation, must stay under-moderated) |
| MR12 RefineParticles (OpenMC only) | `openmc_input_adapter_refine_particles.py` | `pincell.json` | factor=10 → variance-ratio 1/√10 |

### Schema extensions (backward-compatible)

* Geometry now reads optional `fuel_offset_x_cm`, `fuel_offset_y_cm`
  (default 0 → centred fuel, all earlier samples unchanged).
* Two new sample cases:
  - `SUT/openmoc/sample/pincell-asymmetric.json` (1.0×1.5 cm) — for MR01.
  - `SUT/openmoc/sample/pincell-offcentre.json` (1.30×1.30 cm + offset
    +0.10/-0.08 cm) — for MR02/MR03. Carefully chosen to avoid an
    OpenMOC `CPUSolver` convergence basin pathology found at larger
    extents.

### Orchestrator extensions (`tools/mutation_study.py`)

* SCENARIOS field `factor_override` so MRs valid only in a small-
  perturbation regime (MR08 fuel-radius) can pin their own factor.
* SCENARIOS field `source_case` so MRs needing asymmetric / off-
  centre geometry can override the default `pincell.json`.
* `evaluate_mr` extended with three new assertion forms:
  - `approx` — `|Δk| ≤ tolerance_rel · |k_source|` (m_inv MRs).
  - `variance-ratio` — observed σ_followup/σ_source within
    tolerance_rel of `target_ratio` (MR12, the MC convergence law).
  - `noise_aware` flag on `greater`/`less` — require `k_followup
    <(>) k_source ∓ max(3·σ, tolerance_rel·|k|)`. Closes the
    Phase-1 hand-off about strict-inequality fragility under MC
    noise. All six OpenMC less/greater scenarios opt in.
* `openmc_available()` probe; OpenMC scenarios degrade gracefully
  to `status=skipped-no-openmc` cells when OpenMC isn't installed.
* `run_subprocess` uses `start_new_session=True` + `os.killpg` on
  timeout so OpenMC orphans never accumulate.
* `MATCHED_PAIRS` index extended to 16 cross-solver matched pairs;
  κ output adds eight per-MR sub-blocks.

### Mutations (Mut00 → Mut44, **41 new** since Phase 1)

Phase-1 mutations Mut00-Mut27 retained unchanged. New for Phase 2:

| Mut | Target | Designed-to-break MR |
|-----|--------|----------------------|
| Mut28 | openmoc runner: hardcode chi=[1,0] | MR04 group-permute |
| Mut29 | openmoc adapter: fuel-sigt no sigma_a update | (equivalent on OpenMOC) |
| Mut30 | openmoc adapter: moderator-sigma-a no sigma_t update | MR07 |
| Mut31 | openmoc adapter: group-permute fuel only | MR04 |
| Mut32 | openmoc adapter: sigma-s identity | MR06 |
| Mut33 | openmoc adapter: fuel-radius shrink instead of grow | MR08 |
| Mut34 | openmc adapter: particles no-op | MR12 |
| Mut35-Mut38 | OpenMC twins of Mut28, Mut31, Mut32, Mut33 | matched-pair κ |
| Mut39 | openmoc runner: half_y = x_extent/2 | MR01 |
| Mut40 | OpenMC twin of Mut39 | MR01 |
| Mut41 | openmoc runner: y0 = max(0, fuel_offset_y) | MR02 |
| Mut42 | openmoc runner: x0 = max(0, fuel_offset_x) | MR03 |
| Mut43-Mut44 | OpenMC twins of Mut41, Mut42 | matched-pair κ |

Plus `tools/rescore_matrix.py` to re-evaluate cell outcomes against
new decision rules without re-running the solvers.

### Reports (auto-generated)

* `mutation-detection-matrix.md` / `.csv` — per-MR detection rates
  with Wilson 95% CI, per-mutation cell detail, Cohen's κ on 11
  matched-pair classes, threshold sensitivity sweep.
* `screening-results.md` / `.csv` — semantic-vs-equivalent
  classification on the 45-mutation catalogue (matrix-based rule).
* `cross-program-report.md` — MR14 OpenMOC-vs-OpenMC report. Two
  sections: baseline pairs (8 evaluable, 2 disagreement) and
  per-matched-pair (58 evaluable, 17 disagreement). Headlines: the
  OpenMOC `ScaleModeratorSigmaA(factor=1.5)` 51% disagreement and
  the `Mut12 vs Mut26` solver-dependent 33% disagreement.
* `calibration-report.md` — LLM filter rejection rate on adversarial
  candidates.
* `historical-bugs.md` — four real-bug walkthroughs. Phase-2 update
  added Case 4 (the OpenMOC `CPUSolver` convergence basin pathology
  Phase 2 itself discovered).

## Headline results

### Coverage growth: 4 MR scenarios → 21

Phase 1 had 4 scenarios (2 transforms × 2 solvers). Phase 2 has 21:

* MR01 Rotate90 × 2 solvers (asymmetric source)
* MR02 MirrorX × 2 (off-centre source)
* MR03 MirrorY × 2 (off-centre source)
* MR04 PermuteEnergyGroups × 2
* MR05 ScaleFuelSigmaT × 2
* MR06 ScaleFuelSigmaS × 2
* MR07 ScaleModeratorSigmaA × 2
* MR08 ScaleFuelRadius × 2
* MR12 RefineParticles × OpenMC only (variance-ratio)
* Plus the 4 Phase-1 scenarios still active.

### Ten Phase-1-invisible mutations now caught

The matrix surfaces 10 mutations that no Phase-1 MR sees:

| Mut | Phase-1 outcome | Phase-2 detector |
|-----|-----------------|------------------|
| Mut03 (swap fuel/moderator) | missed by both Phase-1 MRs | MR08 fuel-radius |
| Mut05 (chi-swap-groups runner) | missed | MR06 fuel-sigma-s |
| Mut17 (vacuum boundary OpenMC) | missed by strict less | MR-sigma-a noise-aware |
| Mut18 (batches-too-few OpenMC) | missed by strict less | MR07/MR08 noise-aware |
| Mut20 (OpenMC chi-swap) | missed | MR06 fuel-sigma-s |
| Mut28 (chi-fast-only hardcode) | missed | MR04 group-permute |
| Mut30 (moderator-sigma-a no sigma_t) | not affecting | MR07 |
| Mut31 (group-permute fuel-only) | not affecting | MR04 |
| Mut32 / Mut37 (sigma-s identity) | not affecting | MR06 (Mut37 needs noise-aware) |
| Mut33 / Mut38 (fuel-radius shrink) | not affecting | MR08 |
| Mut39 / Mut40 (hardcode-y-from-x) | not affecting | MR01 rotate-90 |
| Mut41 / Mut42 (clamp-offset-positive) | not affecting | MR02 / MR03 (OpenMOC only) |

Plus a real upstream-relevant finding:

* **OpenMOC `CPUSolver` convergence-basin pathology** (Case 4 in
  historical-bugs.md): two reproducible parameter configurations
  where OpenMOC's power iteration converges to a non-physical
  k_eff in 30-35 iters and OpenMC disagrees by 30-50%. **MR14
  cross-program detects it**; no single-program MR can.

### Cross-solver κ on Phase-2 matched pairs

| Pair class | κ |
|-----------|--:|
| MR01 rotate-90 (Mut39/Mut40) | **1.000** |
| MR02 mirror-x (Mut41/Mut43) | 0.000 (OpenMC misses on MC noise floor) |
| MR03 mirror-y (Mut42/Mut44) | 0.000 (same reason) |
| MR04 chi-fast-only (Mut28/Mut35) | **1.000** |
| MR04 group-permute-fuel-only (Mut31/Mut36) | **1.000** |
| MR06 fuel-sigma-s-identity (Mut32/Mut37) | **1.000** (was 0.000 before noise-aware) |
| MR08 fuel-radius-shrink (Mut33/Mut38) | **1.000** |

### Mut00 false-positive rate: 0 / 21 across all rounds

The identity control never trips a Phase-2 MR (or any earlier MR).

### N12 1/√N law empirically validated

OpenMC source σ at 5000 particles: 0.001787. At 50000 particles
(factor=10): 0.000549. Observed ratio 0.307 vs predicted 0.316 —
**2.8% relative deviation**, far inside the 30% tolerance.

## Known limitations / explicitly deferred

| Item | Why deferred | Tracking |
|------|--------------|----------|
| MaterialTemperatureScaling MR (covers historical Case 2) | Our SUT uses fixed multi-group library; needs Doppler-broadening simulation | [`docs/superpowers/plans/2026-05-13-stage5-phase3-tallies-and-temperature.md`](../superpowers/plans/2026-05-13-stage5-phase3-tallies-and-temperature.md) |
| Tally-symmetry MR (covers historical Case 3) | Runner output is single k_eff; needs per-cell flux tally export | same plan |
| MR15 P0 vs P1 scattering | Runner does not expose `xsdata.order` | catalogue marked `realizable_with_current_sut=False` |
| Mut15/Mut19/Mut21 (OpenMC chi-zero / hardcode-keff / fission-zero) | Pathological OpenMC runs hit the 60s timeout per cell | recorded as `status=error`; honest |
| MR02/MR03 OpenMC κ = 0 | MC noise floor (5000 particles, σ ≈ 0.0018) wider than the 1e-3 geometric shift the mutations produce | activate at higher particle counts or larger fuel offset |

## Reproducing

```bash
# 1. Provision OpenMOC + .NET 8 (Linux cloud session)
.claude/web-setup.sh
# 2. (Optional) provision OpenMC via miniconda-forge
# (script lives inline in commit history; ~15 min)
# 3. Recompute baseline (uses both solvers if available)
OPENMOC_PYTHON=/opt/openmoc-venv/bin/python \
OPENMC_PYTHON=/opt/miniconda3/envs/openmc-env/bin/python \
    python3 tools/mutation_study.py baseline --force
# 4. Recompute the matrix
... matrix --force --all
# 5. Regenerate reports
... stats
python3 tools/cross_program_mr.py
# 6. (Optional) run LLM filter — needs .env with ANTHROPIC_API_KEY etc.
python3 tools/noether_llm_filter.py
python3 tools/noether_filter_calibration.py
```

A fresh end-to-end Phase-2 reproduction (excluding LLM filter) takes
~30-45 min on a Linux cloud session with both venvs provisioned.

## Commit trail

Phase-2 development on branch `claude/continue-phase-2-AdZ6f`:

| Commit | Stage |
|--------|-------|
| `c746a0f` | wip: NOETHER catalogue + LLM filter scaffolding |
| `e8de712` | Phase 2A: 3 MRs + matrix on OpenMOC subset |
| `24b5ed0` | Phase 2B: N06/N08 + variance-ratio + 4 mutants |
| `7254d34` | Phase 2C: OpenMC env + N12 verified + κ on Phase-2 pairs |
| `ea9d02a` | Phase 2D: rename M→Mut, N→MR; MR01 on asymmetric pin-cell |
| `91f0d44` | Phase 2E: MR14 cross-program report + LLM calibration |
| `299ac2b` | Phase 2F: Mut15-21 long-run + MR02/MR03 mirror + per-pair MR14 |
| `a03e5ef` | Phase 2G: noise-aware greater/less + OpenMOC pathology in historical-bugs |
| (this) | Phase 2H: top-level README + Phase-3 plan stubs |
