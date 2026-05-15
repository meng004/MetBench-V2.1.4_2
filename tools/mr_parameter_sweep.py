"""Parameter sweep for sweepable MRs — produces 5+ sample points per MR.

For each MR scenario whose adapter takes a continuous `factor` parameter,
this tool runs the adapter at five+ sample factor values, records the
resulting k_eff (and k_eff_std for stochastic OpenMC), and evaluates
the assertion at each point. Output: `docs/experiments/mr-parameter-sweep.md`.

Goal: demonstrate that MetBench's MetaPattern-based MRs hold across a
range of parameter values (not just one cherry-picked factor), and
surface any factor-dependent solver pathologies along the way (similar
to how Phase-2 found the OpenMOC moderator-sigma-a basin at factor=1.5).

Sample points are chosen per-MR to cover a physically meaningful range
without crossing solver pathologies we already know about. See SWEEPS
dict for the choices and rationale.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO_ROOT / "tools"))
from mutation_study import (  # noqa: E402
    SCENARIOS, scenario_source, evaluate_mr, openmc_available,
)

REPORT_PATH = REPO_ROOT / "docs" / "experiments" / "mr-parameter-sweep.md"
DATA_PATH = REPO_ROOT / "docs" / "experiments" / "_data" / "mr-parameter-sweep.json"

OPENMOC_PYTHON = os.environ.get("OPENMOC_PYTHON", "/opt/openmoc-venv/bin/python")
OPENMC_PYTHON = os.environ.get("OPENMC_PYTHON", "/opt/miniconda3/envs/openmc-env/bin/python")

# Sample points per scenario id. Choices documented inline.
SWEEPS: dict[str, list[float]] = {
    # MR05 ScaleFuelSigmaT — increasing, "less" assertion. Range above
    # 1.0 (no effect) up to 2.0 (severely subcritical). Avoids the
    # OpenMOC pathology zone (the moderator-sigma-a basin doesn't apply
    # here since this scales fuel, not moderator).
    "openmoc-pincell-fuel-sigma-t":     [1.05, 1.10, 1.25, 1.50, 2.00],
    "openmc-pincell-fuel-sigma-t":      [1.05, 1.10, 1.25, 1.50, 2.00],

    # MR06 ScaleFuelSigmaS — decreasing (factor < 1), "less" assertion.
    # Sweep from "almost identity" (0.99) down to "severe under-scatter".
    "openmoc-pincell-fuel-sigma-s":     [0.99, 0.90, 0.75, 0.50, 0.25],
    "openmc-pincell-fuel-sigma-s":      [0.99, 0.90, 0.75, 0.50, 0.25],

    # MR07 ScaleModeratorSigmaA — careful: OpenMOC CPUSolver hits a
    # non-physical convergence basin at factor=1.5 (Case 4). Sweep
    # stays in the well-converged regime [1.05, 1.40].
    "openmoc-pincell-moderator-sigma-a": [1.05, 1.10, 1.20, 1.30, 1.40],
    "openmc-pincell-moderator-sigma-a":  [1.05, 1.20, 1.50, 1.80, 2.00],

    # MR08 ScaleFuelRadius — "greater" assertion. Sweep stays small to
    # avoid crossing the over-moderated regime where the monotonicity
    # sign flips, and to keep new_radius < half_extent.
    "openmoc-pincell-fuel-radius":      [1.01, 1.02, 1.05, 1.08, 1.10],
    "openmc-pincell-fuel-radius":       [1.01, 1.02, 1.05, 1.08, 1.10],

    # MR12 RefineParticles — variance-ratio, OpenMC-only.
    # 1/sqrt(factor) ∈ {1/√2, 1/2, 1/√10, 1/4, 1/5}. Stops at factor=25
    # (125k particles ≈ 2-3 min per run); factor=100 (500k particles)
    # would push the sweep past 10 min on a single point.
    "openmc-pincell-particles-refine":  [2.0, 4.0, 10.0, 16.0, 25.0],

    # MR-T RaiseFuelTemperature — "less" assertion (Doppler). Range
    # [1.10 → 2.00] gives T 660 K → 1200 K against T_ref 600 K.
    "openmoc-pincell-fuel-temperature": [1.10, 1.25, 1.50, 1.75, 2.00],
    "openmc-pincell-fuel-temperature":  [1.10, 1.25, 1.50, 1.75, 2.00],
}


def run_one_point(scenario: dict, factor: float, sc_source: Path) -> dict:
    """Apply adapter at `factor`, run runner, return cell-like dict."""
    py = OPENMOC_PYTHON if scenario["solver"] == "openmoc" else OPENMC_PYTHON
    adapter = REPO_ROOT / "SUT" / scenario["adapter"]
    runner = REPO_ROOT / "SUT" / scenario["runner"]
    with tempfile.TemporaryDirectory(prefix="sweep-") as tmp:
        flw_in = Path(tmp) / "flw-in.json"
        flw_out = Path(tmp) / "flw-out.json"
        try:
            subprocess.run(
                [py, str(adapter), "transform-input",
                 "--source-file", str(sc_source), "--output-file", str(flw_in),
                 "--params", json.dumps({"factor": str(factor)})],
                check=True, capture_output=True, timeout=120,
            )
            subprocess.run(
                [py, str(runner), "--input", str(flw_in), "--output", str(flw_out)],
                check=True, capture_output=True, timeout=300,
            )
        except subprocess.CalledProcessError as e:
            return {"factor": factor, "error": e.stderr.decode("utf-8", "replace")[:400]}
        except subprocess.TimeoutExpired:
            return {"factor": factor, "error": "timeout"}
        r = json.loads(flw_out.read_text())
        return {
            "factor": factor,
            "k_followup": float(r["k_eff"]),
            "k_followup_std": float(r.get("k_eff_std", 0.0)),
        }


def sweep_scenario(scenario: dict, source_k: float, source_sigma: float, points: list[float]) -> list[dict]:
    sc_source = scenario_source(scenario)
    cells: list[dict] = []
    for factor in points:
        cell = run_one_point(scenario, factor, sc_source)
        if "error" not in cell:
            cell["k_source"] = source_k
            cell["k_source_std"] = source_sigma
            cell["assertion"] = scenario["assertion"]
            # Honour scenario-level config that evaluate_mr reads.
            sc_for_eval = dict(scenario)
            if scenario["assertion"] == "variance-ratio":
                # Override target_ratio per sample point: 1/sqrt(factor)
                import math
                sc_for_eval["target_ratio"] = 1.0 / math.sqrt(factor)
            try:
                passed = evaluate_mr(cell, sc_for_eval)
                cell["passed"] = passed
                cell["outcome"] = "missed" if passed else "detected"
            except Exception as e:
                cell["passed"] = None
                cell["outcome"] = f"eval-error: {type(e).__name__}: {e}"
        cells.append(cell)
    return cells


def get_source_baseline(scenario: dict) -> tuple[float, float]:
    """Run unpatched source case to get k_source / sigma_source for the scenario's source_case."""
    py = OPENMOC_PYTHON if scenario["solver"] == "openmoc" else OPENMC_PYTHON
    runner = REPO_ROOT / "SUT" / scenario["runner"]
    sc_source = scenario_source(scenario)
    with tempfile.TemporaryDirectory(prefix="sweep-src-") as tmp:
        out = Path(tmp) / "src.json"
        subprocess.run(
            [py, str(runner), "--input", str(sc_source), "--output", str(out)],
            check=True, capture_output=True, timeout=300,
        )
        r = json.loads(out.read_text())
        return float(r["k_eff"]), float(r.get("k_eff_std", 0.0))


def main() -> int:
    has_openmc = openmc_available()
    results: dict = {"scenarios": {}}
    by_id = {sc["id"]: sc for sc in SCENARIOS}

    for sc_id, points in SWEEPS.items():
        sc = by_id.get(sc_id)
        if sc is None:
            print(f"  {sc_id}: not in SCENARIOS, skipping", file=sys.stderr)
            continue
        if sc["solver"] == "openmc" and not has_openmc:
            print(f"  {sc_id}: skipped (no OpenMC)", file=sys.stderr)
            results["scenarios"][sc_id] = {"skipped": True}
            continue
        print(f"  {sc_id}: source baseline ...", file=sys.stderr)
        try:
            src_k, src_sigma = get_source_baseline(sc)
        except Exception as e:
            print(f"    source baseline failed: {e}", file=sys.stderr)
            results["scenarios"][sc_id] = {"error": str(e)}
            continue
        print(f"    k_source={src_k:.5f} σ={src_sigma:.5f}", file=sys.stderr)
        cells = sweep_scenario(sc, src_k, src_sigma, points)
        results["scenarios"][sc_id] = {
            "solver": sc["solver"],
            "assertion": sc["assertion"],
            "noether_id": sc.get("noether_id", sc.get("transform", "?")),
            "meta_pattern": sc.get("meta_pattern", "?"),
            "k_source": src_k,
            "k_source_std": src_sigma,
            "cells": cells,
        }
        for cell in cells:
            if "error" in cell:
                print(f"    factor={cell['factor']}: ERROR {cell['error'][:60]}", file=sys.stderr)
            else:
                arrow = "DETECT" if cell["outcome"] == "detected" else "miss  "
                print(f"    factor={cell['factor']:>6.3f}: {arrow}  "
                      f"k_flw={cell['k_followup']:.5f}", file=sys.stderr)

    DATA_PATH.parent.mkdir(parents=True, exist_ok=True)
    DATA_PATH.write_text(json.dumps(results, indent=2) + "\n")
    print(f"\nWrote {DATA_PATH.relative_to(REPO_ROOT)}", file=sys.stderr)

    # Markdown report
    write_report(results)
    return 0


def write_report(results: dict) -> None:
    md: list[str] = []
    md.append("# MR parameter sweep — ≥5 sample points per sweepable MR\n")
    md.append("Auto-generated by `tools/mr_parameter_sweep.py`. For each MR\n"
              "whose adapter takes a continuous factor, the harness samples\n"
              "five values across a physically-meaningful range, runs the\n"
              "adapter+runner at each, evaluates the assertion, and tabulates\n"
              "the trajectory.\n")
    md.append("\nThe goal is to (a) show MRs hold across factor variation, not\n"
              "just at one cherry-picked point, and (b) surface factor-dependent\n"
              "solver pathologies (the way Phase-2 found the OpenMOC moderator-\n"
              "sigma-a basin at factor=1.5).\n")

    # Group by MetaPattern
    by_mp: dict[str, list] = {}
    for sc_id, s in results["scenarios"].items():
        if "cells" not in s:
            continue
        mp = s.get("meta_pattern", "?")
        by_mp.setdefault(mp, []).append((sc_id, s))

    for mp in sorted(by_mp):
        md.append(f"\n## MetaPattern `{mp}`\n")
        for sc_id, s in by_mp[mp]:
            md.append(f"\n### {sc_id} (MR {s.get('noether_id', '?')}, assertion={s['assertion']})\n")
            md.append(f"\nSource baseline: k = {s['k_source']:.5f}, σ = {s['k_source_std']:.5f}\n")
            md.append("\n| factor | k_followup | σ_followup | outcome | note |")
            md.append("|---:|---:|---:|---|---|")
            for c in s["cells"]:
                if "error" in c:
                    md.append(f"| {c['factor']:.3f} | — | — | ERROR | `{c['error'][:60]}` |")
                    continue
                kflw = c["k_followup"]; sigma = c["k_followup_std"]
                ratio = (kflw - s["k_source"]) / max(abs(s["k_source"]), 1e-12)
                outcome = c.get("outcome", "?")
                note = ""
                if s["assertion"] == "variance-ratio":
                    if sigma > 0 and s["k_source_std"] > 0:
                        obs_ratio = sigma / s["k_source_std"]
                        import math
                        target = 1.0 / math.sqrt(c["factor"])
                        note = f"σ-ratio obs={obs_ratio:.3f} target={target:.3f}"
                else:
                    note = f"Δk/k = {ratio*100:+.2f}%"
                md.append(f"| {c['factor']:.3f} | {kflw:.5f} | {sigma:.5f} | "
                          f"{outcome} | {note} |")

            # Sweep-level scorecard
            ok = sum(1 for c in s["cells"] if c.get("outcome") == "missed")
            det = sum(1 for c in s["cells"] if c.get("outcome") == "detected")
            err = sum(1 for c in s["cells"] if "error" in c)
            md.append(f"\n**Scorecard**: {ok}/{len(s['cells'])} satisfied "
                      f"({det} would-have-detected, {err} errored).")

    # Skipped section
    skipped = [k for k, v in results["scenarios"].items() if v.get("skipped")]
    if skipped:
        md.append("\n## Skipped\n")
        for k in skipped:
            md.append(f"* {k} — OpenMC not available in this session")

    md.append("\n---\n\nRaw data: `docs/experiments/_data/mr-parameter-sweep.json`.\n")
    REPORT_PATH.write_text("\n".join(md) + "\n")
    print(f"Wrote {REPORT_PATH.relative_to(REPO_ROOT)}")


if __name__ == "__main__":
    raise SystemExit(main())
