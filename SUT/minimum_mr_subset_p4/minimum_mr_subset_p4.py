"""Pure-stdlib Hamiltonian pendulum SUT for Minimum-MR-SubSet P4."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path


def _energy(q: float, p: float) -> float:
    return 0.5 * p * p + 1.0 - math.cos(q)


def solve(case: dict) -> dict:
    initial = case["initial"]
    integration = case["integration"]
    q = float(initial["q"])
    p = float(initial["p"])
    n_steps = int(integration["n_steps"])
    t_final = float(integration["t_final"])
    if n_steps < 1:
        raise ValueError("integration.n_steps must be >= 1")
    if t_final <= 0:
        raise ValueError("integration.t_final must be > 0")

    dt = t_final / n_steps
    energies = [_energy(q, p)]

    # Velocity Verlet for q'' = -sin(q).
    for _ in range(n_steps):
        p_half = p - 0.5 * dt * math.sin(q)
        q = q + dt * p_half
        p = p_half - 0.5 * dt * math.sin(q)
        energies.append(_energy(q, p))

    return {
        "q": q,
        "p": p,
        "energy_initial": energies[0],
        "energy_final": energies[-1],
        "energy_min": min(energies),
        "energy_max": max(energies),
        "energy_drift": max(energies) - min(energies),
        "n_steps": n_steps,
        "dt": dt,
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
