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

M00 = Mutation(
    id="M00-identity",
    target_file="SUT/openmoc/openmoc_runner.py",  # arbitrary; not actually patched
    description="Identity (no change).",
    rationale="False-positive control. Any MR reporting `detected` on M00 is a bug "
              "in the MR or the harness, not in the SUT.",
    predicted_classification="equivalent",
    predicted_detector=(),
    apply=lambda text: text,
)

# ---------------------------------------------------------------------------
# OpenMOC runner mutations
# ---------------------------------------------------------------------------

M01 = Mutation(
    id="M01-openmoc-runner-chi-zero",
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

M02 = Mutation(
    id="M02-openmoc-runner-sigt-from-siga",
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

M03 = Mutation(
    id="M03-openmoc-runner-swap-fuel-moderator",
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

M04 = Mutation(
    id="M04-openmoc-runner-drop-nu-sigma-f",
    target_file="SUT/openmoc/openmoc_runner.py",
    description="Drop the setNuSigmaF call entirely.",
    rationale="Missing fission production cross section. k_eff drops drastically. "
              "Tests whether the MR-suite can spot a complete-zero fission scenario.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        '    m.setNuSigmaF(mat["nu_sigma_f"])\n',
        '    # m.setNuSigmaF(mat["nu_sigma_f"])  # MUTATION M04\n',
    ),
)

M05 = Mutation(
    id="M05-openmoc-runner-chi-swap-groups",
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

M06 = Mutation(
    id="M06-openmoc-runner-vacuum-boundary",
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

M07 = Mutation(
    id="M07-openmoc-adapter-nsf-inverse",
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

M08 = Mutation(
    id="M08-openmoc-adapter-nsf-square",
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

M09 = Mutation(
    id="M09-openmoc-adapter-nsf-moderator",
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

M10 = Mutation(
    id="M10-openmoc-adapter-nsf-identity",
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

M11 = Mutation(
    id="M11-openmoc-adapter-nsf-fast-only",
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

M12 = Mutation(
    id="M12-openmoc-adapter-sa-no-sigt-update",
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
        '# fuel["sigma_t"] = new_sigma_t  # MUTATION M12',
    ),
)

M13 = Mutation(
    id="M13-openmoc-adapter-sa-inverse",
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

M14 = Mutation(
    id="M14-openmoc-adapter-sa-moderator",
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

M15 = Mutation(
    id="M15-openmc-runner-chi-zero",
    target_file="SUT/openmc/openmc_runner.py",
    description="Zero out chi for every material in the MGXS library.",
    rationale="OpenMC mirror of M01. Same logic: kills fission source. Expected "
              "to drive baseline k_eff toward zero.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'xsdata.set_chi(np.array(mat["chi"], dtype=np.float64))',
        'xsdata.set_chi(np.zeros(n, dtype=np.float64))',
    ),
)

M16 = Mutation(
    id="M16-openmc-runner-scatter-transpose",
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

M17 = Mutation(
    id="M17-openmc-runner-vacuum-boundary",
    target_file="SUT/openmc/openmc_runner.py",
    description="Change boundary_type from reflective to vacuum on all four planes.",
    rationale="OpenMC mirror of M06. Neutrons escape; k_eff drops. Useful to "
              "compare against the OpenMOC reflective→vacuum baseline shift.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=lambda text: text.replace('boundary_type="reflective"', 'boundary_type="vacuum"'),
)

M18 = Mutation(
    id="M18-openmc-runner-batches-too-few",
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

M19 = Mutation(
    id="M19-openmc-runner-hardcode-keff",
    target_file="SUT/openmc/openmc_runner.py",
    description="Hardcode k_mean = 1.0 regardless of statepoint contents.",
    rationale="Worst-case bug: solver bypassed. k_eff identical across source "
              "and follow-up; both MRs' strict assertions fail. Easy detection.",
    predicted_classification="semantic",
    predicted_detector=("nu_sigma_f", "sigma_a"),
    apply=_replace_exactly_once(
        'k_mean = float(k.nominal_value)',
        'k_mean = 1.0  # MUTATION M19',
    ),
)

M20 = Mutation(
    id="M20-openmc-runner-chi-swap-groups",
    target_file="SUT/openmc/openmc_runner.py",
    description="Reverse chi array before passing to set_chi.",
    rationale="OpenMC mirror of M05. Fast-to-thermal fission shift.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'xsdata.set_chi(np.array(mat["chi"], dtype=np.float64))',
        'xsdata.set_chi(np.array(list(reversed(mat["chi"])), dtype=np.float64))',
    ),
)

M21 = Mutation(
    id="M21-openmc-runner-fission-zero",
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

M22 = Mutation(
    id="M22-openmc-adapter-nsf-inverse",
    target_file="SUT/openmc/openmc_input_adapter.py",
    description="Scale fuel.nu_sigma_f by 1/factor (OpenMC twin of M07).",
    rationale="Matched pair with M07. Used for cross-solver Cohen's κ.",
    predicted_classification="semantic",
    predicted_detector=("nu_sigma_f",),
    apply=_replace_exactly_once(
        'new_nsf = [v * factor for v in old_nsf]',
        'new_nsf = [v / factor for v in old_nsf]',
    ),
)

M23 = Mutation(
    id="M23-openmc-adapter-nsf-square",
    target_file="SUT/openmc/openmc_input_adapter.py",
    description="Apply factor twice (OpenMC twin of M08).",
    rationale="Matched pair with M08.",
    predicted_classification="semantic",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'new_nsf = [v * factor for v in old_nsf]',
        'new_nsf = [v * factor * factor for v in old_nsf]',
    ),
)

M24 = Mutation(
    id="M24-openmc-adapter-nsf-moderator",
    target_file="SUT/openmc/openmc_input_adapter.py",
    description="Scale moderator nu_sigma_f (OpenMC twin of M09).",
    rationale="Matched pair with M09; equivalent-mutant control on the OpenMC side.",
    predicted_classification="equivalent",
    predicted_detector=(),
    apply=_replace_exactly_once(
        'fuel = case["materials"]["fuel"]',
        'fuel = case["materials"]["moderator"]',
    ),
)

M25 = Mutation(
    id="M25-openmc-adapter-nsf-identity",
    target_file="SUT/openmc/openmc_input_adapter.py",
    description="Ignore factor; copy unchanged (OpenMC twin of M10).",
    rationale="Matched pair with M10.",
    predicted_classification="semantic",
    predicted_detector=("nu_sigma_f",),
    apply=_replace_exactly_once(
        'new_nsf = [v * factor for v in old_nsf]',
        'new_nsf = [v for v in old_nsf]',
    ),
)

M26 = Mutation(
    id="M26-openmc-adapter-sa-no-sigt-update",
    target_file="SUT/openmc/openmc_input_adapter_sigma_a.py",
    description="Update fuel.sigma_a but leave sigma_t unchanged (OpenMC twin of M12).",
    rationale="Matched pair with M12. OpenMC twin is semantic (OpenMC reads "
              "sigma_t and sigma_a independently). Together with M12 this "
              "documents the cross-solver split clearly.",
    predicted_classification="semantic",
    predicted_detector=("sigma_a",),
    apply=_replace_exactly_once(
        'fuel["sigma_t"] = new_st',
        '# fuel["sigma_t"] = new_st  # MUTATION M26',
    ),
)

M27 = Mutation(
    id="M27-openmc-adapter-sa-inverse",
    target_file="SUT/openmc/openmc_input_adapter_sigma_a.py",
    description="Scale fuel.sigma_a by 1/factor (OpenMC twin of M13).",
    rationale="Matched pair with M13.",
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


ALL_MUTATIONS: tuple[Mutation, ...] = (
    M00,
    M01, M02, M03, M04, M05, M06,
    M07, M08, M09, M10, M11,
    M12, M13, M14,
    M15, M16, M17, M18, M19, M20, M21,
    M22, M23, M24, M25,
    M26, M27,
)


def by_id(mid: str) -> Mutation:
    for m in ALL_MUTATIONS:
        if m.id == mid:
            return m
    raise KeyError(f"Unknown mutation id: {mid}")
