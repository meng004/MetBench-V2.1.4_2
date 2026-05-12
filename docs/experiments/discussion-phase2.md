# Discussion — Stage 5 Phase 2 (NOETHER)

> Hand-written analysis of Phase 2 results. Companion to Phase 1's
> [`discussion.md`](discussion.md). The matrix this discussion reads
> from is the same auto-generated
> [`mutation-detection-matrix.md`](mutation-detection-matrix.md), now
> extended with three new MR scenarios from the NOETHER catalogue.

## What Phase 2 added

1. **NOETHER candidate catalogue** (`tools/noether_candidates.py`) —
   fifteen MetaPattern × parameter candidates derived from the operator
   algebra in Pham et al. (2026), restricted to the four blocks our
   2D-pin-cell + static-eigenvalue SUT actually exercises (`m_inv`,
   `m_mono`, `m_conv`, `m_cmp`). The catalogue is transparent about
   what we excluded and why: `m_rev` (dissipative scattering), `m_dyn`
   (no trajectories in static eigenvalue), `m_adj` (no adjoint solver
   wired in).
2. **LLM filter** (`tools/noether_llm_filter.py`) — Anthropic-format
   Messages API filter with prompt caching, `.env` loading, and a
   resume cache. Driven against DeepSeek's anthropic-compatible
   gateway in this study (model `deepseek-v4-pro[1m]`); the script
   works against any `ANTHROPIC_BASE_URL` endpoint. Per-candidate
   verdicts land in `_data/noether/llm-verdicts.json` for audit.
3. **Six new MR scenarios** wired into the existing matrix harness
   (5 OpenMOC active in this session; OpenMC mirrors written but
   deferred until OpenMC venv is installed):
   - `openmoc-pincell-group-permute` (N04, `m_inv`, assertion=`approx`)
   - `openmoc-pincell-fuel-sigma-t` (N05, `m_mono`, assertion=`less`)
   - `openmoc-pincell-moderator-sigma-a` (N07, `m_mono`, assertion=`less`)
   - `openmoc-pincell-fuel-sigma-s` (N06, `m_mono`, assertion=`less`,
     factor 0.5)
   - `openmoc-pincell-fuel-radius` (N08, `m_mono`, assertion=`greater`,
     factor 1.05 — small perturbation, must stay under-moderated)
   - `openmc-pincell-particles-refine` (N12, `m_conv`,
     assertion=`variance-ratio`, factor 10, target_ratio = 1/√10)
4. **Three orchestrator extensions**:
   - **Per-scenario `factor_override`** so MRs that only hold in a
     small-perturbation regime (e.g. fuel-radius) can pin their own
     factor without conflicting with the CLI default (1.5).
   - **`approx` assertion** with relative tolerance — the m_inv MRs.
   - **`variance-ratio` assertion** comparing follow-up σ to source σ
     against a target ratio with relative tolerance — the m_conv MR.
5. **Seven new mutations** (M28-M34) **specifically designed to break
   the new MRs' algebraic invariants**:
   - M28 (`runner-chi-fast-only`) — hard-codes chi=[1,0]; breaks N04.
   - M29 (`adapter-fuel-sigt-no-siga-update`) — JSON inconsistency.
   - M30 (`adapter-moderator-sigma-a-no-sigt-update`) — breaks N07 on
     OpenMOC because OpenMOC reads moderator absorption from sigma_t.
   - M31 (`adapter-group-permute-fuel-only`) — half-permutes only fuel;
     breaks N04.
   - M32 (`adapter-fuel-sigma-s-identity`) — adapter ignores factor;
     breaks N06.
   - M33 (`adapter-fuel-radius-shrink`) — direction inversion;
     breaks N08.
   - M34 (`adapter-particles-no-op`) — particles unchanged in
     follow-up; breaks N12 variance ratio (deferred until OpenMC).

## Headline Phase-2 result

**Five Phase-1-invisible mutations are caught by Phase-2 MRs**, plus
**M03 and M05** flip from missed-by-Phase-1 to detected-by-Phase-2:

| Mutation | Phase-1 outcome | Phase-2 outcome | New detector |
|----------|-----------------|-----------------|--------------|
| M03 (swap fuel/moderator) | missed by both Phase-1 MRs | **detected** | `fuel-radius` (N08) |
| M05 (chi-swap-groups) | missed by both Phase-1 MRs | **detected** | `fuel-sigma-s` (N06) |
| M28 (chi-fast-only) | missed | **detected** | `group-permute` (N04) |
| M30 (moderator sa, no sigt) | not affecting | **detected** | `moderator-sigma-a` (N07) |
| M31 (group-permute fuel only) | not affecting | **detected** | `group-permute` (N04) |
| M32 (sigma-s identity adapter) | not affecting | **detected** | `fuel-sigma-s` (N06) |
| M33 (radius direction inversion) | not affecting | **detected** | `fuel-radius` (N08) |

M28 remains the canonical example: a chi hard-coding bug is
*invisible* to every monotonicity-style MR (Phase 1's `nu-sigma-f`
and `sigma-a` MRs both miss it because the multiplicative scaling
cancels through the chi term). Only the symmetry MR (N04 group
permutation) detects it — the textbook NOETHER use case.

The newest finding from this round is **M03 caught by N08
fuel-radius**: M03 swaps which cell holds fuel vs moderator, leaving
k_eff at 1.43 (close to the unperturbed 1.13 — Phase-1 MRs miss
this). But growing the fuel cylinder by 5% with a swapped layout
*shrinks* the fissile volume (because cylinder=moderator now), so
k_eff *drops* to 0.49. N08's `greater` assertion fails sharply. This
is the kind of geometrical / topological fault that only a geometry-
sensitive MR can see.

M00 (identity) detects on **0/15** scenarios — false-positive control
holds across the doubled MR set.

## Per-MR detection rates (OpenMOC subset)

| MR scenario | n affected | detected | rate |
|-------------|-----------:|---------:|-----:|
| `openmoc-pincell-nu-sigma-f` (Phase 1) | 12 | 7 | 58.3% |
| `openmoc-pincell-sigma-a` (Phase 1) | 10 | 4 | 40.0% |
| `openmoc-pincell-group-permute` (N04) | 8 | 4 | 50.0% |
| `openmoc-pincell-fuel-sigma-t` (N05) | 7 | 1 | 14.3% |
| `openmoc-pincell-moderator-sigma-a` (N07) | 8 | 3 | 37.5% |
| `openmoc-pincell-fuel-sigma-s` (N06) | 8 | **5** | **62.5%** |
| `openmoc-pincell-fuel-radius` (N08) | 8 | **5** | **62.5%** |

**N06 and N08 are the new top-rate detectors** (62.5% each, beating
both Phase-1 MRs). Both are sensitive to small perturbations that
the multiplicative monotonicity MRs cancel through.

Direct comparison of rates is misleading — the denominator is the
set of *affected* mutations, which differs across MRs. The right
question is "does adding a new MR convert any previously-missed
mutation into `detected`?" — and yes, **seven** mutations across the
expanded catalogue (M03, M05, M28, M30, M31, M32, M33) move from
"missed by every Phase-1 MR" to "detected by at least one Phase-2 MR".

The low rate of N05 (`fuel-sigma-t`, 14.3%) deserves a note: scaling
fuel sigma_t by 1.5 collapses k_eff from 1.13 to 0.11 because most
mutations leave the multiplicative dependence intact, so k_followup is
strictly less than k_source for almost everything. The MR is
therefore satisfied by most mutations (so they're "missed"). N05's
real value is in catching mutations that *invert* this monotonicity
direction (e.g., a future "scale wrong material" bug analogous to M14
but on sigma_t) rather than as a high-rate detector.

## OpenMC matrix added in this round

The OpenMC half of the matrix is now populated (7/8 OpenMC scenarios
ran; M15/M19/M21 — chi-zero, hardcode-keff, fission-zero — produce
pathological OpenMC subprocesses, 60s timeout caps them). Key OpenMC
findings:

* **N12 variance-ratio empirically validated**: source σ at 5000
  particles = 0.001787; followup σ at 50000 particles = 0.000549.
  Observed ratio = 0.307. Predicted (1/√10) = 0.316. Relative
  deviation = **2.8%** — well inside the 30% MR tolerance. The
  textbook MC convergence law holds. M34 (adapter no-op on particles)
  is correctly detected by this MR.

* **Cross-solver κ on Phase-2 matched pairs**:
  | Pair class | κ | Note |
  |-----------|--:|------|
  | N04 chi-fast-only (M28/M35) | **1.000** | both detect |
  | N04 group-permute-fuel-only (M31/M36) | **1.000** | both detect |
  | N06 fuel-sigma-s-identity (M32/M37) | **0.000** | **MOC detects, MC misses** |
  | N08 fuel-radius-shrink (M33/M38) | **1.000** | both detect |

* **M37 (OpenMC twin of M32) is the lone Phase-2 disagreement**.
  Same logical bug (adapter no-op on sigma_s scaling), same MR, but
  M37 is missed on OpenMC. The reason is MC seed-noise: OpenMC's
  k_eff at 5000 particles fluctuates by ~σ = 0.0018 between runs,
  and the followup happened to land **bit-for-bit fractionally below**
  the source (1.124500014025275**9** vs 1.124500014025276**0**). The
  strict `k_followup < k_source` assertion passes by ~10⁻¹⁵, masking
  the bug. OpenMOC's deterministic k_eff is bit-equivalent across
  identical inputs, so the same assertion strictly fails and the
  bug is detected. **This is exactly the "tolerance-aware GreaterThan
  / LessThan" hand-off note Phase 1 already raised**; Phase 2
  reproduces it on a new MR class. The fix (replace `<` with
  `< -3·σ`) is one line away once a tolerance plumbing is added.

## Bonus finding — OpenMOC convergence pathology

A side discovery from running the matrix end-to-end on both solvers:
**OpenMOC's `CPUSolver` converges to a wrong eigenvalue when the
moderator absorption is scaled by 1.5**, while OpenMC on the same
JSON produces a physically sensible answer.

Reproduction (factor 1.5 ScaleModeratorSigmaA on the reference
`SUT/openmoc/sample/pincell.json`):

| Solver | k_eff | Iterations | Comment |
|--------|------:|-----------:|---------|
| OpenMOC `CPUSolver` | **0.4764** | **30** | converged=true, but stuck on wrong fixed point |
| OpenMC multi-group (5000 particles, 60 batches) | **0.9683 ± 0.0017** | (n/a, MC) | physically expected k for ~17% absorption increase |

A factor sweep on OpenMOC alone exposes the discontinuity:

| factor | OpenMOC k_eff | iters |
|-------:|--------------:|------:|
| 1.01 | 1.12935 | 552 |
| 1.05 | 1.11476 | 548 |
| 1.10 | 1.09710 | 544 |
| 1.20 | 1.06352 | 535 |
| **1.50** | **0.47635** | **30** ← discontinuity |

The fuel-sigma-a Phase-1 scenario shows a *different* bad basin: at
factor 1.01 OpenMOC converges in 26 iters to k=0.508 (wildly wrong);
at factor 1.05 it correctly returns k=1.091 in 542 iters.

Tightening `convergence_threshold` from 1e-4 to 1e-7 does **not**
fix it — the solver still claims converged at iter 30 with
`k_eff=0.47635`. So this is "power iteration converges to a
non-physical eigenvalue under certain parameter combinations,"
not "needs more iterations."

The implications:

1. **Our matrix evaluation is unaffected**: the assertion check
   operates on whatever k OpenMOC reports; both the source and
   follow-up runs of any given mutant go through the same
   `CPUSolver` path, so the pathology is internally consistent
   within each cell. The boolean detected/missed verdict is
   correct.
2. **The magnitude numbers in the per-MR detail table are not
   physical** for the OpenMOC moderator-sigma-a rows. Readers
   should consult the OpenMC twin scenarios for physical realism.
3. **This is exactly the kind of fault N14 (cross-program OpenMOC
   vs OpenMC) is designed to surface**: a 51% k_eff disagreement
   on the same JSON between two physically-equivalent solvers is
   well outside any MC noise or MOC discretisation budget. Folding
   the existing `tools/cross_program_comparison.py` into a matrix
   cell would make this a first-class report. The bug found by
   that comparison is a *real* upstream OpenMOC weakness, not an
   MR artefact.

We have not filed a bug upstream — the issue may be a known
limitation of basic power iteration without acceleration (Wielandt
shift, Anderson, …), or it may be specific to this particular
material configuration. Further investigation is out of scope for
the MR-matrix study.

## Symmetry interaction note (M05)

A non-obvious result worth flagging: M05 (`runner-chi-swap-groups`,
which reverses chi inside the runner) is **not** detected by the N04
group-permute MR even though it conceptually breaks chi handling.
Why: M05 reverses chi inside the runner *unconditionally*, and the
N04 follow-up *also* reverses chi (along with all other per-group
arrays). The two reversals compose to identity. M05 + N04 yields a
physically equivalent model.

This is a real coverage gap and a useful reminder that MRs interact
with mutations as algebraic operators — composing two transformations
can recover an equivalent model. The lesson for the next NOETHER
expansion is that an MR family hostile to a specific bug class is
still defeated when the mutation lies in the kernel of the MR
transformation.

## LLM filter — calibration notes

Final verdicts on the fifteen candidates: **14 valid, 1 uncertain, 0
invalid**. Confidences cluster between 0.75 and 0.99, with median
~0.95. The lone `uncertain` is N10 (`conv-num-azim-refine`) —
correctly flagging that "tracking refinement reduces |k_eff − k_true|"
is true asymptotically but the SUT does not expose `k_true`, so the
assertion is unverifiable as stated. All other candidates (including
the genuinely vacuous-on-this-SUT N01-N03 mirror/rotation MRs and the
out-of-scope-without-runner-changes N15 P0-vs-P1 MR) survived. This
**100% survival rate** is itself worth flagging: an LLM filter that
never rejects anything is providing zero information beyond a
plausibility check. Future iterations should either tighten the
rubric ("invalidate any MR that is vacuous on the reference SUT") or
add adversarial candidates to validate the filter's discrimination.

The filter pattern (Anthropic-compatible gateway + ephemeral cache on
the framing block) is reusable for future MR catalogues. Mean
per-candidate latency at our settings was ~75 s with `max_tokens=4096`
(set high enough to leave room for the `thinking` blocks DeepSeek-V4
emits before the JSON answer — the original `max_tokens=1024` was
exhausted by the thinking phase and produced empty `text` blocks).

## Deferred work

Out of scope for this Phase 2 commit:

1. **OpenMC matrix run** — all seven OpenMC scenarios (the four
   Phase-1 OpenMC ones plus the new `fuel-sigma-s`, `fuel-radius`,
   `particles-refine`) are wired but currently report
   `status=skipped-no-openmc`. The OpenMC adapters are already
   written and matched against the OpenMOC ones on the same JSON
   schema; running on a session with the OpenMC venv (or with
   `METBENCH_FORCE_OPENMC=1`) populates the missing rows without
   code changes. This unblocks variance-ratio validation for N12
   (the only assertion that requires non-zero σ).
2. **N14 (cross-program OpenMOC vs OpenMC)** — already partially
   covered by `tools/cross_program_comparison.py`; folding it into
   the matrix harness as an MR cell is a small refactor, deferred.
3. **N01-N03 vacuous-on-this-SUT note** — the LLM correctly marked
   these `valid` in the abstract, but on the symmetric centred
   pin-cell case they reduce to identity transformations
   (rotation/mirror produces the same JSON). An asymmetric
   pin-cell sample (e.g. off-centred fuel, asymmetric extents) is
   the prerequisite for these MRs to do any work.
4. **N15 (P0 vs P1 scattering)** — runner does not expose the
   scattering order; `realizable_with_current_sut=False` in the
   catalogue.

## Hand-off to Phase 3 (or next sprint)

Concrete priorities, in order:

1. **OpenMC matrix run** — populate the seven `openmc-pincell-*` rows.
   Cohen's κ on the matched-pair index will then have non-degenerate
   data for the new scenarios; the N12 variance-ratio MR can finally
   be empirically tested (it requires non-zero σ which OpenMOC does
   not produce). Concretely: install OpenMC into a venv at
   `/opt/miniconda3/envs/openmc-env/bin/python` (or set
   `OPENMC_PYTHON`), then re-run
   `tools/mutation_study.py matrix --force --all`.
2. **Cohen's κ on the new MRs** — once OpenMC is back, the matched-pair
   index in `mutation_study.py` should grow new entries for the
   Phase-2 mutations (M28/M? for N04, M32/M? for N06, M33/M? for N08).
   Today's commit only has OpenMOC-side mutations for the new MRs;
   matched OpenMC twins should be added.
3. **N14 as a first-class MR** — refactor
   `cross_program_comparison.py` to write per-cell results into the
   matrix data dir. This makes cross-solver agreement reportable
   alongside per-solver detection rates.
4. **Asymmetric pin-cell test case** — one new entry under
   `SUT/openmoc/sample/` that breaks the C4 symmetry of the reference
   `pincell.json`, so N01-N03 stop being identity transformations and
   start exercising the rotation / reflection invariants.
5. **Per-MR factor sweep** — N05 and N08 detection rates depend
   strongly on the chosen factor (N05 uses 1.5, dominating the
   absorption physics; N08 uses 1.05, deliberately small). A factor
   sweep would quantify the rate-vs-coverage trade-off and could
   surface mutations that survive the canonical factor but flip at a
   nearby one.
