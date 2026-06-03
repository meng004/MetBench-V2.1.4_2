"""Deterministic OpenMC surrogate for Minimum-MR-SubSet P9.

This is not a real OpenMC execution path. It preserves the imported P9
canonical relation with a pure-stdlib formula so launcher runtime evidence can
exercise MetBench transform/assertion binding without external OpenMC.
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path


def solve(case: dict) -> dict:
    surrogate = case["surrogate"]
    simulation = case["simulation"]
    enrichment = float(surrogate["enrichment"])
    particles = int(simulation["particles"])
    if particles <= 0:
        raise ValueError("simulation.particles must be > 0")

    sigma_k = 0.75 / math.sqrt(particles)
    k_values = [
        0.98 + 4.0 * ((enrichment * factor) - 0.04)
        for factor in (0.9, 1.0, 1.1)
    ]
    k_eff = k_values[1]
    reaction_balance = k_eff / max(k_values)
    return {
        "k_eff": k_eff,
        "sigma_k": sigma_k,
        "reaction_balance": reaction_balance,
        "particles": particles,
        "enrichment": enrichment,
        "surrogate": True,
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
