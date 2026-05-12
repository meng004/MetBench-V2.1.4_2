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
3. **Three new MR scenarios** wired into the existing matrix harness:
   - `openmoc-pincell-group-permute` (N04, `m_inv`, assertion=`approx`)
   - `openmoc-pincell-fuel-sigma-t` (N05, `m_mono`, assertion=`less`)
   - `openmoc-pincell-moderator-sigma-a` (N07, `m_mono`, assertion=`less`)
   Plus their OpenMC mirrors (deferred — see "Deferred work" below).
4. **Four new mutations** (M28-M31) **specifically designed to break
   the new MRs' algebraic invariants**:
   - M28 (`runner-chi-fast-only`) — hard-codes chi=[1,0]; breaks N04.
   - M29 (`adapter-fuel-sigt-no-siga-update`) — JSON inconsistency.
   - M30 (`adapter-moderator-sigma-a-no-sigt-update`) — breaks N07 on
     OpenMOC because OpenMOC reads moderator absorption from sigma_t.
   - M31 (`adapter-group-permute-fuel-only`) — half-permutes only fuel;
     breaks N04.

## Headline Phase-2 result

**Three Phase-1-invisible mutations are caught by Phase-2 MRs**:

| Mutation | Phase-1 outcome | Phase-2 outcome | New detector |
|----------|-----------------|-----------------|--------------|
| M28 (chi-fast-only) | missed by both `nu-sigma-f` and `sigma-a` | **detected** | `group-permute` (N04) |
| M30 (moderator sa, no sigt) | not affecting | **detected** | `moderator-sigma-a` (N07) |
| M31 (group-permute fuel only) | not affecting | **detected** | `group-permute` (N04) |

M28 is the headline: a chi hard-coding bug is *invisible* to every
monotonicity-style MR (Phase 1's `nu-sigma-f` and `sigma-a` MRs both
miss it because the multiplicative scaling cancels through the chi
term). Only the symmetry MR (N04 group permutation) detects it,
because the permuted JSON has chi=[0,1] yet the runner still emits
into group 0. This is the canonical NOETHER use case from the paper:
a different MetaPattern catches what monotonicity cannot.

M00 (identity) detects on **0/10** scenarios — false-positive control
holds across the expanded MR set.

## Per-MR detection rates (OpenMOC subset)

| MR scenario | n affected semantic mutants | detected | rate |
|-------------|----------------------------|----------|------|
| `openmoc-pincell-nu-sigma-f` (Phase 1) | 12 | 7 | 58.3% |
| `openmoc-pincell-sigma-a` (Phase 1) | 10 | 4 | 40.0% |
| `openmoc-pincell-group-permute` (N04) | 8 | 4 | **50.0%** |
| `openmoc-pincell-fuel-sigma-t` (N05) | 7 | 1 | 14.3% |
| `openmoc-pincell-moderator-sigma-a` (N07) | 8 | 3 | 37.5% |

Direct comparison of the rates is misleading — the denominator is the
set of *affected* mutations, which differs across MRs. The right
question is "does adding a new MR convert any previously-missed
mutation into `detected`?" — and yes, three of them.

The low rate of N05 (`fuel-sigma-t`, 14.3%) deserves a note: scaling
fuel sigma_t by 1.5 collapses k_eff from 1.13 to 0.11 because most
mutations leave the multiplicative dependence intact, so k_followup is
strictly less than k_source for almost everything. The MR is
therefore satisfied by most mutations (so they're "missed"). N05's
real value is in catching mutations that *invert* this monotonicity
direction (e.g., a future "scale wrong material" bug analogous to M14
but on sigma_t) rather than as a high-rate detector.

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

1. **OpenMC mirrors** of the new scenarios. The OpenMC adapters are
   already written (`openmc_input_adapter_group_permute.py`,
   `..._fuel_sigma_t.py`, `..._moderator_sigma_a.py`,
   `..._refine_particles.py`) and their scenarios are wired into
   `SCENARIOS`. The matrix run skipped them gracefully because this
   cloud session does not have OpenMC installed
   (`openmc_available()` returns False; cells emit
   `status=skipped-no-openmc`). Re-running on a session with the
   OpenMC venv (or with `METBENCH_FORCE_OPENMC=1`) populates the
   missing rows without code changes.
2. **N06 (fuel sigma_s monotonicity)** — adapter not implemented;
   requires careful handling of the 4-element scattering matrix.
3. **N08 (fuel radius monotonicity)** — adapter not implemented;
   trivial JSON edit but needs a small-perturbation factor (0.01-0.05)
   to stay in the under-moderated regime; deferred until we add
   per-MR factor overrides.
4. **N12 (Monte-Carlo σ scaling)** — OpenMC adapter
   (`openmc_input_adapter_refine_particles.py`) **is implemented**,
   but the matrix harness only checks k_eff comparisons; testing the
   1/√N scaling of `k_eff_std` requires extending `evaluate_mr` with
   a variance-ratio assertion. Listed for the next sprint.
5. **N14 (cross-program OpenMOC vs OpenMC)** — already partially
   covered by `tools/cross_program_comparison.py`; folding it into the
   matrix harness as an MR cell is a small refactor, deferred.
6. **N01-N03 vacuous-on-this-SUT note** — the LLM correctly marked
   these `valid` in the abstract, but on the symmetric centred
   pin-cell case they reduce to identity transformations
   (rotation/mirror produces the same JSON). Worth keeping in the
   catalogue for transparency, but they need an asymmetric pin-cell
   test case before they have any fault-detection power.

## Hand-off to Phase 3 (or next sprint)

Concrete priorities, in order:

1. **OpenMC matrix run** — populate the five `openmc-pincell-*` rows.
   Cohen's κ on the matched-pair index will then have non-degenerate
   data for the new scenarios.
2. **Variance-ratio assertion** — extend `evaluate_mr` with a
   `variance-scaling` assertion to land N12. This is the only Phase-2
   MR that doesn't fit the existing greater/less/approx vocabulary.
3. **N14 as a first-class MR** — refactor
   `cross_program_comparison.py` to write per-cell results into the
   matrix data dir. This makes cross-solver agreement reportable
   alongside per-solver detection rates.
4. **Asymmetric pin-cell test case** — one new entry under
   `SUT/openmoc/sample/` that breaks the C4 symmetry of the reference
   `pincell.json`, so N01-N03 stop being identity transformations and
   start exercising the rotation / reflection invariants.
