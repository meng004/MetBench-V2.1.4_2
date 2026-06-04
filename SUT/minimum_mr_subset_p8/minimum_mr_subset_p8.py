"""Pure-stdlib Schrodinger norm-conservation runtime slice for Minimum-MR-SubSet P8."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def solve(case: dict) -> dict:
    wave = case["wave"]
    solver = case["solver"]
    initial_norm = float(wave["initial_norm"])
    drift_constant = float(wave["drift_constant"])
    time_steps = int(solver["time_steps"])
    if initial_norm <= 0:
        raise ValueError("wave.initial_norm must be > 0")
    if drift_constant < 0:
        raise ValueError("wave.drift_constant must be >= 0")
    if time_steps < 1:
        raise ValueError("solver.time_steps must be >= 1")

    norm_drift = drift_constant / time_steps
    norm_final = initial_norm + norm_drift
    return {
        "norm_initial": initial_norm,
        "norm_final": norm_final,
        "norm_drift": norm_drift,
        "time_steps": time_steps,
        "probability_density_l1": norm_final,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    case = json.loads(Path(args.input).read_text(encoding="utf-8"))
    result = solve(case)
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
