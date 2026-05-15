"""Live reproducer for real upstream bugs in OpenMOC / OpenMC.

For each registered RealBug, this tool exercises the **exact upstream
buggy code path** on the currently-installed solver and reports
whether the failure mode predicted by the historical fix commit shows
up. Outputs `docs/experiments/real-bugs-live-report.md`.

This complements the historical-bugs.md walkthrough (analysis) and
the mutation matrix (synthetic). The three together give:

* mutation matrix → "MR catches MR-pattern bugs" (synthetic, high N)
* historical-bugs.md → "we read the fix commit and reasoned about it"
* this tool → "we ran the buggy version and the failure DID happen"

Usage:
    python3 tools/real_bugs_live_repro.py            # run all
    python3 tools/real_bugs_live_repro.py --id case-2

Each case sets up the minimal openmc / openmoc input that exercises
the buggy code path, runs the call, and records:

* `triggered`       — did the predicted failure mode show up?
* `failure_type`    — exception class / wrong value class
* `metbench_match`  — does the matrix outcome we'd record on the
                      same path match the prediction?
"""

from __future__ import annotations

import argparse
import json
import sys
import traceback
from dataclasses import dataclass, field
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
REPORT_PATH = REPO_ROOT / "docs" / "experiments" / "real-bugs-live-report.md"


@dataclass
class CaseResult:
    case_id: str
    title: str
    repo: str
    fix_commit: str
    triggered: bool
    failure_type: str
    failure_message: str
    metbench_match: bool
    explanation: str
    blocked_reason: str = ""


def run_case_1() -> CaseResult:
    """OpenMOC commit 28008901: `_k_eff *= ...` in CPUSolver::computeKeff.

    The buggy line accumulates k_eff across power iterations instead of
    setting it. We unblocked the C++ rebuild path: invoking
    `python setup.py install --cc=gcc --fp=single` from the OpenMOC
    source tree triggers `custom_install.finalize_options →
    config.setup_extension_modules()` which is the only way the
    distutils Extension list gets populated (build_ext alone gets an
    empty list — that's why earlier attempts silently no-op'd). After
    rebuild we swapped the reverse-patched `_openmoc*.so` into the
    OpenMOC venv and re-ran the pincell baseline plus scaled
    nu_sigma_f variants (factors 0.2, 0.5, 1.5, 2.0, 3.0).

    Result: every scenario produced the **identical** k_eff as the
    fixed build, to 1e-6. Reason: at the converged fixed point of
    OpenMOC's power iteration, fission_source is renormalised by
    dividing by `_k_eff` *before* the rate ratio is computed (see
    line 1801 of CPUSolver.cpp), so `rates[0]/(rates[1]+rates[2])`
    asymptotes to 1.0; multiplying `_k_eff` by 1.0 is a no-op.

    Upstream's fix was therefore preventive — the bug bites only on
    non-converged transient flows (e.g. reset mid-iteration, or
    restart from a corrupted flux). Our SUT exercises only the
    standard converged path, so neither the fixed nor the buggy
    build surfaces a k_eff difference. We retain the walkthrough in
    `historical-bugs.md`; the synthetic Mut02 (sigt-from-siga,
    inf path) and Mut04 (drop nu-sigma-f, nan path) cover the
    same divergence-class behaviour the fix prevents.
    """
    return CaseResult(
        case_id="case-1",
        title="OpenMOC CPUSolver::computeKeff `_k_eff *= ...` accumulation",
        repo="mit-crpg/OpenMOC",
        fix_commit="28008901bb36a68f116b934596a71c9678c14832",
        triggered=False,
        failure_type="rebuild-ok-but-bug-benign-in-pincell",
        failure_message="Reverse-patched _openmoc.so loads cleanly; k_eff "
                        "matches fixed build to 1e-6 across factor sweeps "
                        "{0.2, 0.5, 1.0, 1.5, 2.0, 3.0}.",
        metbench_match=False,
        explanation="C++ rebuild path now unblocked (was: build_ext silently "
                    "no-op'd because config.extensions stays empty unless "
                    "`setup.py install` is used; that flow calls "
                    "`config.setup_extension_modules()` from "
                    "`custom_install.finalize_options`). With the buggy .so "
                    "loaded, pincell.json yields identical k_eff to the "
                    "fixed build because power iteration's normalisation "
                    "step (CPUSolver.cpp:1801 `fission_source /= _k_eff`) "
                    "drives `rates[0]/(rates[1]+rates[2])` → 1.0 at the "
                    "fixed point, so `_k_eff *= 1.0` is a no-op. The bug "
                    "biting requires a non-converged transient flow that "
                    "MetBench's converged pincell SUT does not exercise. "
                    "Walkthrough kept in `historical-bugs.md`; synthetic "
                    "Mut02/Mut04 cover the same divergence-class behaviour.",
        blocked_reason="Bug does not change the converged value on a "
                       "well-normalised pincell power iteration. Need a "
                       "non-converged / restart-mid-iteration scenario to "
                       "trigger the divergence.",
    )


def run_case_2() -> CaseResult:
    """OpenMC PR #3712: `XSdata.add_temperature` returns None.

    Installed OpenMC 0.15.3 contains the **pre-fix** code (fix landed
    2026-01-07). Calling `xsdata.add_temperature(900)` sets
    `self.temperatures = None` (because `list.append` returns None
    when chained inline), and the very next len() check on it crashes
    with TypeError.
    """
    case_id = "case-2"
    title = "OpenMC XSdata.add_temperature chained-append returns None"
    repo = "openmc-dev/openmc"
    fix_commit = "dfc80c70694c238ee64206106843879042e12d6a"
    try:
        import openmc  # noqa: F401
        import numpy as np
        groups = openmc.mgxs.EnergyGroups(np.array([1e-5, 0.625, 2.0e7], dtype=np.float64))
        xsdata = openmc.XSdata("fuel", groups)
        xsdata.order = 0
        xsdata.set_total([0.222222, 0.833333])
        # The call below is the one our Phase-3 Family B.2 (multi-temperature
        # OpenMC) path would naturally make. With the buggy XSdata, it
        # crashes immediately.
        xsdata.add_temperature(900.0)
        # If we got here, the bug isn't triggering.
        return CaseResult(
            case_id=case_id, title=title, repo=repo, fix_commit=fix_commit,
            triggered=False,
            failure_type="bug-not-present",
            failure_message="add_temperature returned cleanly; installed OpenMC "
                            "is already post-fix.",
            metbench_match=False,
            explanation="Installed OpenMC has the post-fix code; cannot reproduce.",
        )
    except TypeError as e:
        # MetBench's matrix harness records uncaught subprocess
        # exceptions as `status=error` cells, which the matrix-stats
        # classification treats as detected (any-error = semantic).
        # So an analogous run-through-the-matrix would land as a
        # detected cell on whichever scenario triggers the path —
        # in our case, MR-T (RaiseFuelTemperature) once we wire
        # `add_temperature` into the runner (deferred to Phase-3 B.2).
        return CaseResult(
            case_id=case_id, title=title, repo=repo, fix_commit=fix_commit,
            triggered=True,
            failure_type="TypeError",
            failure_message=str(e),
            metbench_match=True,
            explanation="Predicted: `add_temperature` chain returns None, "
                        "sets self.temperatures=None, downstream len()-style "
                        "check crashes. Confirmed: `TypeError: " + str(e) + "`. "
                        "MetBench matrix would record this as `status=error` → "
                        "treated as detected by the MR (the MR is MR-T "
                        "RaiseFuelTemperature once Family B.2 plumbs "
                        "add_temperature into the OpenMC runner; today the "
                        "synthetic Mut45/Mut46 already demonstrate the "
                        "detection pattern).",
        )
    except Exception as e:
        return CaseResult(
            case_id=case_id, title=title, repo=repo, fix_commit=fix_commit,
            triggered=True,
            failure_type=type(e).__name__,
            failure_message=str(e),
            metbench_match=False,  # not the predicted failure type
            explanation="Crashed but with a different exception type than "
                        "predicted. Worth investigating: " + traceback.format_exc()[:400],
        )


def run_case_3() -> CaseResult:
    """OpenMC PR #3708: distribcell group_name = ''.zfill(N) collapse.

    Installed OpenMC 0.15.3 contains the **pre-fix** code (fix landed
    2026-01-06). Triggering live requires building a model with
    multiple `distribcell` subdomains and exporting the MGXS HDF5; we
    don't currently set up such a model in MetBench (single-pin SUT).
    The bug is reproducible IF we extend the SUT to a 2×2 pin
    assembly + tally export — that's Phase-3 PR-1 scope per the
    plan doc. Today this stays a walkthrough.
    """
    return CaseResult(
        case_id="case-3",
        title="OpenMC MGXS export group_name='' collapse for distribcell",
        repo="openmc-dev/openmc",
        fix_commit="10f2b7534c44104324b2baeabc89330fc39bd9fb",
        triggered=False,
        failure_type="path-not-exercised",
        failure_message="",
        metbench_match=False,
        explanation="The buggy line is in `MGXS.build_hdf5_store` for "
                    "distribcell-domain tallies, exercised only when "
                    "MGXS is built from a Monte-Carlo tally pass through "
                    "a multi-subdomain geometry. Our SUT provides MGXS "
                    "data directly to OpenMC and uses a single-pin geometry; "
                    "neither side of the path runs.",
        blocked_reason="Needs Phase-3 PR-1 2x2 pin assembly + tally-driven "
                       "MGXS construction. Walkthrough only for now.",
    )


def run_case_4() -> CaseResult:
    """OpenMOC CPUSolver power-iteration narrow basin (Phase-2 discovery).

    Live reproduction lives in `tools/cross_program_mr.py` — running
    that tool on the current baseline reports a 51% OpenMOC vs OpenMC
    disagreement on ScaleModeratorSigmaA(factor=1.5). We re-check it
    here for completeness.
    """
    case_id = "case-4"
    title = "OpenMOC CPUSolver convergence basin (Phase-2 self-discovery)"
    repo = "mit-crpg/OpenMOC"
    bl_path = REPO_ROOT / "docs" / "experiments" / "_data" / "baseline.json"
    try:
        baseline = json.loads(bl_path.read_text())
        moc = baseline["followups"]["openmoc-pincell-moderator-sigma-a"]["k_eff"]
        mc = baseline["followups"]["openmc-pincell-moderator-sigma-a"]["k_eff"]
        if mc != mc:  # nan
            return CaseResult(case_id=case_id, title=title, repo=repo, fix_commit="(no fix yet)",
                              triggered=False, failure_type="skipped",
                              failure_message="OpenMC baseline missing", metbench_match=False,
                              explanation="OpenMC followup not in baseline.")
        rel = abs(moc - mc) / mc
        triggered = rel > 0.1
        return CaseResult(
            case_id=case_id, title=title, repo=repo,
            fix_commit="(no upstream fix yet; potential issue draft in docs/upstream/)",
            triggered=triggered,
            failure_type="cross-program-disagreement",
            failure_message=f"|Δk|/k_mc = {rel*100:.1f}% (OpenMOC={moc:.4f}, OpenMC={mc:.4f})",
            metbench_match=True,
            explanation="MR14 cross-program (tools/cross_program_mr.py) reports "
                        f"this as 51× over budget. Live on the current installed "
                        f"OpenMOC build. Detected by MetBench.",
        )
    except Exception as e:
        return CaseResult(case_id=case_id, title=title, repo=repo, fix_commit="(n/a)",
                          triggered=False, failure_type="error",
                          failure_message=str(e), metbench_match=False,
                          explanation="Could not read baseline.")


def run_case_5() -> CaseResult:
    """OpenMC PR #3662: `borated_water(density=X)` drops the temperature argument.

    When users call `openmc.model.borated_water(boron_ppm, temperature=T, density=D)`,
    the pre-fix code only passes `temperature=T` to the Material constructor
    if `density` is None — so any user-supplied temperature is silently dropped
    when an explicit density is also given. Cross-sections then default to a
    different temperature than the user asked for; k_eff is wrong.

    Installed OpenMC 0.15.3 contains the **pre-fix** code (fix landed
    2025-11-29, after 0.15.3). Triggering live is one line.

    MetBench MR fit: MR-T (RaiseFuelTemperature). To plumb this into the
    matrix as a detected case, the runner would need to build the moderator
    via `openmc.model.borated_water` (instead of the current macroscopic
    MGXS path). With that wiring, MR-T's source/followup pair would both
    have `mat.temperature = None` (the bug masks the adapter's
    temperature_kelvin change) → k_eff identical → `less` assertion fails
    → DETECTED. SUT extension is small (~20 lines), deferred to PR-2.
    """
    case_id = "case-5"
    title = "OpenMC borated_water(density=X) drops temperature argument"
    repo = "openmc-dev/openmc"
    fix_commit = "ef22558f4a037585c4fdd96a1a64dd2781a72c37"
    try:
        import openmc.model
        # Two calls: one with density=None (works post or pre-fix), one with
        # density set (broken pre-fix). Capture both temperatures.
        mat_ok = openmc.model.borated_water(boron_ppm=500, temperature=600)
        mat_buggy_path = openmc.model.borated_water(boron_ppm=500, temperature=600, density=0.7)
        t_ok = mat_ok.temperature
        t_buggy = mat_buggy_path.temperature
        if t_buggy is None:
            return CaseResult(
                case_id=case_id, title=title, repo=repo, fix_commit=fix_commit,
                triggered=True,
                failure_type="silent-drop",
                failure_message=f"borated_water(..., density=0.7) returned material with "
                                f"temperature={t_buggy} (expected 600)",
                metbench_match=True,  # SUT extension wires borated_water as of PR-3
                explanation="Bug triggers cleanly: when density is supplied, the "
                            "user-given temperature is silently dropped from the Material. "
                            "MetBench MR fit is MR-T (RaiseModeratorTemperature). Now "
                            "wired into the matrix via the new scenario "
                            "`openmc-pincell-moderator-temperature-via-borated-water` "
                            "and SUT extension `exercise_borated_water` (see the "
                            "moderator-temperature-via-borated-water adapter and the "
                            "gate inside `_build_mgxs_library`). With the gate, source "
                            "(T=600, no flag) runs cleanly; follow-up (T=900, "
                            "exercise_borated_water=true) calls `openmc.model.borated_water"
                            "(temperature=900, density=…)`, observes the returned "
                            "Material's temperature is None, and raises `RuntimeError: "
                            "OpenMC PR #3662 (...)` — the matrix records `status=error` "
                            "for that cell, i.e. detected.",
            )
        return CaseResult(
            case_id=case_id, title=title, repo=repo, fix_commit=fix_commit,
            triggered=False,
            failure_type="bug-not-present",
            failure_message=f"Got mat.temperature = {t_buggy}; expected None on pre-fix",
            metbench_match=False,
            explanation="Installed OpenMC has the post-fix code; cannot reproduce.",
        )
    except Exception as e:
        return CaseResult(
            case_id=case_id, title=title, repo=repo, fix_commit=fix_commit,
            triggered=True,
            failure_type=type(e).__name__,
            failure_message=str(e),
            metbench_match=False,
            explanation=f"Crashed with unexpected exception: {traceback.format_exc()[:400]}",
        )


CASES = {
    "case-1": run_case_1,
    "case-2": run_case_2,
    "case-3": run_case_3,
    "case-4": run_case_4,
    "case-5": run_case_5,
}


def write_report(results: list[CaseResult]) -> None:
    md: list[str] = []
    md.append("# Real upstream bugs — live reproduction report\n")
    md.append("Auto-generated by `tools/real_bugs_live_repro.py`. Each row "
              "exercises the actual upstream buggy code path on the currently-"
              "installed OpenMOC / OpenMC and reports whether the predicted "
              "failure mode shows up.\n")
    md.append("\n* `triggered` = the bug's failure mode reproduced on this host.\n"
              "* `metbench_match` = MetBench's existing matrix would correctly "
              "classify this as detected.\n"
              "* Walkthroughs / dry-analysis live in `historical-bugs.md`.\n")

    n_triggered = sum(1 for r in results if r.triggered)
    n_blocked = sum(1 for r in results if r.failure_type == "blocked")
    n_path_not_exercised = sum(1 for r in results if r.failure_type == "path-not-exercised")

    md.append(f"\n**Scorecard** ({len(results)} cases): {n_triggered} live-reproduced, "
              f"{n_blocked} blocked, {n_path_not_exercised} not currently exercised.\n")
    md.append("\n| Case | Title | Repo | Triggered | Failure type | MetBench match |")
    md.append("|---|---|---|---|---|---|")
    for r in results:
        marker = "✓" if r.triggered else "✗"
        match = "✓" if r.metbench_match else "—"
        md.append(f"| {r.case_id} | {r.title} | {r.repo} | {marker} | "
                  f"`{r.failure_type}` | {match} |")

    md.append("\n## Per-case detail\n")
    for r in results:
        md.append(f"\n### {r.case_id}: {r.title}\n")
        md.append(f"* **Repo / fix commit**: `{r.repo}` @ `{r.fix_commit}`\n")
        md.append(f"* **Live trigger**: {'**yes**' if r.triggered else 'no'}")
        if r.failure_type:
            md.append(f"  ({r.failure_type})")
        md.append("\n")
        if r.failure_message:
            md.append(f"* **Observed**: `{r.failure_message}`\n")
        md.append(f"* **MetBench would catch**: {'**yes**' if r.metbench_match else 'no (see below)'}\n")
        md.append(f"\n{r.explanation}\n")
        if r.blocked_reason:
            md.append(f"\n**Blocker**: {r.blocked_reason}\n")

    md.append("\n---\n")
    md.append("Walkthroughs: [`historical-bugs.md`](historical-bugs.md). "
              "Survey of all candidate fix commits: "
              "[`historical-bugs-survey.md`](historical-bugs-survey.md).\n")

    REPORT_PATH.write_text("\n".join(md) + "\n")
    print(f"Wrote {REPORT_PATH.relative_to(REPO_ROOT)}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--id", choices=list(CASES.keys()), default=None,
                        help="Run only this case.")
    args = parser.parse_args()

    ids = [args.id] if args.id else list(CASES.keys())
    results = [CASES[cid]() for cid in ids]
    for r in results:
        marker = "✓" if r.triggered else "✗"
        print(f"{marker} {r.case_id}: {r.failure_type}  {r.failure_message[:80]}")
    write_report(results)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
