"""NOETHER MetaPattern × physics-parameter Cartesian candidate set.

Implements the upstream layer from Pham et al. (NOETHER paper, §3 + §5):
seven MetaPatterns derived from an operator-algebra decomposition. We
restrict the enumeration to the four blocks our 2D pin-cell + static
eigenvalue SUT exercises:

* `m_inv`  (B1 symmetry)         — rotation / reflection / group-relabelling
                                   invariants on (geometry, energy groups).
* `m_mono` (B2 order)            — parameter-monotonicity ∂k_eff/∂θ for
                                   θ ∈ {cross sections, geometry, …}.
* `m_conv` (B5 limit)            — k_eff convergence under discretisation
                                   refinement (num_azim → finer, particles
                                   → larger, etc.).
* `m_cmp`  (B7 method-comparison)— cross-solver / cross-order agreement.

Blocks dropped for this SUT:

* `m_rev`  (B4 time-reversal)     — dissipative steady-state system; empty.
* `m_dyn`  (B6 qualitative-dyn)   — no trajectories; static eigenvalue only.
* `m_adj`  (B3 self-adjoint)      — adjoint solve not wired into the runner;
                                   listed as `out_of_scope` so the catalogue
                                   is transparent rather than silently
                                   incomplete.

The downstream step (CONSTRUCT-MP in the paper) takes the Cartesian
product MetaPattern × {parameters / generators} to produce a candidate
MR family. Each candidate gets passed through an LLM-validation filter
(`tools/noether_llm_filter.py`) before going into the MT matrix.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Callable


@dataclass(frozen=True)
class CandidateMR:
    id: str
    meta_pattern: str          # m_inv | m_mono | m_conv | m_cmp
    block: str                 # "G" | "O≤" | "L*" | "E*"
    parameter: str             # short human-readable parameter name
    description: str           # one-line description of the transformation
    hypothesis: str            # the MR statement (k_followup vs k_source)
    assertion: str             # "greater" | "less" | "approx" | "bound"
    physical_rationale: str    # 1-3 sentences a domain expert would write
    realizable_with_current_sut: bool = True


# ---------------------------------------------------------------------------
# m_inv  (B1 symmetry) — invariance under geometric / energy-group group action
# ---------------------------------------------------------------------------

M_INV = (
    CandidateMR(
        id="MR01-inv-quarter-rotation-90",
        meta_pattern="m_inv",
        block="G",
        parameter="geometry.rotation.90deg",
        description="Rotate the 2D pin-cell geometry by 90° (swap x/y extents and rotate fuel-cylinder axes).",
        hypothesis="k_eff_followup == k_eff_source within tolerance",
        assertion="approx",
        physical_rationale="2D square pin-cell with reflective BCs and a centred circular fuel rod has discrete C4 rotational symmetry. Any genuine eigenvalue solver must commute with this rotation; the follow-up k_eff must equal the source k_eff modulo Monte-Carlo noise and tracking discretisation.",
    ),
    CandidateMR(
        id="MR02-inv-mirror-x",
        meta_pattern="m_inv",
        block="G",
        parameter="geometry.reflection.x",
        description="Mirror the geometry along the x-axis (y → -y).",
        hypothesis="k_eff_followup == k_eff_source within tolerance",
        assertion="approx",
        physical_rationale="Pin-cell + reflective BC + centred fuel is invariant under reflection across either axis. k_eff is a scalar invariant of the underlying operator, so it must be preserved bit-equivalently in a deterministic solver and within MC σ for a stochastic one.",
    ),
    CandidateMR(
        id="MR03-inv-mirror-y",
        meta_pattern="m_inv",
        block="G",
        parameter="geometry.reflection.y",
        description="Mirror the geometry along the y-axis (x → -x).",
        hypothesis="k_eff_followup == k_eff_source within tolerance",
        assertion="approx",
        physical_rationale="Same mirror-symmetry argument as MR02 with the orthogonal axis.",
    ),
    CandidateMR(
        id="MR04-inv-energy-group-relabel",
        meta_pattern="m_inv",
        block="G",
        parameter="materials.group_relabel",
        description="Permute the energy-group index simultaneously across all cross-section arrays (groups 0↔1).",
        hypothesis="k_eff_followup == k_eff_source within tolerance",
        assertion="approx",
        physical_rationale="The eigenvalue is independent of which integer index a developer assigned to each group, provided all per-group arrays (sigma_t, sigma_a, sigma_s, nu_sigma_f, sigma_f, chi) are permuted consistently. Detects bugs that hard-code group ordering rather than indexing by name.",
    ),
)

# ---------------------------------------------------------------------------
# m_mono  (B2 order) — parameter-monotonicity
# ---------------------------------------------------------------------------
# Phase 1 already covers two members: ScaleNuSigmaF (+) and ScaleFuelSigmaA (-).
# Add a few more parameters that should produce monotone-direction k_eff
# response under standard PWR physics.
# ---------------------------------------------------------------------------

M_MONO = (
    CandidateMR(
        id="MR05-mono-fuel-sigma-t-up",
        meta_pattern="m_mono",
        block="O≤",
        parameter="fuel.sigma_t",
        description="Scale fuel sigma_t by factor > 1 (and adjust sigma_s consistently to preserve scattering).",
        hypothesis="k_eff_followup < k_eff_source (more total xs ⇒ fewer fission chains)",
        assertion="less",
        physical_rationale="Total cross section being increased without proportional scattering increases effective absorption per path length; k_eff drops. PWR textbook monotonicity.",
    ),
    CandidateMR(
        id="MR06-mono-fuel-sigma-s-down",
        meta_pattern="m_mono",
        block="O≤",
        parameter="fuel.sigma_s",
        description="Scale fuel scattering matrix down by factor < 1.",
        hypothesis="k_eff_followup < k_eff_source",
        assertion="less",
        physical_rationale="Reducing scattering in fuel reduces the chance of slow-down to thermal where fission is most likely; k_eff drops.",
    ),
    CandidateMR(
        id="MR07-mono-moderator-sigma-a-up",
        meta_pattern="m_mono",
        block="O≤",
        parameter="moderator.sigma_a",
        description="Scale moderator sigma_a by factor > 1 (and adjust moderator.sigma_t consistently).",
        hypothesis="k_eff_followup < k_eff_source",
        assertion="less",
        physical_rationale="Increasing moderator absorption removes neutrons from the thermal flux that would otherwise drive fission; k_eff drops. Standard PWR poison response.",
    ),
    CandidateMR(
        id="MR08-mono-fuel-radius-up",
        meta_pattern="m_mono",
        block="O≤",
        parameter="geometry.fuel_radius_cm",
        description="Increase fuel_radius_cm by a small factor (with x/y extents fixed).",
        hypothesis="k_eff_followup > k_eff_source",
        assertion="greater",
        physical_rationale="Larger fuel volume fraction in a fixed-pitch pin-cell raises fission rate per unit volume; k_eff rises. Valid for small perturbations that don't change the moderator/fuel ratio enough to flip the usual under-moderated regime.",
    ),
    CandidateMR(
        id="MR09-mono-fuel-chi-thermal-up",
        meta_pattern="m_mono",
        block="O≤",
        parameter="fuel.chi[1]",
        description="Shift chi from [1,0] to [0.7, 0.3] (some fission neutrons emerge thermal).",
        hypothesis="k_eff_followup > k_eff_source",
        assertion="greater",
        physical_rationale="Emitting fission neutrons directly into the thermal group bypasses the moderation losses and accelerates the chain; k_eff rises. Non-physical for U-235 (which is ~99.99% fast-emission) but the monotonicity direction is well-defined.",
        realizable_with_current_sut=True,
    ),
)

# ---------------------------------------------------------------------------
# m_conv (B5 limit) — discretisation convergence
# ---------------------------------------------------------------------------

M_CONV = (
    CandidateMR(
        id="MR10-conv-num-azim-refine",
        meta_pattern="m_conv",
        block="L*",
        parameter="tracking.num_azim",
        description="Refine OpenMOC tracking azimuthal angle count: 16 → 64 (factor 4 finer).",
        hypothesis="|k_eff_refined − k_eff_coarse| < |k_eff_coarse − k_eff_truth| (Cauchy-style)",
        assertion="bound",
        physical_rationale="Method-of-characteristics discretisation error in OpenMOC scales as O(1/num_azim). Refining must move k_eff monotonically toward the continuum value. Realisable on OpenMOC only.",
    ),
    CandidateMR(
        id="MR11-conv-azim-spacing-refine",
        meta_pattern="m_conv",
        block="L*",
        parameter="tracking.azim_spacing_cm",
        description="Halve azim_spacing_cm: 0.05 → 0.025.",
        hypothesis="|Δk_eff| decreases as spacing halves",
        assertion="bound",
        physical_rationale="Track-spacing is the other half of MOC discretisation; standard Richardson-extrapolation argument.",
    ),
    CandidateMR(
        id="MR12-conv-particles-refine",
        meta_pattern="m_conv",
        block="L*",
        parameter="solver.particles",
        description="OpenMC: increase particles 5000 → 50000 (factor 10).",
        hypothesis="k_eff_std_followup / k_eff_std_source ≈ 1/sqrt(10) ≈ 0.316",
        assertion="approx",
        physical_rationale="Monte-Carlo statistical error scales as 1/√N. The reported k_eff standard deviation must shrink by √10 when N grows by 10. Direct test of the MC convergence law.",
    ),
    CandidateMR(
        id="MR13-conv-batches-refine",
        meta_pattern="m_conv",
        block="L*",
        parameter="solver.batches",
        description="OpenMC: increase active batches (batches − inactive) from 40 → 200.",
        hypothesis="k_eff_std_followup < k_eff_std_source",
        assertion="less",
        physical_rationale="Reported MC σ shrinks monotonically with number of active batches. Sanity check on the variance reduction reporting path.",
    ),
)

# ---------------------------------------------------------------------------
# m_cmp  (B7 method-comparison) — partial-order on solvers / methods
# ---------------------------------------------------------------------------

M_CMP = (
    CandidateMR(
        id="MR14-cmp-openmoc-vs-openmc",
        meta_pattern="m_cmp",
        block="E*",
        parameter="solver.OpenMOC_vs_OpenMC",
        description="Run the same case through both solvers and compare k_eff.",
        hypothesis="|k_OpenMOC − k_OpenMC| < 3·σ_MC + ε_MOC, where ε_MOC ≈ 0.005·k (MOC discretisation budget)",
        assertion="bound",
        physical_rationale="Two physically equivalent solvers (MOC deterministic vs MC Monte-Carlo) discretising the same operator must agree within the sum of their respective discretisation budgets. Generalises the existing `cross_program_comparison.py` to an MR.",
    ),
    CandidateMR(
        id="MR15-cmp-p0-vs-p1-scattering",
        meta_pattern="m_cmp",
        block="E*",
        parameter="solver.scattering_order",
        description="Compare OpenMC with xsdata.order=0 vs xsdata.order=1 (P1 anisotropic).",
        hypothesis="k_P0 ≥ k_P1 within budget (Bol-Alg-04 textbook result)",
        assertion="bound",
        physical_rationale="The paper (Table 3) cites this exact MR as Bol-Alg-04: 'P0 over-estimates k in H-systems'. P0 (isotropic) approximation typically gives a slightly higher k than P1 for moderated systems. Requires runner extension to plumb order through.",
        realizable_with_current_sut=False,
    ),
)


ALL_CANDIDATES: tuple[CandidateMR, ...] = M_INV + M_MONO + M_CONV + M_CMP


# ---------------------------------------------------------------------------
# Block taxonomy table (mirror of Section 3.9 of the paper, restricted to
# blocks this SUT exercises).
# ---------------------------------------------------------------------------

METAPATTERN_TABLE = {
    "m_inv":  {"block": "G",   "definition": "Group symmetry: P(g·x) = ρ(g)·P(x).",
               "applicable": True,  "notes": "Geometry C4 + reflections + energy-group permutation."},
    "m_mono": {"block": "O≤",  "definition": "Parameter-monotonicity: θ1 ≤ θ2 ⇒ P(θ1) ≤ P(θ2).",
               "applicable": True,  "notes": "Phase 1 covers ScaleNuSigmaF and ScaleFuelSigmaA; this catalogue adds 5 more."},
    "m_adj":  {"block": "T*",  "definition": "Self-adjoint: ⟨Lx,y⟩ = ⟨x,Ly⟩. Yields source/detector reciprocity.",
               "applicable": False, "notes": "Boltzmann transport IS self-adjoint under the adjoint inner product, but our runner does not wire up adjoint solves. Out of scope for Phase 2; revisit when adjoint mode is added to openmoc_runner.py."},
    "m_rev":  {"block": "T*_rev",  "definition": "Time-reversal.",
               "applicable": False, "notes": "Dissipative scattering breaks T-reversibility. Empty by Section 3.5."},
    "m_conv": {"block": "L*",  "definition": "Parametrised limit: P(θ) → P* as θ → θ*.",
               "applicable": True,  "notes": "Tracking refinement, particles → ∞, batches → ∞."},
    "m_dyn":  {"block": "D*",  "definition": "Qualitative-dynamics features of solution trajectories.",
               "applicable": False, "notes": "Steady-state eigenvalue: no trajectory in the time sense. Burnup or transient solvers would populate this; out of Phase 2 scope."},
    "m_cmp":  {"block": "E*",  "definition": "Method-comparison partial order M1 ⪯E M2 in a specified error norm.",
               "applicable": True,  "notes": "OpenMOC vs OpenMC; P0 vs P1; future MOC vs SN."},
    "m_rel":  {"block": "B*_rel", "definition": "Relational-equivalence over an idempotent semiring.",
               "applicable": False, "notes": "Empty: pin-cell physics has no idempotent-semiring rewriting structure."},
}


def realizable() -> list[CandidateMR]:
    return [c for c in ALL_CANDIDATES if c.realizable_with_current_sut]


if __name__ == "__main__":
    import json
    out = {
        "metapatterns": METAPATTERN_TABLE,
        "candidates": [
            {
                "id": c.id, "meta_pattern": c.meta_pattern, "block": c.block,
                "parameter": c.parameter, "description": c.description,
                "hypothesis": c.hypothesis, "assertion": c.assertion,
                "physical_rationale": c.physical_rationale,
                "realizable_with_current_sut": c.realizable_with_current_sut,
            }
            for c in ALL_CANDIDATES
        ],
    }
    print(json.dumps(out, indent=2))
