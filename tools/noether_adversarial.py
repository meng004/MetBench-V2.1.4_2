"""Adversarial MR candidates for LLM-filter calibration.

The Phase-2 filter run produced 14 valid + 1 uncertain + 0 invalid
verdicts on the 15 real NOETHER candidates. That's a 100% survival
rate, which means the filter has demonstrated no rejection power —
it could be rubber-stamping anything we feed it.

This module ships five deliberately-bogus candidates that a domain
expert would reject (wrong direction, vacuous, impossible budget,
inverted convergence, fabricated invariant). A working filter
should mark each `invalid` with high confidence and pinpoint the
physical error in `reasoning`. The driver is `tools/noether_filter_calibration.py`,
which runs the filter prompt against these and emits a per-candidate
verdict + a calibration summary.

The IDs use the prefix `Adv` so they cannot be confused with real
NOETHER candidates (`MR01..MR15`) in the catalogue or the matrix.
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from noether_candidates import CandidateMR  # noqa: E402


ADVERSARIAL_CANDIDATES: tuple[CandidateMR, ...] = (
    CandidateMR(
        id="Adv01-mono-direction-inverted",
        meta_pattern="m_mono",
        block="O≤",
        parameter="fuel.nu_sigma_f",
        description="Scale fuel.nu_sigma_f DOWN by factor < 1.",
        hypothesis="k_eff_followup > k_eff_source (LESS production raises k)",
        assertion="greater",
        physical_rationale="Fewer fission neutrons per fission means more chain reactions — k_eff goes up.",
        realizable_with_current_sut=True,
    ),
    CandidateMR(
        id="Adv02-cmp-impossible-budget",
        meta_pattern="m_cmp",
        block="E*",
        parameter="solver.OpenMOC_vs_OpenMC",
        description="Run the same pincell case through OpenMOC and OpenMC; require agreement to within 1e-10.",
        hypothesis="|k_OpenMOC − k_OpenMC| < 1e-10",
        assertion="bound",
        physical_rationale="Two correct solvers must produce bit-identical eigenvalues regardless of any statistical or discretization noise — a robust correctness criterion.",
        realizable_with_current_sut=True,
    ),
    CandidateMR(
        id="Adv03-conv-direction-inverted",
        meta_pattern="m_conv",
        block="L*",
        parameter="tracking.num_azim",
        description="Coarsen OpenMOC tracking: num_azim 16 → 4 (a 4× reduction).",
        hypothesis="|k_eff_coarser − k_eff_continuum| < |k_eff_baseline − k_eff_continuum|",
        assertion="bound",
        physical_rationale="Reducing the number of azimuthal angles improves MOC discretization quality by Cauchy-style refinement.",
        realizable_with_current_sut=True,
    ),
    CandidateMR(
        id="Adv04-inv-vacuous-identity",
        meta_pattern="m_inv",
        block="G",
        parameter="materials.fuel.sigma_t",
        description="Replace every fuel.sigma_t entry with itself (x ← x).",
        hypothesis="k_eff_followup == k_eff_source",
        assertion="approx",
        physical_rationale="Identity transformation preserves the eigenvalue exactly — a fundamental MetaPattern symmetry on any solver.",
        realizable_with_current_sut=True,
    ),
    CandidateMR(
        id="Adv05-fabricated-cmp",
        meta_pattern="m_cmp",
        block="E*",
        parameter="solver.thread_count",
        description="Compare OpenMOC k_eff at 1 thread vs at 8 threads; require strict inequality k(1) > k(8).",
        hypothesis="k_eff(num_threads=1) > k_eff(num_threads=8) by at least 0.01",
        assertion="greater",
        physical_rationale="More threads cause numerical reduction-order differences that systematically depress k_eff in deterministic transport solvers.",
        realizable_with_current_sut=True,
    ),
)
