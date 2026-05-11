"""Cross-program OpenMOC vs OpenMC comparison driver.

Runs the same 2-group pin-cell test case (and its two MR-transformed
follow-ups) through both solvers and prints a markdown comparison table.

Usage:

    OPENMOC_PYTHON=/opt/openmoc-venv/bin/python \
    OPENMC_PYTHON=/opt/miniconda3/envs/openmc-env/bin/python \
    python3 tools/cross_program_comparison.py [--output docs/cross-program-mr-comparison.md]

The script does not assume conda or any specific layout; it just needs two
python executables, each with the corresponding solver importable.

For each solver:

  1. Run on the source case  -> k_eff_source
  2. Run OpenMOC input adapter (ScaleNuSigmaF, factor=1.5) -> followup-nu.json
     Run on the follow-up   -> k_eff_followup_nu_sigma_f
  3. Run OpenMOC input adapter (ScaleFuelSigmaA, factor=1.5) -> followup-sa.json
     Run on the follow-up   -> k_eff_followup_sigma_a

(Both solvers' SigmaA input adapters produce equivalent follow-up JSON —
the OpenMOC adapter has been used as the canonical source-of-truth here.)
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent.parent
SUT_DIR = REPO_ROOT / "SUT"
SOURCE_CASE = SUT_DIR / "openmoc" / "sample" / "pincell.json"


def run_python(python_exec: str, script: str, *args: str) -> str:
    """Run a python script as a subprocess and return its stdout."""
    cmd = [python_exec, script, *args]
    proc = subprocess.run(cmd, capture_output=True, text=True, check=False)
    if proc.returncode != 0:
        raise RuntimeError(
            f"Subprocess failed: {' '.join(cmd)}\n"
            f"stdout:\n{proc.stdout}\nstderr:\n{proc.stderr}"
        )
    return proc.stdout


def run_solver(solver: str, python_exec: str, input_json: Path, output_json: Path) -> dict:
    runner_script = {
        "openmoc": str(SUT_DIR / "openmoc" / "openmoc_runner.py"),
        "openmc": str(SUT_DIR / "openmc" / "openmc_runner.py"),
    }[solver]
    run_python(python_exec, runner_script, "--input", str(input_json), "--output", str(output_json))
    return json.loads(output_json.read_text(encoding="utf-8"))


def apply_transformation(python_exec: str, adapter_script: Path, source: Path, output: Path, factor: float) -> None:
    run_python(
        python_exec,
        str(adapter_script),
        "transform-input",
        "--source-file", str(source),
        "--output-file", str(output),
        "--params", json.dumps({"factor": str(factor)}),
    )


def format_keff(result: dict) -> str:
    k = result.get("k_eff", float("nan"))
    std = result.get("k_eff_std")
    if std is not None and std > 0:
        return f"{k:.5f} ± {std:.5f}"
    return f"{k:.5f}"


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMOC vs OpenMC cross-program MR comparison")
    parser.add_argument("--openmoc-python", default=os.environ.get("OPENMOC_PYTHON", "python3"))
    parser.add_argument("--openmc-python", default=os.environ.get("OPENMC_PYTHON", "python3"))
    parser.add_argument("--factor", type=float, default=1.5)
    parser.add_argument("--output", default=None, help="Write markdown report to this path (otherwise stdout)")
    args = parser.parse_args()

    factor = args.factor
    openmoc_input_adapter = SUT_DIR / "openmoc" / "openmoc_input_adapter.py"
    openmoc_input_adapter_sa = SUT_DIR / "openmoc" / "openmoc_input_adapter_sigma_a.py"

    with tempfile.TemporaryDirectory(prefix="cross-program-compare-") as tmp:
        tmp = Path(tmp)

        # 1. Build follow-up cases once (OpenMOC adapter is canonical;
        #    OpenMC's adapter is byte-equivalent for these MRs).
        followup_nu = tmp / "followup-nu-sigma-f.json"
        followup_sa = tmp / "followup-sigma-a.json"
        apply_transformation(args.openmoc_python, openmoc_input_adapter,    SOURCE_CASE, followup_nu, factor)
        apply_transformation(args.openmoc_python, openmoc_input_adapter_sa, SOURCE_CASE, followup_sa, factor)

        results: dict[str, dict[str, dict]] = {"openmoc": {}, "openmc": {}}

        for solver, python_exec in [("openmoc", args.openmoc_python), ("openmc", args.openmc_python)]:
            print(f"# {solver}: running source ...", file=sys.stderr)
            results[solver]["source"] = run_solver(solver, python_exec, SOURCE_CASE, tmp / f"out-{solver}-source.json")

            print(f"# {solver}: running follow-up (ScaleNuSigmaF) ...", file=sys.stderr)
            results[solver]["nu_sigma_f"] = run_solver(solver, python_exec, followup_nu, tmp / f"out-{solver}-nu.json")

            print(f"# {solver}: running follow-up (ScaleFuelSigmaA) ...", file=sys.stderr)
            results[solver]["sigma_a"] = run_solver(solver, python_exec, followup_sa, tmp / f"out-{solver}-sa.json")

        # Build markdown report
        lines = []
        lines.append("# OpenMOC vs OpenMC — cross-program MR comparison")
        lines.append("")
        lines.append(f"Test case: 2D pin-cell, 2-group MGXS, reflective boundaries "
                     f"(`{SOURCE_CASE.relative_to(REPO_ROOT)}`).")
        lines.append("")
        lines.append(f"MR factor: **{factor}**")
        lines.append("")
        lines.append("## k_eff results")
        lines.append("")
        lines.append("| Case | OpenMOC | OpenMC | OpenMC − OpenMOC | (OpenMC − OpenMOC) / OpenMOC |")
        lines.append("|------|---------|--------|-------------------|------------------------------|")
        for case_id, label in [("source", "Source"),
                               ("nu_sigma_f", f"ScaleNuSigmaF (factor={factor})"),
                               ("sigma_a", f"ScaleFuelSigmaA (factor={factor})")]:
            moc = results["openmoc"][case_id]["k_eff"]
            mc = results["openmc"][case_id]["k_eff"]
            mc_std = results["openmc"][case_id].get("k_eff_std", 0.0)
            diff = mc - moc
            rel = diff / moc if moc != 0 else float("nan")
            mc_str = format_keff(results["openmc"][case_id])
            lines.append(f"| {label} | {moc:.5f} | {mc_str} | {diff:+.5f} | {rel:+.4%} |")
        lines.append("")

        lines.append("## MR direction verification")
        lines.append("")
        lines.append("| Solver | ScaleNuSigmaF (k_followup > k_source) | ScaleFuelSigmaA (k_followup < k_source) |")
        lines.append("|--------|---------------------------------------|-----------------------------------------|")
        for solver in ("openmoc", "openmc"):
            ks = results[solver]["source"]["k_eff"]
            kn = results[solver]["nu_sigma_f"]["k_eff"]
            ka = results[solver]["sigma_a"]["k_eff"]
            nu_pass = "✅ PASS" if kn > ks else "❌ FAIL"
            sa_pass = "✅ PASS" if ka < ks else "❌ FAIL"
            lines.append(f"| {solver} | {nu_pass} ({kn:.5f} > {ks:.5f}) | {sa_pass} ({ka:.5f} < {ks:.5f}) |")
        lines.append("")

        lines.append("## Cross-solver consistency")
        lines.append("")
        lines.append("Both solvers should respond to the MR in the same direction. Compare")
        lines.append("the k_eff ratios `k_followup / k_source` per solver:")
        lines.append("")
        lines.append("| MR | OpenMOC ratio | OpenMC ratio | Δratio |")
        lines.append("|----|---------------|--------------|--------|")
        for case_id, label in [("nu_sigma_f", "ScaleNuSigmaF"), ("sigma_a", "ScaleFuelSigmaA")]:
            moc_ratio = results["openmoc"][case_id]["k_eff"] / results["openmoc"]["source"]["k_eff"]
            mc_ratio = results["openmc"][case_id]["k_eff"] / results["openmc"]["source"]["k_eff"]
            lines.append(f"| {label} | {moc_ratio:.5f} | {mc_ratio:.5f} | {mc_ratio - moc_ratio:+.5f} |")
        lines.append("")

        lines.append("## Raw outputs")
        lines.append("")
        lines.append("```json")
        lines.append(json.dumps(results, indent=2, default=str))
        lines.append("```")

        report = "\n".join(lines)
        if args.output:
            Path(args.output).parent.mkdir(parents=True, exist_ok=True)
            Path(args.output).write_text(report, encoding="utf-8")
            print(f"Wrote {args.output}", file=sys.stderr)
        else:
            print(report)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
