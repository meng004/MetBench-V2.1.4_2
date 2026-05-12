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
   - `openmoc-pincell-group-permute` (MR04, `m_inv`, assertion=`approx`)
   - `openmoc-pincell-fuel-sigma-t` (MR05, `m_mono`, assertion=`less`)
   - `openmoc-pincell-moderator-sigma-a` (MR07, `m_mono`, assertion=`less`)
   - `openmoc-pincell-fuel-sigma-s` (MR06, `m_mono`, assertion=`less`,
     factor 0.5)
   - `openmoc-pincell-fuel-radius` (MR08, `m_mono`, assertion=`greater`,
     factor 1.05 — small perturbation, must stay under-moderated)
   - `openmc-pincell-particles-refine` (MR12, `m_conv`,
     assertion=`variance-ratio`, factor 10, target_ratio = 1/√10)
4. **Three orchestrator extensions**:
   - **Per-scenario `factor_override`** so MRs that only hold in a
     small-perturbation regime (e.g. fuel-radius) can pin their own
     factor without conflicting with the CLI default (1.5).
   - **`approx` assertion** with relative tolerance — the m_inv MRs.
   - **`variance-ratio` assertion** comparing follow-up σ to source σ
     against a target ratio with relative tolerance — the m_conv MR.
5. **Seven new mutations** (Mut28-Mut34) **specifically designed to break
   the new MRs' algebraic invariants**:
   - Mut28 (`runner-chi-fast-only`) — hard-codes chi=[1,0]; breaks MR04.
   - Mut29 (`adapter-fuel-sigt-no-siga-update`) — JSON inconsistency.
   - Mut30 (`adapter-moderator-sigma-a-no-sigt-update`) — breaks MR07 on
     OpenMOC because OpenMOC reads moderator absorption from sigma_t.
   - Mut31 (`adapter-group-permute-fuel-only`) — half-permutes only fuel;
     breaks MR04.
   - Mut32 (`adapter-fuel-sigma-s-identity`) — adapter ignores factor;
     breaks MR06.
   - Mut33 (`adapter-fuel-radius-shrink`) — direction inversion;
     breaks MR08.
   - Mut34 (`adapter-particles-no-op`) — particles unchanged in
     follow-up; breaks MR12 variance ratio (deferred until OpenMC).

## Headline Phase-2 result

**Five Phase-1-invisible mutations are caught by Phase-2 MRs**, plus
**Mut03 and Mut05** flip from missed-by-Phase-1 to detected-by-Phase-2:

| Mutation | Phase-1 outcome | Phase-2 outcome | New detector |
|----------|-----------------|-----------------|--------------|
| Mut03 (swap fuel/moderator) | missed by both Phase-1 MRs | **detected** | `fuel-radius` (MR08) |
| Mut05 (chi-swap-groups) | missed by both Phase-1 MRs | **detected** | `fuel-sigma-s` (MR06) |
| Mut28 (chi-fast-only) | missed | **detected** | `group-permute` (MR04) |
| Mut30 (moderator sa, no sigt) | not affecting | **detected** | `moderator-sigma-a` (MR07) |
| Mut31 (group-permute fuel only) | not affecting | **detected** | `group-permute` (MR04) |
| Mut32 (sigma-s identity adapter) | not affecting | **detected** | `fuel-sigma-s` (MR06) |
| Mut33 (radius direction inversion) | not affecting | **detected** | `fuel-radius` (MR08) |

Mut28 remains the canonical example: a chi hard-coding bug is
*invisible* to every monotonicity-style MR (Phase 1's `nu-sigma-f`
and `sigma-a` MRs both miss it because the multiplicative scaling
cancels through the chi term). Only the symmetry MR (MR04 group
permutation) detects it — the textbook NOETHER use case.

The newest finding from this round is **Mut03 caught by MR08
fuel-radius**: Mut03 swaps which cell holds fuel vs moderator, leaving
k_eff at 1.43 (close to the unperturbed 1.13 — Phase-1 MRs miss
this). But growing the fuel cylinder by 5% with a swapped layout
*shrinks* the fissile volume (because cylinder=moderator now), so
k_eff *drops* to 0.49. MR08's `greater` assertion fails sharply. This
is the kind of geometrical / topological fault that only a geometry-
sensitive MR can see.

Mut00 (identity) detects on **0/15** scenarios — false-positive control
holds across the doubled MR set.

## Per-MR detection rates (OpenMOC subset)

| MR scenario | n affected | detected | rate |
|-------------|-----------:|---------:|-----:|
| `openmoc-pincell-nu-sigma-f` (Phase 1) | 12 | 7 | 58.3% |
| `openmoc-pincell-sigma-a` (Phase 1) | 10 | 4 | 40.0% |
| `openmoc-pincell-group-permute` (MR04) | 8 | 4 | 50.0% |
| `openmoc-pincell-fuel-sigma-t` (MR05) | 7 | 1 | 14.3% |
| `openmoc-pincell-moderator-sigma-a` (MR07) | 8 | 3 | 37.5% |
| `openmoc-pincell-fuel-sigma-s` (MR06) | 8 | **5** | **62.5%** |
| `openmoc-pincell-fuel-radius` (MR08) | 8 | **5** | **62.5%** |

**MR06 and MR08 are the new top-rate detectors** (62.5% each, beating
both Phase-1 MRs). Both are sensitive to small perturbations that
the multiplicative monotonicity MRs cancel through.

Direct comparison of rates is misleading — the denominator is the
set of *affected* mutations, which differs across MRs. The right
question is "does adding a new MR convert any previously-missed
mutation into `detected`?" — and yes, **seven** mutations across the
expanded catalogue (Mut03, Mut05, Mut28, Mut30, Mut31, Mut32, Mut33) move from
"missed by every Phase-1 MR" to "detected by at least one Phase-2 MR".

The low rate of MR05 (`fuel-sigma-t`, 14.3%) deserves a note: scaling
fuel sigma_t by 1.5 collapses k_eff from 1.13 to 0.11 because most
mutations leave the multiplicative dependence intact, so k_followup is
strictly less than k_source for almost everything. The MR is
therefore satisfied by most mutations (so they're "missed"). MR05's
real value is in catching mutations that *invert* this monotonicity
direction (e.g., a future "scale wrong material" bug analogous to Mut14
but on sigma_t) rather than as a high-rate detector.

## Phase-3 Family A — Tally-symmetry MRs (Case 3 coverage)

Phase-3 plan delivered: per-cell scalar flux tally output and a
mirror-invariance MR family that catches tally-export bugs which
preserve k_eff. The Phase-2 hand-off doc spelled out the design;
this commit lands the implementation.

### Runner changes (backward-compatible)

Both runners now emit `flux_per_cell` in the result JSON:

```json
"flux_per_cell": {
  "fuel":      [<group-0-flux>, <group-1-flux>],
  "moderator": [<group-0-flux>, <group-1-flux>]
}
```

* OpenMOC: walks the FSR list, dispatches by `cell.getId()`, sums
  scalar flux per group via `solver.getFlux(fsr, group_index)`.
* OpenMC: builds `openmc.Tally` with `CellFilter([fuel, mod])` ×
  `EnergyFilter([1e-5, 0.625, 2e7])`, reads from the statepoint
  post-run, reverses energy axis so MetBench's [fast, thermal]
  convention matches OpenMOC's.

Existing samples / scenarios pass through identically — the flux
field is additive, not replacing anything.

### New scenarios + assertion

Four scenarios on `pincell-offcentre.json`:

| ID | Solver | Transform |
|---|---|---|
| openmoc-pincell-mirror-x-tally | openmoc | MirrorX |
| openmc-pincell-mirror-x-tally | openmc | MirrorX |
| openmoc-pincell-mirror-y-tally | openmoc | MirrorY |
| openmc-pincell-mirror-y-tally | openmc | MirrorY |

Assertion: **`flux-pointwise-approx`** — `max |Δflux| / max(|src|,
|flw|, ε) ≤ tolerance_rel` over every (cell, group) entry.
OpenMOC tolerance 1e-3 (deterministic); OpenMC 0.05 (3·σ on per-
cell flux estimates at 5000 particles).

### Mut47 — tally-only OpenMOC bug

Replaces the cell-ID dispatch in OpenMOC's tally extraction with
y-sign bucketing. Source case (fuel at y=-0.08) buckets fuel
correctly; MirrorX follow-up (fuel at y=+0.08) swaps fuel and
moderator in the output dict. **k_eff is unaffected** (mutation
runs post-solve), so no Phase-1/Phase-2 MR sees the bug.

Matrix outcome:

| MR | Mut47 verdict |
|----|---------------|
| All Phase-1 + Phase-2 k_eff-based MRs | miss (every single one) |
| **MR02-tally (openmoc-pincell-mirror-x-tally)** | **detected** |
| MR03-tally (openmoc-pincell-mirror-y-tally) | miss (the y-sign bucketing is invariant under MirrorY, which only flips x_offset) |

Historical-bug coverage now reaches **3/4**: the Case-3-class
"tally-export bug invisible to eigenvalue MRs" pattern is now
detected by an actual MR scenario. Mut00 false-positive rate stays
**0/27**.

OpenMC twin of Mut47 deferred: the simplest swap-the-CellFilter
patches turn out to be tally-symmetric (the post-statepoint
extraction matches by cell ID rather than filter index, so the
output dict ends up correct regardless). A real OpenMC tally-export
bug analog would need either (a) plumbing fuel-cell geometric
position into the post-statepoint loop, or (b) mutating the openmc
binary itself — both out of MetBench's MR-matrix scope. Phase-3
PR-1 ships the OpenMOC-only demo.

## Tolerance-aware greater/less assertions (Phase-1 hand-off completed)

Phase-1's discussion file flagged "tolerance-aware GreaterThan /
LessThan" as the second-priority hand-off item. Phase 2's MC-noise
misses (Mut37 on MR06 fuel-sigma-s; Mut43, Mut44 on MR02/MR03 mirror)
re-confirmed it: a strict `k_followup < k_source` check passes when
k_followup happens to land 10⁻¹⁵ below k_source by RNG luck, so an
identity-bug adapter slips through.

The fix in `evaluate_mr` is one line per branch:

```python
if scenario.get("noise_aware"):
    sigma = float(cell.get("k_source_std", 0.0) or 0.0)
    margin = max(3.0 * sigma, tolerance_rel * abs(k_src))
    return k_flw < k_src - margin   # `less` branch
```

Opt-in via the SCENARIOS field `noise_aware: True` so Phase-1 strict
semantics stay the default. All six OpenMC less/greater scenarios
(Phase-1 nu-sigma-f and sigma-a; Phase-2 fuel-sigma-t,
moderator-sigma-a, fuel-sigma-s, fuel-radius) opt in. OpenMOC
deterministic scenarios stay strict (σ = 0, so the noise margin is
zero anyway).

To avoid an expensive re-run of every OpenMC mutation, Phase 2 ships
**`tools/rescore_matrix.py`**: walks every `_data/candidates/*/matrix.json`,
re-evaluates outcome on each `ran` cell against the current scenario
config, and writes the corrected outcome / `assertion_passed` back.
Only the *decision rule* changed, not the *measurements*, so
re-scoring is sufficient.

Effect on detection counts after rescore (only changes shown):

| Mutation | Scenario | Before | After |
|----------|----------|--------|-------|
| Mut17 vacuum-boundary | openmc-pincell-sigma-a | missed | **detected** |
| Mut18 batches-too-few | openmc-pincell-moderator-sigma-a | missed | **detected** |
| Mut18 batches-too-few | openmc-pincell-fuel-radius | missed | **detected** |
| Mut20 chi-swap-groups | openmc-pincell-fuel-sigma-s | missed | **detected** |
| Mut37 sigma-s identity adapter | openmc-pincell-fuel-sigma-s | missed | **detected** |

Five flips, **all from missed → detected** (i.e. true positives the
strict rule missed). κ on the MR06 matched pair (Mut32 / Mut37) goes
from **0.000 → 1.000**: both solvers now agree to detect, the
solver-asymmetric MC blind spot closed.

Mut00 false-positive rate stays at **0/21** — noise-aware is a
strictly safer (more conservative) assertion: it never flips a
correctly-passing identity case to "detected", only catches genuine
bugs that strict was too lenient on.

The mirror MRs (MR02/MR03) on OpenMC remain at 0% even with
noise-aware on, because they use the `approx` assertion and their
geometric perturbation (a few-thousandths-of-cm fuel offset) produces
a k_eff shift smaller than 3·σ (the irreducible MC noise floor at
5000 particles). They would activate at higher particle counts or
larger offsets — flagged for future rounds.

## OpenMC matrix added in this round

The OpenMC half of the matrix is now populated (7/8 OpenMC scenarios
ran; Mut15/Mut19/Mut21 — chi-zero, hardcode-keff, fission-zero — produce
pathological OpenMC subprocesses, 60s timeout caps them). Key OpenMC
findings:

* **MR12 variance-ratio empirically validated**: source σ at 5000
  particles = 0.001787; followup σ at 50000 particles = 0.000549.
  Observed ratio = 0.307. Predicted (1/√10) = 0.316. Relative
  deviation = **2.8%** — well inside the 30% MR tolerance. The
  textbook MC convergence law holds. Mut34 (adapter no-op on particles)
  is correctly detected by this MR.

* **Cross-solver κ on Phase-2 matched pairs**:
  | Pair class | κ | Note |
  |-----------|--:|------|
  | MR04 chi-fast-only (Mut28/Mut35) | **1.000** | both detect |
  | MR04 group-permute-fuel-only (Mut31/Mut36) | **1.000** | both detect |
  | MR06 fuel-sigma-s-identity (Mut32/Mut37) | **0.000** | **MOC detects, MC misses** |
  | MR08 fuel-radius-shrink (Mut33/Mut38) | **1.000** | both detect |

* **Mut37 (OpenMC twin of Mut32) is the lone Phase-2 disagreement**.
  Same logical bug (adapter no-op on sigma_s scaling), same MR, but
  Mut37 is missed on OpenMC. The reason is MC seed-noise: OpenMC's
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

## MR14 (cross-program OpenMOC vs OpenMC) — first-class report

The NOETHER catalogue's `m_cmp` block (MR14: OpenMOC vs OpenMC k_eff
agreement) is now reportable through a dedicated tool,
`tools/cross_program_mr.py`. It reads baseline.json and compares the
unpatched OpenMOC follow-up k_eff against the OpenMC twin for every
matched transform pair, producing
`docs/experiments/cross-program-report.md`.

Budget rule: `|Δk| ≤ max(3·σ_OpenMC, 1.0%·k_OpenMC)`. The 1% relative
budget is empirically calibrated for this SUT (16-azim MOC tracking
vs 5000-particle MC) — the paper's 0.5% (Table 3) flags routine
discretization spread as "disagreement" and is too tight in practice.

Result on the 8 evaluable baseline pairs: **2 disagreements**:

| Transform | Δk | budget | excess | notes |
|-----------|---:|------:|------:|-------|
| ScaleModeratorSigmaA | **0.49196** | 0.00968 | 0.48228 | OpenMOC k=0.476 vs OpenMC k=0.968 — 51% disagreement. The OpenMOC `CPUSolver` convergence pathology documented below. |
| ScaleFuelSigmaT | 0.00165 | 0.00113 | 0.00052 | 1.5% of k at k≈0.11 — borderline; collapse-near-zero regime amplifies any MOC vs MC drift. |

The first row is the killer: **MR14 catches a real numerical artefact
in OpenMOC** that no monotonicity MR could ever flag (the artefact is
solver-specific, source and follow-up of OpenMOC are mutually
consistent). This is the textbook NOETHER use case: cross-program
comparison surfaces faults that single-program MRs cannot.

Per-mutation MR14 (compare OpenMOC mutant k_eff vs OpenMC mutant
k_eff for the same transform + same patch class) is left as a small
follow-up — the standalone tool intentionally avoids invasive
changes to `matrix_one`. The data is already in `_data/candidates/*/matrix.json`.

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
3. **This is exactly the kind of fault MR14 (cross-program OpenMOC
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

## MR02 / MR03 (mirror x / y) on an off-centre pin-cell

The same vacuous-on-symmetric-input issue that MR01 had also applies
to MR02 / MR03 (mirror reflections about x or y). The fix is the
same kind: an off-centre sample where the mirror is non-trivial.
Phase-2 adds **`SUT/openmoc/sample/pincell-offcentre.json`** (fuel
offset (+0.10, -0.08) cm, square 1.30 × 1.30 cm extent) and the
required runner change — both `openmoc_runner.py` and
`openmc_runner.py` now read `fuel_offset_x_cm` / `fuel_offset_y_cm`
from the geometry block (defaulting to 0, so all earlier samples are
unaffected).

Mirror adapters flip the relevant sign:

* `openmoc/openmc_input_adapter_mirror_x.py`: `y_offset → -y_offset`
* `openmoc/openmc_input_adapter_mirror_y.py`: `x_offset → -x_offset`

Mutations that break the invariance use `max(0, offset)` so negative
offsets are silently clamped to 0:

* **Mut41** OpenMOC `y = max(0, y_offset)` → breaks MR02 (mirror x)
* **Mut42** OpenMOC `x = max(0, x_offset)` → breaks MR03 (mirror y)
* **Mut43**, **Mut44**: OpenMC twins.

Matrix outcome:

| Pair | MR02 (OpenMOC) | MR03 (OpenMOC) | MR02 (OpenMC) | MR03 (OpenMC) |
|------|----------------|----------------|---------------|---------------|
| Mut41 / Mut43 | **DETECT** | miss (not affected) | miss (MC noise hides 2e-3 shift) | — |
| Mut42 / Mut44 | miss (not affected) | **DETECT** | — | miss (MC noise) |

OpenMOC catches both because its 1e-5 strict tolerance is plenty for
the 1e-3 geometric shift. OpenMC's necessary 0.5%-of-k_eff tolerance
(driven by σ ≈ 0.0018 at 5000 particles) is wider than the geometric
shift produces, so MC misses both. **Same MC-vs-MOC strict-inequality
fragility we documented for MR06 fuel-sigma-s** (Mut32 detected,
Mut37 missed) reproduces here. Cohen's κ on the MR02 and MR03 pairs
is **0.000** for the same reason.

The headline is that the **MRs themselves are now correctly active
on this SUT** — MR01 (Rotate90 on pincell-asymmetric.json), MR02 and
MR03 (mirror x/y on pincell-offcentre.json). Closing the residual
"vacuous-on-this-SUT" gap from the Phase-2 catalogue.

**Caveat — OpenMOC pathology #2**: at `pin extent = 1.50 cm`,
`fuel_offset = (0.15, -0.10) cm`, OpenMOC's `CPUSolver` converges to
k=0.5356 in only 35 iters while OpenMC gives 0.959 on the same
input. This is a SECOND example of the convergence basin pathology
we documented at `ScaleModeratorSigmaA(factor=1.5)`. The off-centre
sample we ship uses the tighter (1.30, 0.10, -0.08) configuration
that stays out of OpenMOC's bad basin — but the discovery confirms
that OpenMOC's basic power iteration has multiple narrow bad
configurations on this material set, and MR14 cross-program would
catch them.

## MR01 (Rotate90) on an asymmetric pin-cell

The reference `SUT/openmoc/sample/pincell.json` is square (1.26 × 1.26 cm)
with centred fuel, so the geometric-invariance MRs (MR01-MR03) reduce
to identity on this case — the rotated/mirrored JSON is byte-identical
to the source. To give MR01 actual fault-detection power, Phase 2 ships
**`SUT/openmoc/sample/pincell-asymmetric.json`** (1.00 × 1.50 cm, same
cross sections, centred fuel). The Rotate90 adapter swaps x_extent and
y_extent — non-trivial on the asymmetric case, no-op on the symmetric
one. Mirror MRs (MR02/MR03) require off-centre fuel and stay deferred
until the runner schema grows a `fuel_offset_cm` field.

A dedicated pair of mutations targets the new MR:

* **Mut39** (`openmoc-runner-hardcode-y-from-x`): replaces
  `half_y = g["y_extent_cm"] / 2.0` with `half_y = g["x_extent_cm"] / 2.0`
  in the OpenMOC runner.
* **Mut40**: OpenMC twin, same edit applied to `openmc_runner.py`.

Both mutations are **silently equivalent on the symmetric pin-cell**
(x_extent == y_extent, so swapping is a no-op for the buggy code path).
Every Phase-1 and earlier-Phase-2 MR uses the symmetric reference, so
**every other MR misses both Mut39 and Mut40**. Only MR01 — on the
asymmetric case — detects them:

| MR | Mut39 (OpenMOC) | Mut40 (OpenMC) |
|----|-----------------|----------------|
| Phase 1 nu-sigma-f / sigma-a / OpenMC nu-sigma-f / sigma-a | missed (n/a or x==y) | missed (n/a or x==y) |
| MR04 / MR05 / MR07 / MR06 / MR08 / MR12 | missed (all use symmetric src) | missed |
| **MR01 (rotate-90 on asymmetric)** | **detected** (k_src=1.33 → k_flw=0.54) | **detected** (k_src=1.32 → k_flw=0.96) |

Cohen's κ for the (Mut39, Mut40) pair on MR01: **1.000** (both
solvers detect). Detection rate of MR01 in the matrix is currently
1/1 = 100% on each solver — the only mutations targeting it are these
two by construction. Future runner / solver mutations that violate
geometric invariance will land here too.

## Symmetry interaction note (Mut05)

A non-obvious result worth flagging: Mut05 (`runner-chi-swap-groups`,
which reverses chi inside the runner) is **not** detected by the MR04
group-permute MR even though it conceptually breaks chi handling.
Why: Mut05 reverses chi inside the runner *unconditionally*, and the
MR04 follow-up *also* reverses chi (along with all other per-group
arrays). The two reversals compose to identity. Mut05 + MR04 yields a
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
~0.95. The lone `uncertain` is MR10 (`conv-num-azim-refine`) —
correctly flagging that "tracking refinement reduces |k_eff − k_true|"
is true asymptotically but the SUT does not expose `k_true`.

### Adversarial probe (`tools/noether_filter_calibration.py`)

The 100% survival rate above raised a fair question: is the filter
actually discriminating, or rubber-stamping? Phase 2 ships five
deliberately-bogus MR candidates in `tools/noether_adversarial.py`
and runs them through the same prompt:

| id | flaw | verdict | confidence |
|----|------|---------|-----------:|
| Adv01-mono-direction-inverted | wrong direction (less production → more k) | valid ⚠ | 0.95 |
| Adv02-cmp-impossible-budget | 1e-10 budget vs ~1e-3 MC noise | **invalid** | 0.90 |
| Adv03-conv-direction-inverted | coarser tracking → better convergence | **invalid** | 0.98 |
| Adv04-inv-vacuous-identity | `x ← x` as a "symmetry" | valid ⚠ | 0.80 |
| Adv05-fabricated-cmp | thread-count inequality (no physics basis) | **invalid** | 0.95 |

Three out of five rejected outright — Adv02, Adv03, Adv05 — with
confident, correctly-targeted reasoning. The two surviving "valid"
verdicts are nuanced:

* **Adv01** — the model's reasoning text explicitly notes "scaling
  nu_sigma_f down reduces neutron production, so k_eff must
  decrease," i.e. it DETECTED the inverted direction. It marked the
  candidate `valid` only because the prompt explicitly allows the
  model to return `valid` with a corrected `suggested_assertion` on
  direction-inverted candidates. **Prompt-designed redirection**, not
  rubber-stamping.
* **Adv04** — vacuous identity slips past as `valid` with mildly
  lower confidence (0.80). **Genuine blind spot**: the prompt
  validates MRs by physical correctness, not by fault-detection
  power. The same blind spot explains why the real-catalogue
  MR01-MR03 (mirror/rotation on the symmetric pin-cell) were
  accepted as `valid` — they are physically correct, just vacuous
  on this reference geometry. Phase-2 P2a added an asymmetric
  pin-cell sample so MR01 is no longer vacuous (see § MR01 above);
  MR02/MR03 still are.

Effective rejection rate: **3–4 / 5 = 60-80%**, depending on whether
Adv01 counts as a catch. The 100% real-catalogue survival is
therefore evidence of catalogue quality rather than filter
rubber-stamping — but the **vacuous-but-true blind spot** is now a
documented known weakness. Future iterations should add a
"realizable on the reference SUT in a non-trivial way" criterion to
the rubric.

Calibration report: [`calibration-report.md`](calibration-report.md).
Raw verdicts: `_data/noether/calibration-verdicts.json`.

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
   code changes. This unblocks variance-ratio validation for MR12
   (the only assertion that requires non-zero σ).
2. **MR14 (cross-program OpenMOC vs OpenMC)** — already partially
   covered by `tools/cross_program_comparison.py`; folding it into
   the matrix harness as an MR cell is a small refactor, deferred.
3. **MR02/MR03 vacuous on the centred pin-cell** — even with the new
   asymmetric extents, the centred fuel makes mirror x/y trivially
   identity. Activating them needs an off-centre fuel placement
   (new `fuel_offset_cm` field in the schema + matching runner
   changes). MR01 is now active (see "MR01 on an asymmetric pin-cell"
   above); MR02/MR03 remain deferred.
4. **MR15 (P0 vs P1 scattering)** — runner does not expose the
   scattering order; `realizable_with_current_sut=False` in the
   catalogue.

## Hand-off to Phase 3 (or next sprint)

Concrete priorities, in order:

1. **OpenMC matrix run** — populate the seven `openmc-pincell-*` rows.
   Cohen's κ on the matched-pair index will then have non-degenerate
   data for the new scenarios; the MR12 variance-ratio MR can finally
   be empirically tested (it requires non-zero σ which OpenMOC does
   not produce). Concretely: install OpenMC into a venv at
   `/opt/miniconda3/envs/openmc-env/bin/python` (or set
   `OPENMC_PYTHON`), then re-run
   `tools/mutation_study.py matrix --force --all`.
2. **Cohen's κ on the new MRs** — once OpenMC is back, the matched-pair
   index in `mutation_study.py` should grow new entries for the
   Phase-2 mutations (Mut28/M? for MR04, Mut32/M? for MR06, Mut33/M? for MR08).
   Today's commit only has OpenMOC-side mutations for the new MRs;
   matched OpenMC twins should be added.
3. **MR14 as a first-class MR** — refactor
   `cross_program_comparison.py` to write per-cell results into the
   matrix data dir. This makes cross-solver agreement reportable
   alongside per-solver detection rates.
4. **Asymmetric pin-cell test case** — one new entry under
   `SUT/openmoc/sample/` that breaks the C4 symmetry of the reference
   `pincell.json`, so MR01-MR03 stop being identity transformations and
   start exercising the rotation / reflection invariants.
5. **Per-MR factor sweep** — MR05 and MR08 detection rates depend
   strongly on the chosen factor (MR05 uses 1.5, dominating the
   absorption physics; MR08 uses 1.05, deliberately small). A factor
   sweep would quantify the rate-vs-coverage trade-off and could
   surface mutations that survive the canonical factor but flip at a
   nearby one.
