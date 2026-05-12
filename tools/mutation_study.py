"""Mutation-detection study orchestrator (Stage 5 Phase 1).

Pipeline (matches `docs/superpowers/plans/2026-05-12-stage5-mutation-detection-study.md`):

    candidates ──▶ baseline screening ──▶ semantic mutants ──▶ MR matrix
                  (3-rep OpenMC, 1-rep OpenMOC, threshold rule)
                                                    ▼
                                            Wilson CI + Cohen's κ + sensitivity

Subcommands
-----------

    python3 tools/mutation_study.py baseline
        # run unpatched source case once per solver, cache in baseline.json

    python3 tools/mutation_study.py screen <id> [<id> ...]
    python3 tools/mutation_study.py screen --all

    python3 tools/mutation_study.py matrix <id> [<id> ...]
    python3 tools/mutation_study.py matrix --all-semantic

    python3 tools/mutation_study.py stats
        # consume the cached per-mutant JSONs and emit CSVs + Markdown

All subcommands are idempotent: results land in
`docs/experiments/_data/candidates/<id>/screening.json` and `matrix.json`,
and the orchestrator skips work that's already on disk.

Environment
-----------

    OPENMOC_PYTHON   path to a Python with openmoc importable
                     (default: /opt/openmoc-venv/bin/python)
    OPENMC_PYTHON    path to a Python with openmc importable
                     (default: /opt/miniconda3/envs/openmc-env/bin/python)
"""

from __future__ import annotations

import argparse
import json
import math
import os
import shutil
import subprocess
import sys
import tempfile
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable

REPO_ROOT = Path(__file__).resolve().parent.parent
SUT_DIR = REPO_ROOT / "SUT"
SOURCE_CASE = SUT_DIR / "openmoc" / "sample" / "pincell.json"
DATA_DIR = REPO_ROOT / "docs" / "experiments" / "_data"
BASELINE_PATH = DATA_DIR / "baseline.json"
CANDIDATES_DIR = DATA_DIR / "candidates"

DEFAULT_OPENMOC_PYTHON = "/opt/openmoc-venv/bin/python"
DEFAULT_OPENMC_PYTHON = "/opt/miniconda3/envs/openmc-env/bin/python"
DEFAULT_FACTOR = 1.5

OPENMC_REPS = 3  # screening replicate count for OpenMC (deterministic OpenMOC needs only 1)

# Scenarios: (id, solver, transform, adapter_filename, assertion, value_name)
#
# Phase 1 scenarios: rows 1-4 (ScaleNuSigmaF and ScaleFuelSigmaA × 2 solvers).
# Phase 2 (NOETHER) additions: rows 5+ — PermuteEnergyGroups (m_inv) and the
# per-material monotonicity MRs (m_mono). The "approx" assertion uses the
# scenario's `tolerance_rel` field; "greater"/"less" use the existing
# strict comparison.
SCENARIOS: list[dict] = [
    {
        "id": "openmoc-pincell-nu-sigma-f",
        "solver": "openmoc",
        "transform": "ScaleNuSigmaF",
        "adapter": "openmoc/openmoc_input_adapter.py",
        "runner": "openmoc/openmoc_runner.py",
        "assertion": "greater",
        "value": "k_eff",
    },
    {
        "id": "openmc-pincell-nu-sigma-f",
        "solver": "openmc",
        "transform": "ScaleNuSigmaF",
        "adapter": "openmc/openmc_input_adapter.py",
        "runner": "openmc/openmc_runner.py",
        "assertion": "greater",
        "value": "k_eff",
    },
    {
        "id": "openmoc-pincell-sigma-a",
        "solver": "openmoc",
        "transform": "ScaleFuelSigmaA",
        "adapter": "openmoc/openmoc_input_adapter_sigma_a.py",
        "runner": "openmoc/openmoc_runner.py",
        "assertion": "less",
        "value": "k_eff",
    },
    {
        "id": "openmc-pincell-sigma-a",
        "solver": "openmc",
        "transform": "ScaleFuelSigmaA",
        "adapter": "openmc/openmc_input_adapter_sigma_a.py",
        "runner": "openmc/openmc_runner.py",
        "assertion": "less",
        "value": "k_eff",
    },
    # ----- Phase 2 (NOETHER) -----
    {
        "id": "openmoc-pincell-group-permute",
        "solver": "openmoc",
        "transform": "PermuteEnergyGroups",
        "adapter": "openmoc/openmoc_input_adapter_group_permute.py",
        "runner": "openmoc/openmoc_runner.py",
        "assertion": "approx",
        "tolerance_rel": 1e-6,  # OpenMOC is deterministic; permutation should be bit-stable
        "value": "k_eff",
        "phase": 2,
        "noether_id": "N04",
        "meta_pattern": "m_inv",
    },
    {
        "id": "openmoc-pincell-fuel-sigma-t",
        "solver": "openmoc",
        "transform": "ScaleFuelSigmaT",
        "adapter": "openmoc/openmoc_input_adapter_fuel_sigma_t.py",
        "runner": "openmoc/openmoc_runner.py",
        "assertion": "less",
        "value": "k_eff",
        "phase": 2,
        "noether_id": "N05",
        "meta_pattern": "m_mono",
    },
    {
        "id": "openmoc-pincell-moderator-sigma-a",
        "solver": "openmoc",
        "transform": "ScaleModeratorSigmaA",
        "adapter": "openmoc/openmoc_input_adapter_moderator_sigma_a.py",
        "runner": "openmoc/openmoc_runner.py",
        "assertion": "less",
        "value": "k_eff",
        "phase": 2,
        "noether_id": "N07",
        "meta_pattern": "m_mono",
    },
    {
        "id": "openmc-pincell-group-permute",
        "solver": "openmc",
        "transform": "PermuteEnergyGroups",
        "adapter": "openmc/openmc_input_adapter_group_permute.py",
        "runner": "openmc/openmc_runner.py",
        "assertion": "approx",
        "tolerance_rel": 0.005,  # OpenMC: relax to 3·σ scale
        "value": "k_eff",
        "phase": 2,
        "noether_id": "N04",
        "meta_pattern": "m_inv",
    },
    {
        "id": "openmc-pincell-fuel-sigma-t",
        "solver": "openmc",
        "transform": "ScaleFuelSigmaT",
        "adapter": "openmc/openmc_input_adapter_fuel_sigma_t.py",
        "runner": "openmc/openmc_runner.py",
        "assertion": "less",
        "value": "k_eff",
        "phase": 2,
        "noether_id": "N05",
        "meta_pattern": "m_mono",
    },
    {
        "id": "openmc-pincell-moderator-sigma-a",
        "solver": "openmc",
        "transform": "ScaleModeratorSigmaA",
        "adapter": "openmc/openmc_input_adapter_moderator_sigma_a.py",
        "runner": "openmc/openmc_runner.py",
        "assertion": "less",
        "value": "k_eff",
        "phase": 2,
        "noether_id": "N07",
        "meta_pattern": "m_mono",
    },
]


# ---------------------------------------------------------------------------
# Plumbing
# ---------------------------------------------------------------------------

sys.path.insert(0, str(REPO_ROOT / "tools"))
from mutations import ALL_MUTATIONS, Mutation, by_id  # noqa: E402


def resolve_pythons() -> tuple[str, str]:
    return (
        os.environ.get("OPENMOC_PYTHON", DEFAULT_OPENMOC_PYTHON),
        os.environ.get("OPENMC_PYTHON", DEFAULT_OPENMC_PYTHON),
    )


def openmc_available() -> bool:
    """OpenMC is considered available iff the resolved python can `import openmc`.

    Used by orchestrator commands to gracefully skip OpenMC scenarios on cloud
    sessions where only the OpenMOC venv was provisioned. Set the env var
    `METBENCH_FORCE_OPENMC=1` to fail loudly instead of skipping.
    """
    if os.environ.get("METBENCH_FORCE_OPENMC") == "1":
        return True
    _, openmc_python = resolve_pythons()
    if not Path(openmc_python).exists():
        return False
    try:
        proc = subprocess.run(
            [openmc_python, "-c", "import openmc"],
            capture_output=True, text=True, timeout=10,
        )
        return proc.returncode == 0
    except Exception:
        return False


SUBPROCESS_TIMEOUT_S = 90  # generous: normal OpenMC source case is ~7s, OpenMOC ~0.6s


def run_subprocess(cmd: list[str], cwd: Path | None = None) -> str:
    try:
        proc = subprocess.run(
            cmd, capture_output=True, text=True, check=False, cwd=cwd,
            timeout=SUBPROCESS_TIMEOUT_S,
        )
    except subprocess.TimeoutExpired as e:
        raise RuntimeError(
            f"Subprocess timed out after {SUBPROCESS_TIMEOUT_S}s: {' '.join(cmd)}\n"
            f"stdout (partial):\n{e.stdout or ''}\n"
            f"stderr (partial):\n{e.stderr or ''}"
        )
    if proc.returncode != 0:
        raise RuntimeError(
            f"Subprocess failed (exit {proc.returncode}): {' '.join(cmd)}\n"
            f"stdout:\n{proc.stdout}\n"
            f"stderr:\n{proc.stderr}"
        )
    return proc.stdout


def run_solver(sut_root: Path, scenario: dict, input_json: Path, output_json: Path, python_exec: str) -> dict:
    runner = sut_root / scenario["runner"]
    run_subprocess([python_exec, str(runner), "--input", str(input_json), "--output", str(output_json)])
    return json.loads(output_json.read_text(encoding="utf-8"))


def apply_transformation(sut_root: Path, scenario: dict, source: Path, output: Path, factor: float, python_exec: str) -> None:
    adapter = sut_root / scenario["adapter"]
    run_subprocess([
        python_exec, str(adapter), "transform-input",
        "--source-file", str(source),
        "--output-file", str(output),
        "--params", json.dumps({"factor": str(factor)}),
    ])


def affected_scenarios(mutation: Mutation) -> list[dict]:
    """Return the subset of SCENARIOS whose adapter or runner is the
    mutation's target. M00 (identity) returns the full list."""
    if mutation.id == "M00-identity":
        return list(SCENARIOS)
    target = mutation.target_file
    # Strip leading "SUT/" since adapter/runner keys are relative to SUT_DIR.
    rel = target[len("SUT/"):] if target.startswith("SUT/") else target
    out = []
    for sc in SCENARIOS:
        if sc["adapter"] == rel or sc["runner"] == rel:
            out.append(sc)
    return out


def mutation_solver(mutation: Mutation) -> str:
    if mutation.id == "M00-identity":
        return "both"
    if mutation.target_file.startswith("SUT/openmoc/"):
        return "openmoc"
    if mutation.target_file.startswith("SUT/openmc/"):
        return "openmc"
    raise RuntimeError(f"Cannot infer solver from target_file {mutation.target_file}")


# ---------------------------------------------------------------------------
# SUT staging
# ---------------------------------------------------------------------------

def stage_sut(mutation: Mutation, dest_root: Path) -> Path:
    """Copy SUT/ to dest_root/SUT and apply the mutation in place.

    Returns the resulting SUT directory path.
    """
    sut_copy = dest_root / "SUT"
    if sut_copy.exists():
        shutil.rmtree(sut_copy)
    shutil.copytree(SUT_DIR, sut_copy)
    if mutation.id == "M00-identity":
        return sut_copy
    target = dest_root / mutation.target_file
    text = target.read_text(encoding="utf-8")
    new_text = mutation.apply(text)
    if new_text == text:
        raise RuntimeError(f"Patch for {mutation.id} resulted in no change to {target}")
    target.write_text(new_text, encoding="utf-8")
    return sut_copy


# ---------------------------------------------------------------------------
# Source-case execution (used by baseline + screening)
# ---------------------------------------------------------------------------

def run_source(sut_root: Path, solver: str, openmoc_python: str, openmc_python: str, reps: int) -> dict:
    """Run the source pincell.json case `reps` times for the named solver.

    Returns: {"reps": [...], "mean": float, "max_std": float}.
    """
    if solver == "openmoc":
        runner = sut_root / "openmoc" / "openmoc_runner.py"
        python_exec = openmoc_python
    elif solver == "openmc":
        runner = sut_root / "openmc" / "openmc_runner.py"
        python_exec = openmc_python
    else:
        raise ValueError(f"Unknown solver {solver!r}")

    keffs: list[float] = []
    stds: list[float] = []
    with tempfile.TemporaryDirectory(prefix=f"src-{solver}-") as tmp:
        tmp_path = Path(tmp)
        for r in range(reps):
            out = tmp_path / f"out-{r}.json"
            run_subprocess([python_exec, str(runner), "--input", str(SOURCE_CASE), "--output", str(out)])
            payload = json.loads(out.read_text(encoding="utf-8"))
            keffs.append(float(payload["k_eff"]))
            stds.append(float(payload.get("k_eff_std", 0.0)))
    mean = sum(keffs) / len(keffs)
    return {
        "solver": solver,
        "reps": keffs,
        "stds": stds,
        "mean": mean,
        "max_std": max(stds) if stds else 0.0,
    }


# ---------------------------------------------------------------------------
# Baseline
# ---------------------------------------------------------------------------

def cmd_baseline(args: argparse.Namespace) -> int:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    openmoc_python, openmc_python = resolve_pythons()
    has_openmc = openmc_available()

    if BASELINE_PATH.exists() and not args.force:
        print(f"baseline.json already exists; pass --force to recompute", file=sys.stderr)
        print(BASELINE_PATH.read_text())
        return 0

    print("Running unpatched baseline:", file=sys.stderr)
    if has_openmc:
        print("  source case: OpenMOC (1 rep) + OpenMC (3 reps) ...", file=sys.stderr)
    else:
        print("  source case: OpenMOC (1 rep); OpenMC SKIPPED (not available)", file=sys.stderr)
    openmoc_base = run_source(SUT_DIR, "openmoc", openmoc_python, openmc_python, reps=1)
    openmc_base = (
        run_source(SUT_DIR, "openmc", openmoc_python, openmc_python, reps=OPENMC_REPS)
        if has_openmc else
        {"solver": "openmc", "reps": [], "stds": [], "mean": float("nan"),
         "max_std": 0.0, "skipped": True}
    )

    # Per-scenario follow-up baselines (unpatched, factor=1.5). Used by the
    # final classification step in `stats` to detect adapter-only mutations
    # that don't shift the source case but do shift the follow-up.
    eligible_scenarios = [sc for sc in SCENARIOS if has_openmc or sc["solver"] != "openmc"]
    skipped_scenarios = [sc["id"] for sc in SCENARIOS if not has_openmc and sc["solver"] == "openmc"]
    print(f"  follow-up cases (factor={DEFAULT_FACTOR}): {len(eligible_scenarios)} scenarios "
          f"({len(skipped_scenarios)} OpenMC skipped) ...", file=sys.stderr)
    followups: dict[str, dict] = {}
    with tempfile.TemporaryDirectory(prefix="baseline-flw-") as tmp:
        tmp_path = Path(tmp)
        for sc in eligible_scenarios:
            python_exec = openmoc_python if sc["solver"] == "openmoc" else openmc_python
            flw_in = tmp_path / f"flw-in-{sc['id']}.json"
            flw_out = tmp_path / f"flw-out-{sc['id']}.json"
            apply_transformation(SUT_DIR, sc, SOURCE_CASE, flw_in, DEFAULT_FACTOR, python_exec)
            result = run_solver(SUT_DIR, sc, flw_in, flw_out, python_exec)
            followups[sc["id"]] = {
                "k_eff": float(result["k_eff"]),
                "k_eff_std": float(result.get("k_eff_std", 0.0)),
            }
    for sid in skipped_scenarios:
        followups[sid] = {"k_eff": float("nan"), "k_eff_std": 0.0, "skipped": True}

    payload = {
        "openmoc": openmoc_base,
        "openmc": openmc_base,
        "followups": followups,
        "factor": DEFAULT_FACTOR,
        "openmc_available": has_openmc,
    }
    BASELINE_PATH.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {BASELINE_PATH}", file=sys.stderr)
    print(json.dumps(payload, indent=2))
    return 0


def load_baseline() -> dict:
    if not BASELINE_PATH.exists():
        raise RuntimeError("baseline.json missing; run `mutation_study.py baseline` first")
    return json.loads(BASELINE_PATH.read_text(encoding="utf-8"))


# ---------------------------------------------------------------------------
# Screening
# ---------------------------------------------------------------------------

def classify(mut_mean: float, mut_max_std: float, baseline_mean: float, threshold_rel: float, solver: str) -> dict:
    """Apply the screening rule:
        semantic iff |k_mut - k_baseline| > max(3*max_std, threshold_rel * k_baseline)
    For deterministic solvers (max_std==0), only the relative threshold applies.
    """
    abs_delta = abs(mut_mean - baseline_mean)
    rel_threshold = threshold_rel * baseline_mean
    mc_threshold = 3.0 * mut_max_std
    threshold = max(mc_threshold, rel_threshold)
    return {
        "abs_delta": abs_delta,
        "rel_delta": abs_delta / baseline_mean if baseline_mean else float("nan"),
        "mc_threshold_3sigma": mc_threshold,
        "rel_threshold": rel_threshold,
        "threshold": threshold,
        "semantic": abs_delta > threshold,
    }


def screen_one(mutation: Mutation, args: argparse.Namespace) -> dict:
    openmoc_python, openmc_python = resolve_pythons()
    has_openmc = openmc_available()
    baseline = load_baseline()

    cand_dir = CANDIDATES_DIR / mutation.id
    cand_dir.mkdir(parents=True, exist_ok=True)
    screening_path = cand_dir / "screening.json"
    if screening_path.exists() and not args.force:
        print(f"  {mutation.id}: cached screening result, skip (--force to redo)", file=sys.stderr)
        return json.loads(screening_path.read_text(encoding="utf-8"))

    solver = mutation_solver(mutation)
    with tempfile.TemporaryDirectory(prefix=f"mut-{mutation.id}-") as tmp:
        tmp_path = Path(tmp)
        sut_copy = stage_sut(mutation, tmp_path)
        print(f"  {mutation.id}: running screening (solver={solver}) ...", file=sys.stderr)
        try:
            if solver in ("openmoc", "both"):
                mut_moc = run_source(sut_copy, "openmoc", openmoc_python, openmc_python, reps=1)
            else:
                mut_moc = None
            if solver in ("openmc", "both") and has_openmc:
                mut_mc = run_source(sut_copy, "openmc", openmoc_python, openmc_python, reps=OPENMC_REPS)
            else:
                mut_mc = None
        except RuntimeError as e:
            payload = {
                "mutation_id": mutation.id,
                "solver": solver,
                "error": str(e)[:2000],
                "classification": "error",
            }
            screening_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
            return payload

    classifications = {}
    if mut_moc is not None:
        classifications["openmoc"] = classify(
            mut_moc["mean"], mut_moc["max_std"], baseline["openmoc"]["mean"],
            threshold_rel=args.threshold_rel, solver="openmoc",
        )
    if mut_mc is not None:
        classifications["openmc"] = classify(
            mut_mc["mean"], mut_mc["max_std"], baseline["openmc"]["mean"],
            threshold_rel=args.threshold_rel, solver="openmc",
        )

    overall = "semantic" if any(c["semantic"] for c in classifications.values()) else "equivalent"
    payload = {
        "mutation_id": mutation.id,
        "predicted_classification": mutation.predicted_classification,
        "target_file": mutation.target_file,
        "solver": solver,
        "patched_source": {
            "openmoc": mut_moc,
            "openmc": mut_mc,
        },
        "baseline": {
            "openmoc": baseline["openmoc"]["mean"],
            "openmc": baseline["openmc"]["mean"],
        },
        "classifications_per_solver": classifications,
        "classification": overall,
        "threshold_rel": args.threshold_rel,
    }
    screening_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    return payload


def cmd_screen(args: argparse.Namespace) -> int:
    candidates = _resolve_candidates(args)
    print(f"Screening {len(candidates)} candidates (threshold_rel={args.threshold_rel}) ...", file=sys.stderr)
    summary = []
    for m in candidates:
        result = screen_one(m, args)
        line = (
            f"{m.id}: predicted={m.predicted_classification}, "
            f"observed={result.get('classification', 'error')}"
        )
        per = result.get("classifications_per_solver", {})
        for solver, c in per.items():
            line += f" | {solver}: |Δ|={c['abs_delta']:.5f} ({c['rel_delta']*100:.3f}%), thr={c['threshold']:.5f}"
        print(line, file=sys.stderr)
        summary.append(result)
    return 0


# ---------------------------------------------------------------------------
# MR matrix
# ---------------------------------------------------------------------------

def evaluate_mr(k_source: float, k_followup: float, assertion: str, tolerance_rel: float = 1e-6) -> bool:
    if assertion == "greater":
        return k_followup > k_source
    if assertion == "less":
        return k_followup < k_source
    if assertion == "approx":
        # Pass iff |Δk| / |k_source| ≤ tolerance_rel. Used by Phase-2 m_inv MRs
        # (PermuteEnergyGroups), where the eigenvalue must be invariant under
        # the symmetry transformation modulo solver noise.
        if k_source == 0:
            return k_followup == 0
        return abs(k_followup - k_source) <= tolerance_rel * abs(k_source)
    raise ValueError(f"Unknown assertion: {assertion}")


def matrix_one(mutation: Mutation, args: argparse.Namespace) -> dict:
    openmoc_python, openmc_python = resolve_pythons()
    has_openmc = openmc_available()
    cand_dir = CANDIDATES_DIR / mutation.id
    cand_dir.mkdir(parents=True, exist_ok=True)
    matrix_path = cand_dir / "matrix.json"
    if matrix_path.exists() and not args.force:
        print(f"  {mutation.id}: cached matrix result, skip", file=sys.stderr)
        return json.loads(matrix_path.read_text(encoding="utf-8"))

    affected = affected_scenarios(mutation)
    cells: list[dict] = []
    with tempfile.TemporaryDirectory(prefix=f"mat-{mutation.id}-") as tmp:
        tmp_path = Path(tmp)
        sut_copy = stage_sut(mutation, tmp_path)

        for sc in SCENARIOS:
            cell: dict = {"scenario_id": sc["id"]}
            if sc not in affected and mutation.id != "M00-identity":
                cell["status"] = "not-affected"
                cells.append(cell)
                continue
            if sc["solver"] == "openmc" and not has_openmc:
                cell["status"] = "skipped-no-openmc"
                cells.append(cell)
                continue
            try:
                source_out = tmp_path / f"src-{sc['solver']}-{sc['id']}.json"
                followup_in = tmp_path / f"flw-in-{sc['id']}.json"
                followup_out = tmp_path / f"flw-out-{sc['id']}.json"
                python_exec = openmoc_python if sc["solver"] == "openmoc" else openmc_python

                # Source run (with patched SUT — this is k_source for the MR).
                src = run_solver(sut_copy, sc, SOURCE_CASE, source_out, python_exec)
                k_src = float(src["k_eff"])

                # Build follow-up via patched adapter.
                apply_transformation(sut_copy, sc, SOURCE_CASE, followup_in, args.factor, python_exec)

                # Follow-up run.
                flw = run_solver(sut_copy, sc, followup_in, followup_out, python_exec)
                k_flw = float(flw["k_eff"])

                passed = evaluate_mr(
                    k_src, k_flw, sc["assertion"],
                    tolerance_rel=float(sc.get("tolerance_rel", 1e-6)),
                )
                cell.update({
                    "status": "ran",
                    "k_source": k_src,
                    "k_followup": k_flw,
                    "k_source_std": float(src.get("k_eff_std", 0.0)),
                    "k_followup_std": float(flw.get("k_eff_std", 0.0)),
                    "ratio": k_flw / k_src if k_src else float("nan"),
                    "assertion": sc["assertion"],
                    "assertion_passed": passed,
                    "outcome": "missed" if passed else "detected",
                })
            except RuntimeError as e:
                cell["status"] = "error"
                cell["error"] = str(e)[:1500]
                cell["outcome"] = "error"
            cells.append(cell)

    payload = {
        "mutation_id": mutation.id,
        "factor": args.factor,
        "cells": cells,
    }
    matrix_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    return payload


def cmd_matrix(args: argparse.Namespace) -> int:
    if args.all_semantic:
        candidates = []
        for m in ALL_MUTATIONS:
            scr_path = CANDIDATES_DIR / m.id / "screening.json"
            if scr_path.exists():
                cls = json.loads(scr_path.read_text())
                if cls.get("classification") == "semantic":
                    candidates.append(m)
        print(f"Matrix run: {len(candidates)} semantic mutants (post-screening)", file=sys.stderr)
    else:
        candidates = _resolve_candidates(args)

    for m in candidates:
        print(f"  {m.id}: running matrix ...", file=sys.stderr)
        result = matrix_one(m, args)
        for cell in result["cells"]:
            if cell.get("status") == "ran":
                arrow = "DETECT" if cell["outcome"] == "detected" else "miss  "
                print(f"    {cell['scenario_id']}: {arrow}  "
                      f"k_src={cell['k_source']:.5f} k_flw={cell['k_followup']:.5f} "
                      f"ratio={cell['ratio']:.5f}", file=sys.stderr)
            elif cell.get("status") == "error":
                print(f"    {cell['scenario_id']}: ERROR {cell.get('error','')[:120]}",
                      file=sys.stderr)
    return 0


def _resolve_candidates(args: argparse.Namespace) -> list[Mutation]:
    if args.all:
        return list(ALL_MUTATIONS)
    if not args.ids:
        raise SystemExit("Specify mutation ids or --all / --all-semantic")
    return [by_id(i) for i in args.ids]


# ---------------------------------------------------------------------------
# Stats
# ---------------------------------------------------------------------------

def wilson_ci(k: int, n: int, z: float = 1.96) -> tuple[float, float]:
    if n == 0:
        return (0.0, 1.0)
    p = k / n
    denom = 1 + z * z / n
    center = (p + z * z / (2 * n)) / denom
    margin = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / denom
    return (max(0.0, center - margin), min(1.0, center + margin))


def cohens_kappa(rater_a: list[int], rater_b: list[int]) -> float:
    """Cohen's κ on two binary raters with equal-length 0/1 lists."""
    if len(rater_a) != len(rater_b) or not rater_a:
        return float("nan")
    n = len(rater_a)
    p_o = sum(1 for a, b in zip(rater_a, rater_b) if a == b) / n
    p_a1 = sum(rater_a) / n
    p_b1 = sum(rater_b) / n
    p_e = p_a1 * p_b1 + (1 - p_a1) * (1 - p_b1)
    if p_e >= 1.0:
        return 1.0 if p_o == 1.0 else float("nan")
    return (p_o - p_e) / (1 - p_e)


def kappa_interpret(k: float) -> str:
    if math.isnan(k):
        return "undefined (degenerate)"
    if k < 0:
        return "worse than chance"
    if k < 0.20:
        return "slight"
    if k < 0.40:
        return "fair"
    if k < 0.60:
        return "moderate"
    if k < 0.80:
        return "substantial"
    return "almost perfect"


def classify_from_matrix(mutation: Mutation, matrix: dict | None, baseline: dict, threshold_rel: float) -> dict:
    """Comprehensive classification using both source and follow-up shifts.

    A candidate is `semantic` if any of:
      - some affected scenario errored (the mutation broke the runner);
      - some affected scenario's |k_source_mut - baseline_source| > threshold;
      - some affected scenario's |k_followup_mut - baseline_followup| > threshold.

    The threshold per scenario is max(3·σ_max, threshold_rel · baseline).
    σ_max is the larger of baseline-statepoint-σ and mutant-statepoint-σ.
    """
    if matrix is None:
        return {"classification": "unknown", "reason": "no matrix data"}

    sources_base = {"openmoc": baseline["openmoc"]["mean"], "openmc": baseline["openmc"]["mean"]}
    flw_base = baseline.get("followups", {})

    detail: list[dict] = []
    any_semantic = False
    any_error = False
    for cell in matrix["cells"]:
        sc = next((s for s in SCENARIOS if s["id"] == cell["scenario_id"]), None)
        if not sc:
            continue
        if cell.get("status") == "not-affected":
            continue
        if cell.get("status") == "error":
            any_error = True
            detail.append({"scenario": cell["scenario_id"], "verdict": "error", "delta_source": None, "delta_followup": None})
            continue
        if cell.get("status") != "ran":
            continue

        # σ_max: larger of statepoint stds reported in the cell.
        sigma = max(cell.get("k_source_std", 0.0), cell.get("k_followup_std", 0.0))

        # Source shift.
        k_src_base = sources_base[sc["solver"]]
        k_src_mut = cell["k_source"]
        # Nan / inf in the patched k_eff is itself a clear semantic shift
        # from a finite baseline (no need to compare against threshold).
        src_pathological = math.isnan(k_src_mut) or math.isinf(k_src_mut)
        d_src = abs(k_src_mut - k_src_base) if not src_pathological else float("inf")
        thr_src = max(3.0 * sigma, threshold_rel * k_src_base)

        # Follow-up shift.
        base_flw = flw_base.get(sc["id"], {})
        k_flw_base = float(base_flw.get("k_eff", float("nan")))
        k_flw_mut = cell["k_followup"]
        flw_pathological = math.isnan(k_flw_mut) or math.isinf(k_flw_mut)
        if math.isnan(k_flw_base):
            d_flw = float("nan")
            thr_flw = float("nan")
        elif flw_pathological:
            d_flw = float("inf")
            thr_flw = max(3.0 * sigma, threshold_rel * k_flw_base)
        else:
            d_flw = abs(k_flw_mut - k_flw_base)
            thr_flw = max(3.0 * sigma, threshold_rel * k_flw_base)

        src_shifted = src_pathological or (not math.isnan(d_src) and d_src > thr_src)
        flw_shifted = flw_pathological or (
            (not math.isnan(d_flw)) and d_flw > thr_flw
        )
        scenario_semantic = src_shifted or flw_shifted
        if scenario_semantic:
            any_semantic = True
        detail.append({
            "scenario": cell["scenario_id"],
            "verdict": "semantic" if scenario_semantic else "equivalent-for-this-scenario",
            "delta_source": d_src,
            "delta_followup": d_flw if not math.isnan(d_flw) else None,
            "threshold_source": thr_src,
            "threshold_followup": thr_flw if not math.isnan(thr_flw) else None,
        })

    if any_error:
        classification = "error"
    elif any_semantic:
        classification = "semantic"
    else:
        classification = "equivalent"
    return {"classification": classification, "detail": detail}


def cmd_stats(args: argparse.Namespace) -> int:
    baseline = load_baseline()
    if "followups" not in baseline:
        print("baseline.json is missing 'followups' — re-run `mutation_study.py baseline --force`", file=sys.stderr)
        return 2

    rows = []
    for m in ALL_MUTATIONS:
        scr_path = CANDIDATES_DIR / m.id / "screening.json"
        mat_path = CANDIDATES_DIR / m.id / "matrix.json"
        scr = json.loads(scr_path.read_text()) if scr_path.exists() else None
        mat = json.loads(mat_path.read_text()) if mat_path.exists() else None
        post = classify_from_matrix(m, mat, baseline, args.threshold_rel)
        rows.append({"mutation": m, "screening": scr, "matrix": mat, "post": post})

    _emit_screening_csv_md(rows, args)
    _emit_matrix_csv_md(rows)
    _emit_kappa_and_sensitivity_md(rows, args)
    return 0


def _emit_screening_csv_md(rows: list[dict], args: argparse.Namespace) -> None:
    """Emit screening-results.{csv,md} using the post-matrix classification.

    Methodology note: the plan PR #25 prescribed a source-only screening rule.
    During execution we found that rule degenerate for adapter mutations
    (the source case doesn't go through the adapter, so the source k_eff
    is unchanged even when the adapter behaviour is fundamentally different).
    The screening below uses the richer rule: a mutation is `semantic` iff
    either (a) the patched source case shifts above threshold, or (b) the
    patched follow-up case shifts above threshold for some affected scenario.
    Both signals come from the matrix runs. The original source-only signal
    is retained in `screening.json` per-candidate as a transparency artifact.
    """
    csv_path = REPO_ROOT / "docs" / "experiments" / "screening-results.csv"
    md_path = REPO_ROOT / "docs" / "experiments" / "screening-results.md"

    headers = ["mutation_id", "predicted", "source_only_observed",
               "matrix_observed", "any_source_shift", "any_followup_shift", "errors"]
    csv_lines = [",".join(headers)]

    semantic_n = equivalent_n = error_n = unknown_n = 0
    md_rows = []
    for row in rows:
        m = row["mutation"]
        scr = row["screening"] or {}
        post = row["post"]
        post_cls = post["classification"]

        if post_cls == "semantic":
            semantic_n += 1
        elif post_cls == "equivalent":
            equivalent_n += 1
        elif post_cls == "error":
            error_n += 1
        else:
            unknown_n += 1

        any_src = any(d["verdict"] != "equivalent-for-this-scenario" and d["delta_source"] is not None
                      and d["delta_source"] > d.get("threshold_source", 0)
                      for d in post.get("detail", []))
        any_flw = any(d["delta_followup"] is not None and d.get("threshold_followup") is not None
                      and d["delta_followup"] > d["threshold_followup"]
                      for d in post.get("detail", []))
        n_err = sum(1 for d in post.get("detail", []) if d["verdict"] == "error")

        record = [m.id, m.predicted_classification, scr.get("classification", "—"),
                  post_cls, str(any_src), str(any_flw), str(n_err)]
        csv_lines.append(",".join(record))
        md_rows.append((m, scr, post, record))

    csv_path.write_text("\n".join(csv_lines) + "\n", encoding="utf-8")

    md = ["# Baseline screening results\n"]
    md.append("Threshold rule: a mutation is **semantic** iff some affected scenario shows\n")
    md.append("`|k_source_mut − k_source_base| > max(3·σ, 0.5%·k_base)` **or**\n")
    md.append("`|k_followup_mut − k_followup_base| > max(3·σ, 0.5%·k_base)`.\n")
    md.append("σ is the larger of source / follow-up statepoint stds for the mutant.\n")
    md.append("\n**Methodology note**: plan PR #25 prescribed a source-only screening rule\n"
              "(`|k_source_mut − k_source_base|`). Execution found that rule degenerate for\n"
              "adapter mutations (adapter is not exercised by the source case). The screening\n"
              "below uses the richer rule above, which combines both signals from the matrix runs.\n"
              "Per-candidate source-only results are retained in `_data/candidates/<id>/screening.json`\n"
              "as a transparency artifact. The discrepancy between the two columns below shows\n"
              "exactly which mutations would have been miss-classified by the source-only rule.\n")
    md.append(f"\n**Discard rate**: {equivalent_n} of {len(rows)} candidates "
              f"({equivalent_n / len(rows) * 100:.1f}%) classified equivalent under the matrix rule.\n")
    md.append(f"\nClassification counts: semantic={semantic_n}, equivalent={equivalent_n}, "
              f"error={error_n}, unknown={unknown_n}\n")
    md.append("\n| mutation | predicted | source-only signal | matrix signal | source shifted? | follow-up shifted? | err cells |")
    md.append("|---|---|---|---|---|---|---|")
    for m, scr, post, rec in md_rows:
        md.append("| " + " | ".join(rec) + " |")

    md.append("\n## Discarded (equivalent) mutants — why?\n")
    for m, scr, post, rec in md_rows:
        if post["classification"] != "equivalent":
            continue
        md.append(f"### {m.id}\n")
        md.append(f"*{m.description}*\n")
        md.append(f"\n**Predicted**: {m.predicted_classification}.\n")
        md.append(f"\n**Rationale**: {m.rationale}\n")
        if post.get("detail"):
            md.append("\nObserved per-scenario shifts:\n")
            md.append("| scenario | Δsource | Δfollow-up | threshold |")
            md.append("|---|---|---|---|")
            for d in post["detail"]:
                ds = f"{d['delta_source']:.6f}" if d.get('delta_source') is not None else "—"
                df = f"{d['delta_followup']:.6f}" if d.get('delta_followup') is not None else "—"
                th = f"{d.get('threshold_source', 0):.6f}"
                md.append(f"| {d['scenario']} | {ds} | {df} | {th} |")
            md.append("")

    md_path.write_text("\n".join(md) + "\n", encoding="utf-8")
    print(f"Wrote {csv_path.relative_to(REPO_ROOT)} and {md_path.relative_to(REPO_ROOT)}", file=sys.stderr)


def _emit_matrix_csv_md(rows: list[dict]) -> None:
    csv_path = REPO_ROOT / "docs" / "experiments" / "mutation-detection-matrix.csv"
    md_path = REPO_ROOT / "docs" / "experiments" / "mutation-detection-matrix.md"

    headers = ["mutation_id", "scenario_id", "status", "outcome",
               "k_source", "k_followup", "ratio", "assertion"]
    csv_lines = [",".join(headers)]
    per_scenario_counts: dict[str, dict[str, int]] = {sc["id"]: {"detected": 0, "missed": 0, "error": 0, "n": 0} for sc in SCENARIOS}

    matrix_rows = []
    for row in rows:
        m = row["mutation"]
        mat = row["matrix"]
        post = row.get("post") or {}
        if not mat:
            continue
        if post.get("classification") not in ("semantic", "error") and m.id != "M00-identity":
            continue
        for cell in mat["cells"]:
            csv_lines.append(",".join([
                m.id, cell["scenario_id"], cell.get("status", ""),
                cell.get("outcome", ""),
                f"{cell.get('k_source', float('nan')):.6f}" if "k_source" in cell else "",
                f"{cell.get('k_followup', float('nan')):.6f}" if "k_followup" in cell else "",
                f"{cell.get('ratio', float('nan')):.6f}" if "ratio" in cell else "",
                cell.get("assertion", ""),
            ]))
            if cell.get("status") == "ran" and m.id != "M00-identity":
                outcome = cell["outcome"]
                per_scenario_counts[cell["scenario_id"]]["n"] += 1
                per_scenario_counts[cell["scenario_id"]][outcome] += 1
            matrix_rows.append((m, cell))

    csv_path.write_text("\n".join(csv_lines) + "\n", encoding="utf-8")

    md = ["# Mutation-detection matrix\n"]
    md.append("MR factor: 1.5. Status `not-affected` means the mutation patches a file the scenario does not exercise; status `ran` reports detected/missed; status `error` reports a runtime failure.\n")
    md.append("\n## Per-MR detection rate (Wilson 95% CI)\n")
    md.append("| Scenario | n (semantic mutants affecting it) | detected | missed | errors | rate | 95% CI |")
    md.append("|---|---|---|---|---|---|---|")
    for sc in SCENARIOS:
        c = per_scenario_counts[sc["id"]]
        n_decisive = c["detected"] + c["missed"]
        if n_decisive > 0:
            lo, hi = wilson_ci(c["detected"], n_decisive)
            rate = c["detected"] / n_decisive
            rate_str = f"{rate*100:.1f}%"
            ci_str = f"[{lo*100:.1f}%, {hi*100:.1f}%]"
        else:
            rate_str = ci_str = "—"
        md.append(f"| {sc['id']} | {c['n']} | {c['detected']} | {c['missed']} | {c['error']} | {rate_str} | {ci_str} |")
    md.append("")
    md.append("## Identity false-positive sanity\n")
    m00 = next((r for r in rows if r["mutation"].id == "M00-identity"), None)
    if m00 and m00["matrix"]:
        fp = sum(1 for c in m00["matrix"]["cells"] if c.get("outcome") == "detected")
        md.append(f"M00 (identity) detected on {fp} / {len(m00['matrix']['cells'])} scenarios. "
                  f"Expected 0 — {'✓ PASS' if fp == 0 else '✗ FAIL'}.\n")
    md.append("\n## Per-mutation detail\n")
    md.append("| mutation | scenario | outcome | k_source | k_followup | ratio |")
    md.append("|---|---|---|---|---|---|")
    for m, cell in matrix_rows:
        if cell.get("status") != "ran":
            md.append(f"| {m.id} | {cell['scenario_id']} | _{cell.get('status', '')}_ |  |  |  |")
        else:
            md.append(f"| {m.id} | {cell['scenario_id']} | {cell['outcome']} | "
                      f"{cell['k_source']:.5f} | {cell['k_followup']:.5f} | {cell['ratio']:.5f} |")
    md_path.write_text("\n".join(md) + "\n", encoding="utf-8")
    print(f"Wrote {csv_path.relative_to(REPO_ROOT)} and {md_path.relative_to(REPO_ROOT)}", file=sys.stderr)


# Matched-pair index for cross-solver κ (see mutation-catalogue.md)
MATCHED_PAIRS = [
    ("M01-openmoc-runner-chi-zero",            "M15-openmc-runner-chi-zero",            "chi-zero"),
    ("M05-openmoc-runner-chi-swap-groups",     "M20-openmc-runner-chi-swap-groups",     "chi-swap"),
    ("M06-openmoc-runner-vacuum-boundary",     "M17-openmc-runner-vacuum-boundary",     "vacuum-bc"),
    ("M07-openmoc-adapter-nsf-inverse",        "M22-openmc-adapter-nsf-inverse",        "nsf-inverse"),
    ("M08-openmoc-adapter-nsf-square",         "M23-openmc-adapter-nsf-square",         "nsf-square"),
    ("M09-openmoc-adapter-nsf-moderator",      "M24-openmc-adapter-nsf-moderator",      "nsf-moderator"),
    ("M10-openmoc-adapter-nsf-identity",       "M25-openmc-adapter-nsf-identity",       "nsf-identity"),
    ("M12-openmoc-adapter-sa-no-sigt-update",  "M26-openmc-adapter-sa-no-sigt-update",  "sa-no-sigt"),
    ("M13-openmoc-adapter-sa-inverse",         "M27-openmc-adapter-sa-inverse",         "sa-inverse"),
]


def _emit_kappa_and_sensitivity_md(rows: list[dict], args: argparse.Namespace) -> None:
    md_path = REPO_ROOT / "docs" / "experiments" / "mutation-detection-matrix.md"
    existing = md_path.read_text(encoding="utf-8") if md_path.exists() else ""

    by_id_local = {r["mutation"].id: r for r in rows}

    def detection_of(mid: str, scenario_substr: str) -> int | None:
        row = by_id_local.get(mid)
        if not row or not row["matrix"]:
            return None
        post = row.get("post") or {}
        if post.get("classification") not in ("semantic", "error"):
            return None
        for cell in row["matrix"]["cells"]:
            if scenario_substr in cell["scenario_id"]:
                if cell.get("status") == "ran":
                    return 1 if cell["outcome"] == "detected" else 0
                if cell.get("status") == "error":
                    return 1  # an erroring runner is a detected fault
        return None

    nsf_pairs = [(moc, mc) for moc, mc, kind in MATCHED_PAIRS if "nsf" in kind]
    sa_pairs = [(moc, mc) for moc, mc, kind in MATCHED_PAIRS if "sa" in kind]
    other_pairs = [(moc, mc) for moc, mc, kind in MATCHED_PAIRS
                   if kind in ("chi-zero", "chi-swap", "vacuum-bc")]

    def kappa_block(label: str, pairs: list[tuple[str, str]], moc_sc: str, mc_sc: str) -> str:
        a, b, paired_ids = [], [], []
        for moc, mc in pairs:
            da = detection_of(moc, moc_sc)
            db = detection_of(mc, mc_sc)
            if da is None or db is None:
                continue
            a.append(da)
            b.append(db)
            paired_ids.append((moc, mc, da, db))
        if not a:
            return f"\n### {label}\n\nNo evaluable pairs (all dropped by screening or errored).\n"
        k = cohens_kappa(a, b)
        out = [f"\n### {label}\n"]
        out.append(f"Pairs evaluated: {len(a)} / {len(pairs)}. Cohen's κ = **{k:.3f}** ({kappa_interpret(k)}).\n")
        out.append("\n| OpenMOC mutant | OpenMC mutant | OpenMOC outcome | OpenMC outcome |")
        out.append("|---|---|---|---|")
        for moc, mc, da, db in paired_ids:
            out.append(f"| {moc} | {mc} | {'detected' if da else 'missed'} | {'detected' if db else 'missed'} |")
        return "\n".join(out) + "\n"

    out = ["\n## Cross-solver agreement (Cohen's κ)\n"]
    out.append("κ measured on matched-pair mutants where the same conceptual fault is "
               "applied to both solvers' files (see `mutation-catalogue.md` matched-pair index). "
               "Each row: did the OpenMOC scenario detect the OpenMOC mutant; did the OpenMC scenario "
               "detect its OpenMC twin.\n")
    out.append(kappa_block("ScaleNuSigmaF pairs", nsf_pairs, "openmoc-pincell-nu-sigma-f", "openmc-pincell-nu-sigma-f"))
    out.append(kappa_block("ScaleFuelSigmaA pairs", sa_pairs, "openmoc-pincell-sigma-a", "openmc-pincell-sigma-a"))
    out.append(kappa_block("Runner-level pairs (chi/boundary) — NuSigmaF scenarios",
                           other_pairs, "openmoc-pincell-nu-sigma-f", "openmc-pincell-nu-sigma-f"))
    out.append("\n## Threshold sensitivity\n")
    out.append("Re-classify candidates at tightened and relaxed relative thresholds using the matrix data;\n")
    out.append("how many flip relative to the 0.5% baseline?\n\n")
    baseline = load_baseline()
    base_cls_by_id = {row["mutation"].id: row["post"]["classification"] for row in rows}
    flip_lines = []
    for alt_rel in (0.002, 0.005, 0.010, 0.020):
        flips = []
        for row in rows:
            m = row["mutation"]
            alt = classify_from_matrix(m, row["matrix"], baseline, alt_rel)
            if alt["classification"] != base_cls_by_id[m.id]:
                flips.append(f"{m.id} ({base_cls_by_id[m.id]} → {alt['classification']})")
        flip_lines.append((alt_rel, flips))
    out.append("| Relative threshold | # flips vs 0.5% baseline | Which |")
    out.append("|---|---|---|")
    for alt_rel, flips in flip_lines:
        marker = " (baseline)" if alt_rel == 0.005 else ""
        out.append(f"| {alt_rel*100:.2f}%{marker} | {len(flips)} | {', '.join(flips) if flips else '—'} |")
    out.append("\n---\n")
    out.append("See [`discussion.md`](discussion.md) for hand-written analysis "
               "(coverage gaps, cross-solver κ interpretation, Phase 2 hand-off).\n")
    md_path.write_text(existing.rstrip() + "\n" + "\n".join(out) + "\n", encoding="utf-8")
    print(f"Appended κ + sensitivity to {md_path.relative_to(REPO_ROOT)}", file=sys.stderr)


# ---------------------------------------------------------------------------
# CLI plumbing
# ---------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    p_base = sub.add_parser("baseline", help="Compute the unpatched baseline (1× OpenMOC, 3× OpenMC).")
    p_base.add_argument("--force", action="store_true")
    p_base.set_defaults(func=cmd_baseline)

    p_screen = sub.add_parser("screen", help="Baseline-screen candidate mutations.")
    p_screen.add_argument("ids", nargs="*")
    p_screen.add_argument("--all", action="store_true")
    p_screen.add_argument("--force", action="store_true")
    p_screen.add_argument("--threshold-rel", type=float, default=0.005,
                          help="Relative threshold for semantic classification (default 0.005 = 0.5%%)")
    p_screen.set_defaults(func=cmd_screen)

    p_mat = sub.add_parser("matrix", help="Run the MR matrix on candidates classified semantic.")
    p_mat.add_argument("ids", nargs="*")
    p_mat.add_argument("--all", action="store_true")
    p_mat.add_argument("--all-semantic", action="store_true",
                       help="Run matrix on all candidates whose screening.classification == 'semantic'.")
    p_mat.add_argument("--factor", type=float, default=DEFAULT_FACTOR)
    p_mat.add_argument("--force", action="store_true")
    p_mat.set_defaults(func=cmd_matrix)

    p_stats = sub.add_parser("stats", help="Compute CSVs + Markdown reports from cached per-mutant JSONs.")
    p_stats.add_argument("--threshold-rel", type=float, default=0.005,
                         help="Relative threshold for matrix-based classification (default 0.005 = 0.5%%)")
    p_stats.set_defaults(func=cmd_stats)

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
