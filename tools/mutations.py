"""Mutation catalogue for Stage 5 Phase 1 (mutation-detection study).

Each entry is a :class:`Mutation` describing one source-level edit to apply
to the SUT files under `SUT/`. Patches are pure string substitutions so the
harness can apply them to a temp copy of `SUT/` without touching the
tracked tree.

Conventions
-----------

* `id` is `MNN-<solver>-<area>-<short>`. NN is zero-padded so the CSV
  sorts naturally.
* `target_file` is relative to the repository root.
* `apply(text)` must be a pure function returning a new string. If the
  substitution does not occur (e.g., the SUT file has been refactored
  out from under us), the function must raise. The harness treats that
  as an `error`, not a `missed`.
* `predicted_classification` is the author's pre-screening guess
  (`semantic` / `equivalent` / `solver-dependent`). It exists so the
  baseline-screening step can be **verified** rather than blindly
  trusted — the catalogue documents what we expected, the screening
  reports what actually happened.
* `predicted_detector` lists the MR family/families we expect to catch
  the mutant if it is in fact semantic. Empty list means "no MR is
  expected to catch this" (e.g., the identity control).
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Callable


@dataclass(frozen=True)
class Mutation:
    id: str
    target_file: str           # relative to repo root, e.g. "SUT/openmoc/openmoc_runner.py"
    description: str           # one-line summary
    rationale: str             # 1-3 lines on why this mutation is in the catalogue
    predicted_classification: str  # "semantic" | "equivalent" | "solver-dependent"
    predicted_detector: tuple[str, ...]  # subset of {"nu_sigma_f", "sigma_a"}, applies to both solvers' scenarios
    apply: Callable[[str], str]


def _replace_exactly_once(needle: str, replacement: str) -> Callable[[str], str]:
    """Return a patch function that asserts `needle` appears exactly once."""

    def patch(text: str) -> str:
        count = text.count(needle)
        if count != 1:
            raise RuntimeError(
                f"Patch precondition failed: expected exactly one occurrence "
                f"of {needle!r}, found {count}"
            )
        return text.replace(needle, replacement, 1)

    return patch


def _chain(*patches: Callable[[str], str]) -> Callable[[str], str]:
    def patch(text: str) -> str:
        for p in patches:
            text = p(text)
        return text

    return patch


# ---------------------------------------------------------------------------
# Identity control
# ---------------------------------------------------------------------------

Mut00 = Mutation(
    id="Mut00-identity",
    target_file="SUT/openmoc/openmoc_runner.py",  # arbitrary; not actually patched
    description="Identity (no change).",
    rationale="False-positive control. Any MR reporting `detected` on Mut00 is a bug "
              "in the MR or the harness, not in the SUT.",
    predicted_classification="equivalent",
    predicted_detector=(),
    apply=lambda text: text,
)

# ---------------------------------------------------------------------------
# OpenMOC runner mutations
# ---------------------------------------------------------------------------

Mut01 = Mutation(
    id="Mut01-openmoc-runner-chi-zero",
    target_file="SUT/openmoc/openmoc_runner.py",
    description="Zero out chi (fission spectrum) for every material.",
    rationale="With chi = 0 the fission source vanishes; k_eff collapses to ~0. "
              "An MR-suite should be insensitive to whether nu_sigma_f or sigma_a "
              "changed because the fission-spectrum bug kills *both* sides of the "
              "MR equally — useful gap check.",
    predicted_classification="semantic",
    predicted_detector=(),  # affects baseline; may not violate the MR ratio
    apply=_replace_exactly_once(
        'm.setChi(mat["chi"])',
        'm.setChi([0.0] * int(mat["num_groups"]))',
    ),
)

Mut02 = Mutation(
    id="Mut02-openmoc-runner-sigt-from-siga",
    target_file="SUT/openmoc/openmoc_runner.py",
    description='Pass mat["sigma_a"] to setSigmaT instead of mat["sigma_t"].',
    rationale="Realistic indexing slip: sigma_a < sigma_t by physics, so the runner "
              "would use a smaller total cross section, raising k_eff. The MR ratio "
              "for ScaleNuSigmaF should still hold qualitatively; this probes whether "
              "the MR is sensitive to absolute-scale corruption.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'm.setSigmaT(mat["sigma_t"])',
        'm.setSigmaT(mat["sigma_a"])',
    ),
)

Mut03 = Mutation(
    id="Mut03-openmoc-runner-swap-fuel-moderator",
    target_file="SUT/openmoc/openmoc_runner.py",
    description="Fill fuel cell with moderator material and vice versa.",
    rationale="Material/geometry swap — fuel sits where moderator should and vice "
              "versa. k_eff drops sharply because the fissile region has the wrong "
              "cross sections. Should trigger both MRs.",
    predicted_classification="semantic",
    predicted_detector=("nu_sigma_f", "sigma_a"),
    apply=_chain(
        _replace_exactly_once(
            'fuel_cell.setFill(fuel_mat)',
            'fuel_cell.setFill(mod_mat)',
        ),
        _replace_exactly_once(
            'mod_cell.setFill(mod_mat)',
            'mod_cell.setFill(fuel_mat)',
        ),
    ),
)

Mut04 = Mutation(
    id="Mut04-openmoc-runner-drop-nu-sigma-f",
    target_file="SUT/openmoc/openmoc_runner.py",
    description="Drop the setNuSigmaF call entirely.",
    rationale="Missing fission production cross section. k_eff drops drastically. "
              "Tests whether the MR-suite can spot a complete-zero fission scenario.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        '    m.setNuSigmaF(mat["nu_sigma_f"])\n',
        '    # m.setNuSigmaF(mat["nu_sigma_f"])  # MUTATION Mut04\n',
    ),
)

Mut05 = Mutation(
    id="Mut05-openmoc-runner-chi-swap-groups",
    target_file="SUT/openmoc/openmoc_runner.py",
    description="Replace setChi(mat['chi']) with setChi(reversed(mat['chi'])).",
    rationale="Fast/thermal swap of fission spectrum. Fuel chi = [1, 0] becomes "
              "[0, 1]: fission emits into the thermal group instead of the fast "
              "group, which is physically wrong and shifts k_eff measurably.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'm.setChi(mat["chi"])',
        'm.setChi(list(reversed(mat["chi"])))',
    ),
)

Mut06 = Mutation(
    id="Mut06-openmoc-runner-vacuum-boundary",
    target_file="SUT/openmoc/openmoc_runner.py",
    description="Change boundary type from REFLECTIVE to VACUUM.",
    rationale="Pin-cell with leakage. k_eff drops because neutrons escape instead "
              "of reflecting. MR ratios may still hold (same scaling applied to "
              "both source and follow-up) — useful coverage probe.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        's.setBoundaryType(openmoc.REFLECTIVE)',
        's.setBoundaryType(openmoc.VACUUM)',
    ),
)

# ---------------------------------------------------------------------------
# OpenMOC input adapter (ScaleNuSigmaF)
# ---------------------------------------------------------------------------

Mut07 = Mutation(
    id="Mut07-openmoc-adapter-nsf-inverse",
    target_file="SUT/openmoc/openmoc_input_adapter.py",
    description="Scale fuel.nu_sigma_f by 1/factor instead of factor.",
    rationale="Sign/direction inversion. The MR expects k_followup > k_source; "
              "with this bug k_followup < k_source. Should be detected by "
              "ScaleNuSigmaF (GreaterThan assertion fails).",
    predicted_classification="semantic",
    predicted_detector=("nu_sigma_f",),
    apply=_replace_exactly_once(
        'after = [v * factor for v in before]',
        'after = [v / factor for v in before]',
    ),
)

Mut08 = Mutation(
    id="Mut08-openmoc-adapter-nsf-square",
    target_file="SUT/openmoc/openmoc_input_adapter.py",
    description="Apply the scaling factor twice (factor**2).",
    rationale="Realistic copy-paste error: scaling line duplicated. k_followup is "
              "over-amplified. The GreaterThan assertion still passes (k_followup "
              "is way bigger), so the MR misses this fault — interesting gap.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'after = [v * factor for v in before]',
        'after = [v * factor * factor for v in before]',
    ),
)

Mut09 = Mutation(
    id="Mut09-openmoc-adapter-nsf-moderator",
    target_file="SUT/openmoc/openmoc_input_adapter.py",
    description="Scale moderator.nu_sigma_f instead of fuel.nu_sigma_f.",
    rationale="Moderator nu_sigma_f is exactly [0, 0] in the pin-cell case. "
              "Multiplying zero by any factor is a no-op, so this is an "
              "equivalent mutant by construction. Included to validate that "
              "baseline screening catches it.",
    predicted_classification="equivalent",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'fuel = case["materials"]["fuel"]',
        'fuel = case["materials"]["moderator"]',
    ),
)

Mut10 = Mutation(
    id="Mut10-openmoc-adapter-nsf-identity",
    target_file="SUT/openmoc/openmoc_input_adapter.py",
    description="Ignore factor; copy nu_sigma_f unchanged.",
    rationale="Bug: factor parsed but not applied. Source and follow-up are "
              "identical, so k_followup == k_source. The MR's GreaterThan "
              "assertion fails strictly — predicted detection.",
    predicted_classification="semantic",
    predicted_detector=("nu_sigma_f",),
    apply=_replace_exactly_once(
        'after = [v * factor for v in before]',
        'after = [v for v in before]',
    ),
)

Mut11 = Mutation(
    id="Mut11-openmoc-adapter-nsf-fast-only",
    target_file="SUT/openmoc/openmoc_input_adapter.py",
    description="Scale only the fast-group nu_sigma_f; leave thermal group untouched.",
    rationale="Partial scaling. k_followup increases but less than expected. "
              "MR still sees k_followup > k_source (assertion passes), so this "
              "fault is missed — another coverage-gap probe.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'after = [v * factor for v in before]',
        'after = [before[0] * factor, before[1]]',
    ),
)

# ---------------------------------------------------------------------------
# OpenMOC input adapter (ScaleFuelSigmaA)
# ---------------------------------------------------------------------------

Mut12 = Mutation(
    id="Mut12-openmoc-adapter-sa-no-sigt-update",
    target_file="SUT/openmoc/openmoc_input_adapter_sigma_a.py",
    description="Update fuel.sigma_a but leave sigma_t unchanged.",
    rationale="Inconsistent input. OpenMOC reads sigma_t directly (sigma_a is "
              "derived), so for the OpenMOC scenario this is an equivalent mutant. "
              "OpenMC reads both, so the inconsistency surfaces there. Tests "
              "cross-solver behaviour on the same patch.",
    predicted_classification="solver-dependent",
    predicted_detector=("sigma_a",),
    apply=_replace_exactly_once(
        'fuel["sigma_t"] = new_sigma_t',
        '# fuel["sigma_t"] = new_sigma_t  # MUTATION Mut12',
    ),
)

Mut13 = Mutation(
    id="Mut13-openmoc-adapter-sa-inverse",
    target_file="SUT/openmoc/openmoc_input_adapter_sigma_a.py",
    description="Scale fuel.sigma_a by 1/factor instead of factor.",
    rationale="Direction inversion. With factor > 1 the bug *decreases* "
              "absorption, so k_followup > k_source, but the assertion is "
              "LessThan. Predicted detection.",
    predicted_classification="semantic",
    predicted_detector=("sigma_a",),
    apply=_chain(
        _replace_exactly_once(
            'new_sigma_a = [a * factor for a in old_sigma_a]',
            'new_sigma_a = [a / factor for a in old_sigma_a]',
        ),
        _replace_exactly_once(
            'delta_t = [(factor - 1.0) * a for a in old_sigma_a]',
            'delta_t = [(1.0 / factor - 1.0) * a for a in old_sigma_a]',
        ),
    ),
)

Mut14 = Mutation(
    id="Mut14-openmoc-adapter-sa-moderator",
    target_file="SUT/openmoc/openmoc_input_adapter_sigma_a.py",
    description="Scale moderator.sigma_a (and moderator.sigma_t) instead of fuel.",
    rationale="Moderator sigma_a is non-zero (unlike its nu_sigma_f). Scaling "
              "moderator absorption by 1.5 should still measurably depress k_eff. "
              "Whether the MR detects this depends on whether the MR allows for "
              "moderator-side perturbation; under the strict ScaleFuelSigmaA "
              "definition this is a mis-targeted fault, not an equivalent.",
    predicted_classification="semantic",
    predicted_detector=("sigma_a",),
    apply=_replace_exactly_once(
        'fuel = case["materials"]["fuel"]',
        'fuel = case["materials"]["moderator"]',
    ),
)

# ---------------------------------------------------------------------------
# OpenMC runner mutations
# ---------------------------------------------------------------------------

Mut15 = Mutation(
    id="Mut15-openmc-runner-chi-zero",
    target_file="SUT/openmc/openmc_runner.py",
    description="Zero out chi for every material in the MGXS library.",
    rationale="OpenMC mirror of Mut01. Same logic: kills fission source. Expected "
              "to drive baseline k_eff toward zero.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'xsdata.set_chi(np.array(mat["chi"], dtype=np.float64))',
        'xsdata.set_chi(np.zeros(n, dtype=np.float64))',
    ),
)

Mut16 = Mutation(
    id="Mut16-openmc-runner-scatter-transpose",
    target_file="SUT/openmc/openmc_runner.py",
    description="Transpose the scatter matrix (swap g_in and g_out axes).",
    rationale="Scattering matrix S[g_in, g_out] becomes S[g_out, g_in]. Up- and "
              "down-scattering swap places, which fundamentally changes the "
              "thermal spectrum. Realistic indexing bug. Expected baseline shift.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'scatter_matrix = sig_s_flat.reshape((n, n, 1))',
        'scatter_matrix = sig_s_flat.reshape((n, n)).T.reshape((n, n, 1))',
    ),
)

Mut17 = Mutation(
    id="Mut17-openmc-runner-vacuum-boundary",
    target_file="SUT/openmc/openmc_runner.py",
    description="Change boundary_type from reflective to vacuum on all four planes.",
    rationale="OpenMC mirror of Mut06. Neutrons escape; k_eff drops. Useful to "
              "compare against the OpenMOC reflective→vacuum baseline shift.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=lambda text: text.replace('boundary_type="reflective"', 'boundary_type="vacuum"'),
)

Mut18 = Mutation(
    id="Mut18-openmc-runner-batches-too-few",
    target_file="SUT/openmc/openmc_runner.py",
    description="Hard-code batches=5, inactive=2, particles=200 (very noisy MC).",
    rationale="Massive statistical-noise injection. Tests whether the MR's "
              "GreaterThan/LessThan assertion is robust to MC noise at low "
              "particle counts.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_chain(
        _replace_exactly_once(
            'settings.batches = int(sv.get("batches", 60))',
            'settings.batches = 5',
        ),
        _replace_exactly_once(
            'settings.inactive = int(sv.get("inactive", 20))',
            'settings.inactive = 2',
        ),
        _replace_exactly_once(
            'settings.particles = int(sv.get("particles", 5000))',
            'settings.particles = 200',
        ),
    ),
)

Mut19 = Mutation(
    id="Mut19-openmc-runner-hardcode-keff",
    target_file="SUT/openmc/openmc_runner.py",
    description="Hardcode k_mean = 1.0 regardless of statepoint contents.",
    rationale="Worst-case bug: solver bypassed. k_eff identical across source "
              "and follow-up; both MRs' strict assertions fail. Easy detection.",
    predicted_classification="semantic",
    predicted_detector=("nu_sigma_f", "sigma_a"),
    apply=_replace_exactly_once(
        'k_mean = float(k.nominal_value)',
        'k_mean = 1.0  # MUTATION Mut19',
    ),
)

Mut20 = Mutation(
    id="Mut20-openmc-runner-chi-swap-groups",
    target_file="SUT/openmc/openmc_runner.py",
    description="Reverse chi array before passing to set_chi.",
    rationale="OpenMC mirror of Mut05. Fast-to-thermal fission shift.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'xsdata.set_chi(np.array(mat["chi"], dtype=np.float64))',
        'xsdata.set_chi(np.array(list(reversed(mat["chi"])), dtype=np.float64))',
    ),
)

Mut21 = Mutation(
    id="Mut21-openmc-runner-fission-zero",
    target_file="SUT/openmc/openmc_runner.py",
    description="Zero out fission cross section but keep nu_sigma_f.",
    rationale="Inconsistent fission data. OpenMC may warn / refuse / silently "
              "proceed; documents observed behaviour either way.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'xsdata.set_fission(np.array(mat["sigma_f"], dtype=np.float64))',
        'xsdata.set_fission(np.zeros(n, dtype=np.float64))',
    ),
)

# ---------------------------------------------------------------------------
# OpenMC input adapters (mirror of OpenMOC ones for cross-solver matched pairs)
# ---------------------------------------------------------------------------

Mut22 = Mutation(
    id="Mut22-openmc-adapter-nsf-inverse",
    target_file="SUT/openmc/openmc_input_adapter.py",
    description="Scale fuel.nu_sigma_f by 1/factor (OpenMC twin of Mut07).",
    rationale="Matched pair with Mut07. Used for cross-solver Cohen's κ.",
    predicted_classification="semantic",
    predicted_detector=("nu_sigma_f",),
    apply=_replace_exactly_once(
        'new_nsf = [v * factor for v in old_nsf]',
        'new_nsf = [v / factor for v in old_nsf]',
    ),
)

Mut23 = Mutation(
    id="Mut23-openmc-adapter-nsf-square",
    target_file="SUT/openmc/openmc_input_adapter.py",
    description="Apply factor twice (OpenMC twin of Mut08).",
    rationale="Matched pair with Mut08.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'new_nsf = [v * factor for v in old_nsf]',
        'new_nsf = [v * factor * factor for v in old_nsf]',
    ),
)

Mut24 = Mutation(
    id="Mut24-openmc-adapter-nsf-moderator",
    target_file="SUT/openmc/openmc_input_adapter.py",
    description="Scale moderator nu_sigma_f (OpenMC twin of Mut09).",
    rationale="Matched pair with Mut09; equivalent-mutant control on the OpenMC side.",
    predicted_classification="equivalent",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'fuel = case["materials"]["fuel"]',
        'fuel = case["materials"]["moderator"]',
    ),
)

Mut25 = Mutation(
    id="Mut25-openmc-adapter-nsf-identity",
    target_file="SUT/openmc/openmc_input_adapter.py",
    description="Ignore factor; copy unchanged (OpenMC twin of Mut10).",
    rationale="Matched pair with Mut10.",
    predicted_classification="semantic",
    predicted_detector=("nu_sigma_f",),
    apply=_replace_exactly_once(
        'new_nsf = [v * factor for v in old_nsf]',
        'new_nsf = [v for v in old_nsf]',
    ),
)

Mut26 = Mutation(
    id="Mut26-openmc-adapter-sa-no-sigt-update",
    target_file="SUT/openmc/openmc_input_adapter_sigma_a.py",
    description="Update fuel.sigma_a but leave sigma_t unchanged (OpenMC twin of Mut12).",
    rationale="Matched pair with Mut12. OpenMC twin is semantic (OpenMC reads "
              "sigma_t and sigma_a independently). Together with Mut12 this "
              "documents the cross-solver split clearly.",
    predicted_classification="semantic",
    predicted_detector=("sigma_a",),
    apply=_replace_exactly_once(
        'fuel["sigma_t"] = new_st',
        '# fuel["sigma_t"] = new_st  # MUTATION Mut26',
    ),
)

Mut27 = Mutation(
    id="Mut27-openmc-adapter-sa-inverse",
    target_file="SUT/openmc/openmc_input_adapter_sigma_a.py",
    description="Scale fuel.sigma_a by 1/factor (OpenMC twin of Mut13).",
    rationale="Matched pair with Mut13.",
    predicted_classification="semantic",
    predicted_detector=("sigma_a",),
    apply=_chain(
        _replace_exactly_once(
            'new_sa = [v * factor for v in old_sa]',
            'new_sa = [v / factor for v in old_sa]',
        ),
        _replace_exactly_once(
            'new_st = [old_st[g] + (factor - 1.0) * old_sa[g] for g in range(len(old_st))]',
            'new_st = [old_st[g] + (1.0 / factor - 1.0) * old_sa[g] for g in range(len(old_st))]',
        ),
    ),
)


# ---------------------------------------------------------------------------
# Phase 2 (NOETHER) mutations — designed to break the new MRs
#
# Mut28-Mut31 each targets the algebraic invariant of one new NOETHER MR. The
# expectation is that the *new* MR catches its target mutant while the
# Phase-1 MRs (ScaleNuSigmaF, ScaleFuelSigmaA) miss it. A successful
# matrix run will show new diagonal hits with off-diagonal misses,
# quantifying coverage growth.
# ---------------------------------------------------------------------------

Mut28 = Mutation(
    id="Mut28-openmoc-runner-chi-fast-only",
    target_file="SUT/openmoc/openmoc_runner.py",
    description='Hard-code chi to [1.0, 0.0] regardless of mat["chi"].',
    rationale="Hard-coded fast-only fission emission. Equivalent for the source case "
              "(reference fuel.chi == [1, 0] already), but breaks PermuteEnergyGroups "
              "(MR04): the permuted JSON has chi == [0, 1] yet the runner still emits "
              "into group 0. Phase-1 monotonicity MRs scale nu_sigma_f / sigma_a only, "
              "so they are insensitive to chi handling — this fault should be invisible "
              "to Phase 1 and visible to Phase 2 MR04.",
    predicted_classification="semantic",
    predicted_detector=("group_permute",),
    apply=_replace_exactly_once(
        'm.setChi(mat["chi"])',
        'm.setChi([1.0, 0.0])  # MUTATION Mut28',
    ),
)

Mut29 = Mutation(
    id="Mut29-openmoc-adapter-fuel-sigt-no-siga-update",
    target_file="SUT/openmoc/openmoc_input_adapter_fuel_sigma_t.py",
    description="Update fuel.sigma_t but skip the matching fuel.sigma_a bump.",
    rationale="Adapter inconsistency analogous to Mut12 but for the new fuel-sigma_t MR. "
              "OpenMOC reads sigma_t directly and derives sigma_a from "
              "sigma_t − Σ sigma_s, so missing the JSON sigma_a write is silent on "
              "OpenMOC: the runner still sees the correct effective absorption. Pure "
              "documentation-vs-runtime split — predicted equivalent for OpenMOC and "
              "semantic only when a downstream consumer actually reads sigma_a from "
              "the JSON (none currently does, so this is in the catalogue mostly to "
              "document the parity with Mut12 — important when OpenMC support lands).",
    predicted_classification="equivalent",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'fuel["sigma_a"] = new_sigma_a',
        '# fuel["sigma_a"] = new_sigma_a  # MUTATION Mut29',
    ),
)

Mut30 = Mutation(
    id="Mut30-openmoc-adapter-moderator-sigma-a-no-sigt-update",
    target_file="SUT/openmoc/openmoc_input_adapter_moderator_sigma_a.py",
    description="Update moderator.sigma_a but skip the matching moderator.sigma_t bump.",
    rationale="Adapter inconsistency for the new moderator-sigma_a MR. Because OpenMOC "
              "derives moderator absorption from sigma_t (not from sigma_a in the JSON), "
              "the runner only sees the original moderator.sigma_t, so the follow-up "
              "k_eff is unchanged from the source — that violates "
              "ScaleModeratorSigmaA's Less assertion. Predicted detected by MR07 on "
              "OpenMOC; missed by Phase-1 MRs (which never touch moderator).",
    predicted_classification="semantic",
    predicted_detector=("moderator_sigma_a",),
    apply=_replace_exactly_once(
        'moderator["sigma_t"] = new_sigma_t',
        '# moderator["sigma_t"] = new_sigma_t  # MUTATION Mut30',
    ),
)

Mut31 = Mutation(
    id="Mut31-openmoc-adapter-group-permute-fuel-only",
    target_file="SUT/openmoc/openmoc_input_adapter_group_permute.py",
    description="Permute energy groups in fuel only; leave moderator unchanged.",
    rationale="Adapter does only half of the symmetry transformation. The follow-up "
              "JSON has fuel with groups 0<->1 but moderator unchanged — a physically "
              "meaningless mixed state. The runner produces a different k_eff than the "
              "source, violating PermuteEnergyGroups' approx assertion. Pure Phase-2 "
              "MR target; Phase-1 MRs do not exercise this adapter.",
    predicted_classification="semantic",
    predicted_detector=("group_permute",),
    apply=_replace_exactly_once(
        'materials["moderator"] = _permute_material(materials["moderator"])',
        '# materials["moderator"] = _permute_material(materials["moderator"])  # MUTATION Mut31',
    ),
)


# ---------------------------------------------------------------------------
# Phase 2 (NOETHER) extension: mutations targeting MR06, MR08, MR12 MRs
# ---------------------------------------------------------------------------

Mut32 = Mutation(
    id="Mut32-openmoc-adapter-fuel-sigma-s-identity",
    target_file="SUT/openmoc/openmoc_input_adapter_fuel_sigma_s.py",
    description="Ignore the factor — copy fuel.sigma_s unchanged.",
    rationale="Adapter no-op: classic 'forgot to apply factor' bug analogous to "
              "Mut10 (nsf-identity). The follow-up case is bit-equivalent to the "
              "source, so k_followup == k_source. The MR06 MR's strict 'less' "
              "assertion fails. Phase-1 MRs do not exercise this adapter; "
              "predicted detection by MR06 only.",
    predicted_classification="semantic",
    predicted_detector=("fuel_sigma_s",),
    apply=_chain(
        _replace_exactly_once(
            'new_sigma_s = [factor * v for v in old_sigma_s]',
            'new_sigma_s = list(old_sigma_s)  # MUTATION Mut32',
        ),
        # Also skip the sigma_t recomputation so the source case is byte-identical.
        _replace_exactly_once(
            '    fuel["sigma_t"] = new_sigma_t',
            '    # fuel["sigma_t"] = new_sigma_t  # MUTATION Mut32',
        ),
    ),
)

Mut33 = Mutation(
    id="Mut33-openmoc-adapter-fuel-radius-shrink",
    target_file="SUT/openmoc/openmoc_input_adapter_fuel_radius.py",
    description="Divide fuel_radius by factor instead of multiplying.",
    rationale="Direction inversion. Scenario factor is 1.05 (factor > 1), so "
              "the bug shrinks the fuel by ~5%. With less fuel volume the cell "
              "is more moderated and k_eff drops, violating MR08's 'greater' "
              "assertion. Predicted detection by MR08; invisible to all Phase-1 "
              "MRs because they do not touch geometry.",
    predicted_classification="semantic",
    predicted_detector=("fuel_radius",),
    apply=_replace_exactly_once(
        'new_radius = old_radius * factor',
        'new_radius = old_radius / factor  # MUTATION Mut33',
    ),
)

Mut34 = Mutation(
    id="Mut34-openmc-adapter-particles-no-op",
    target_file="SUT/openmc/openmc_input_adapter_refine_particles.py",
    description="Ignore the factor — leave solver.particles unchanged.",
    rationale="Adapter doesn't change particle count, so the follow-up MC run "
              "has the same statistical noise as the source. Observed σ ratio "
              "≈ 1.0, but MR12's variance-ratio assertion expects 1/√10 ≈ 0.316. "
              "Detected by MR12 (variance-ratio). Inert in deterministic OpenMOC; "
              "OpenMC-only mutation.",
    predicted_classification="semantic",
    predicted_detector=("particles_refine",),
    apply=_replace_exactly_once(
        'new_particles = max(1, int(round(old_particles * factor)))',
        'new_particles = old_particles  # MUTATION Mut34',
    ),
)


# ---------------------------------------------------------------------------
# OpenMC twins of Phase-2 mutations (matched pairs for cross-solver κ)
# ---------------------------------------------------------------------------

Mut35 = Mutation(
    id="Mut35-openmc-runner-chi-fast-only",
    target_file="SUT/openmc/openmc_runner.py",
    description="Hard-code chi to [1.0, 0.0] regardless of mat['chi']. OpenMC twin of Mut28.",
    rationale="OpenMC twin of Mut28. Same logic: chi[0]=1 hardcoded so fission "
              "always emits into group 0, regardless of the JSON. Permuted JSON "
              "(MR04) has chi=[0,1] but runner still emits to 0 → MR04 detects.",
    predicted_classification="semantic",
    predicted_detector=("group_permute",),
    apply=_replace_exactly_once(
        'xsdata.set_chi(np.array(mat["chi"], dtype=np.float64))',
        'xsdata.set_chi(np.array([1.0, 0.0], dtype=np.float64))  # MUTATION Mut35',
    ),
)

Mut36 = Mutation(
    id="Mut36-openmc-adapter-group-permute-fuel-only",
    target_file="SUT/openmc/openmc_input_adapter_group_permute.py",
    description="Permute groups in fuel only; leave moderator unchanged. OpenMC twin of Mut31.",
    rationale="OpenMC twin of Mut31. Half-permutes the JSON, producing a mixed "
              "state that violates MR04's approx assertion.",
    predicted_classification="semantic",
    predicted_detector=("group_permute",),
    apply=_replace_exactly_once(
        'materials["moderator"] = _permute_material(materials["moderator"])',
        '# materials["moderator"] = _permute_material(materials["moderator"])  # MUTATION Mut36',
    ),
)

Mut37 = Mutation(
    id="Mut37-openmc-adapter-fuel-sigma-s-identity",
    target_file="SUT/openmc/openmc_input_adapter_fuel_sigma_s.py",
    description="Ignore the factor — copy fuel.sigma_s unchanged. OpenMC twin of Mut32.",
    rationale="OpenMC twin of Mut32. Adapter no-op, so follow-up == source, "
              "violating MR06's strict less assertion.",
    predicted_classification="semantic",
    predicted_detector=("fuel_sigma_s",),
    apply=_chain(
        _replace_exactly_once(
            'new_sigma_s = [factor * v for v in old_sigma_s]',
            'new_sigma_s = list(old_sigma_s)  # MUTATION Mut37',
        ),
        _replace_exactly_once(
            '    fuel["sigma_t"] = new_sigma_t',
            '    # fuel["sigma_t"] = new_sigma_t  # MUTATION Mut37',
        ),
    ),
)

Mut39 = Mutation(
    id="Mut39-openmoc-runner-hardcode-y-from-x",
    target_file="SUT/openmoc/openmoc_runner.py",
    description='Use g["x_extent_cm"] for half_y instead of g["y_extent_cm"].',
    rationale="Hard-codes y-extent to x-extent — geometry is correct only when "
              "x == y. On the asymmetric pin-cell (1.0 × 1.5 cm) the runner "
              "constructs a 1.0 × 0.5 cell from the source JSON and a 1.5 × "
              "0.75 cell from the 90°-rotated JSON; k_eff differs noticeably "
              "between them, so MR01 (Rotate90) detects. Both source and "
              "follow-up of Phase-1 MRs use the symmetric sample, where this "
              "patch is silently equivalent — exactly the MR-coverage gap "
              "MR01 was designed to fill.",
    predicted_classification="semantic",
    predicted_detector=("rotate_90",),
    apply=_replace_exactly_once(
        'half_y = g["y_extent_cm"] / 2.0',
        'half_y = g["x_extent_cm"] / 2.0  # MUTATION Mut39',
    ),
)

Mut40 = Mutation(
    id="Mut40-openmc-runner-hardcode-y-from-x",
    target_file="SUT/openmc/openmc_runner.py",
    description='OpenMC twin of Mut39: half_y = g["x_extent_cm"] / 2.0.',
    rationale="OpenMC mirror of Mut39. Same logical bug, same MR01 detection "
              "prediction. Matched pair entry for cross-solver κ.",
    predicted_classification="semantic",
    predicted_detector=("rotate_90",),
    apply=_replace_exactly_once(
        'half_y = g["y_extent_cm"] / 2.0',
        'half_y = g["x_extent_cm"] / 2.0  # MUTATION Mut40',
    ),
)


# ---------------------------------------------------------------------------
# Earlier Phase-2 OpenMC twin (kept here for symmetric ordering)
# ---------------------------------------------------------------------------

Mut38 = Mutation(
    id="Mut38-openmc-adapter-fuel-radius-shrink",
    target_file="SUT/openmc/openmc_input_adapter_fuel_radius.py",
    description="Divide fuel_radius by factor instead of multiplying. OpenMC twin of Mut33.",
    rationale="OpenMC twin of Mut33. Direction inversion; shrinks the fuel, "
              "drops k_eff, violates MR08's greater assertion.",
    predicted_classification="semantic",
    predicted_detector=("fuel_radius",),
    apply=_replace_exactly_once(
        'new_radius = old_radius * factor',
        'new_radius = old_radius / factor  # MUTATION Mut38',
    ),
)


# ---------------------------------------------------------------------------
# Phase-2 mutations targeting MR02 / MR03 (mirror reflection invariance)
#
# The reference off-centre pin-cell has fuel_offset_y_cm = -0.05 and
# fuel_offset_x_cm = +0.10. clamp-y-positive replaces the runner's
# `y=...` argument with `y=max(0, ...)`, so the source (y=-0.05) sees
# fuel forced to y=0, while the MirrorX follow-up (y=+0.05) keeps fuel
# at +0.05 — the two cells differ physically and MR02 detects.
# Symmetrically for clamp-x-positive on MR03.
# ---------------------------------------------------------------------------

Mut41 = Mutation(
    id="Mut41-openmoc-runner-clamp-y-offset-positive",
    target_file="SUT/openmoc/openmoc_runner.py",
    description='Replace y=...fuel_offset_y_cm with y=max(0.0, ...) inside ZCylinder.',
    rationale="Asymmetric y-handling: negative offsets get clamped to 0, positive "
              "ones pass through. Silent equivalent on the symmetric pin-cell "
              "(no offset). On the off-centre sample, source (y=-0.05) sees "
              "fuel at y=0 but the MirrorX follow-up (y=+0.05) sees fuel at "
              "+0.05 — k_eff differs, MR02 detects. MR03 (mirror y) and "
              "rotation MR untouched.",
    predicted_classification="semantic",
    predicted_detector=("mirror_x",),
    apply=_replace_exactly_once(
        'y=float(g.get("fuel_offset_y_cm", 0.0)),',
        'y=max(0.0, float(g.get("fuel_offset_y_cm", 0.0))),  # MUTATION Mut41',
    ),
)

Mut42 = Mutation(
    id="Mut42-openmoc-runner-clamp-x-offset-positive",
    target_file="SUT/openmoc/openmoc_runner.py",
    description='Replace x=...fuel_offset_x_cm with x=max(0.0, ...) inside ZCylinder.',
    rationale="Symmetric of Mut41 across the y-axis: negative x-offsets clamped "
              "to 0, positive ones pass through. On the off-centre sample "
              "source (x=+0.10) is unchanged, but MirrorY follow-up (x=-0.10) "
              "becomes x=0 — k_eff differs, MR03 detects. MR02 untouched.",
    predicted_classification="semantic",
    predicted_detector=("mirror_y",),
    apply=_replace_exactly_once(
        'x=float(g.get("fuel_offset_x_cm", 0.0)),',
        'x=max(0.0, float(g.get("fuel_offset_x_cm", 0.0))),  # MUTATION Mut42',
    ),
)

Mut43 = Mutation(
    id="Mut43-openmc-runner-clamp-y-offset-positive",
    target_file="SUT/openmc/openmc_runner.py",
    description='OpenMC twin of Mut41 (y0=max(0.0, ...)).',
    rationale="OpenMC mirror of Mut41. Matched pair for cross-solver κ on MR02.",
    predicted_classification="semantic",
    predicted_detector=("mirror_x",),
    apply=_replace_exactly_once(
        'y0=float(g.get("fuel_offset_y_cm", 0.0)),',
        'y0=max(0.0, float(g.get("fuel_offset_y_cm", 0.0))),  # MUTATION Mut43',
    ),
)

Mut44 = Mutation(
    id="Mut44-openmc-runner-clamp-x-offset-positive",
    target_file="SUT/openmc/openmc_runner.py",
    description='OpenMC twin of Mut42 (x0=max(0.0, ...)).',
    rationale="OpenMC mirror of Mut42. Matched pair for cross-solver κ on MR03.",
    predicted_classification="semantic",
    predicted_detector=("mirror_y",),
    apply=_replace_exactly_once(
        'x0=float(g.get("fuel_offset_x_cm", 0.0)),',
        'x0=max(0.0, float(g.get("fuel_offset_x_cm", 0.0))),  # MUTATION Mut44',
    ),
)


# ---------------------------------------------------------------------------
# Phase-3 stub mutation: closes the historical-bug Case 2 coverage gap by
# exercising the temperature-plumbing code path. The bug pattern (Mut45)
# mirrors OpenMC PR #3712 — chained construction returns the wrong value
# (or here: the runner ignores the field entirely), so any MR that exercises
# the temperature parameter notices the bypass.
# ---------------------------------------------------------------------------

Mut45 = Mutation(
    id="Mut45-openmoc-runner-ignore-temperature",
    target_file="SUT/openmoc/openmoc_runner.py",
    description='Force doppler factor = 1.0 in _build_material (ignore mat["temperature_kelvin"]).',
    rationale="Plumbing bypass analogous to upstream OpenMC PR #3712 (chained "
              "Material.add_temperature returns None). Runner reads the "
              "temperature field from JSON but discards it, so source and "
              "follow-up of MR-T (RaiseFuelTemperature) run with the same "
              "factor=1.0 (no Doppler scaling) and produce identical k_eff. "
              "MR-T's `less` assertion fails strictly → DETECTED. All "
              "non-temperature MRs are unaffected (their adapters never "
              "touch temperature_kelvin).",
    predicted_classification="semantic",
    predicted_detector=("fuel_temperature",),
    apply=_replace_exactly_once(
        'doppler = _doppler_factor(t_kelvin)',
        'doppler = 1.0  # MUTATION Mut45 — plumbing bypass',
    ),
)

Mut46 = Mutation(
    id="Mut46-openmc-runner-ignore-temperature",
    target_file="SUT/openmc/openmc_runner.py",
    description='Force doppler = 1.0 in OpenMC MGXS-library construction.',
    rationale="OpenMC twin of Mut45. Same logical bug, same MR detection "
              "prediction. Matched-pair entry for MR-T cross-solver κ.",
    predicted_classification="semantic",
    predicted_detector=("fuel_temperature",),
    apply=_replace_exactly_once(
        'doppler = 1.0 + 0.05 * _math.log(t_kelvin / 600.0) if t_kelvin > 0 else 1.0',
        'doppler = 1.0  # MUTATION Mut46 — plumbing bypass',
    ),
)


ALL_MUTATIONS: tuple[Mutation, ...] = (
    Mut00,
    Mut01, Mut02, Mut03, Mut04, Mut05, Mut06,
    Mut07, Mut08, Mut09, Mut10, Mut11,
    Mut12, Mut13, Mut14,
    Mut15, Mut16, Mut17, Mut18, Mut19, Mut20, Mut21,
    Mut22, Mut23, Mut24, Mut25,
    Mut26, Mut27,
    Mut28, Mut29, Mut30, Mut31,
    Mut32, Mut33, Mut34,
    Mut35, Mut36, Mut37, Mut38,
    Mut39, Mut40,
    Mut41, Mut42, Mut43, Mut44,
    Mut45, Mut46,
)


def by_id(mid: str) -> Mutation:
    for m in ALL_MUTATIONS:
        if m.id == mid:
            return m
    raise KeyError(f"Unknown mutation id: {mid}")
