"""Pure-stdlib Lorenz runtime slice for Minimum-MR-SubSet P3."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path


def _rhs(state: tuple[float, float, float], sigma: float, rho: float, beta: float) -> tuple[float, float, float]:
    x, y, z = state
    return (
        sigma * (y - x),
        x * (rho - z) - y,
        x * y - beta * z,
    )


def _rk4_step(
    state: tuple[float, float, float],
    dt: float,
    sigma: float,
    rho: float,
    beta: float,
) -> tuple[float, float, float]:
    k1 = _rhs(state, sigma, rho, beta)
    k2 = _rhs(tuple(state[i] + 0.5 * dt * k1[i] for i in range(3)), sigma, rho, beta)
    k3 = _rhs(tuple(state[i] + 0.5 * dt * k2[i] for i in range(3)), sigma, rho, beta)
    k4 = _rhs(tuple(state[i] + dt * k3[i] for i in range(3)), sigma, rho, beta)
    return tuple(state[i] + (dt / 6.0) * (k1[i] + 2.0 * k2[i] + 2.0 * k3[i] + k4[i]) for i in range(3))


def _integrate(
    state: tuple[float, float, float],
    steps: int,
    dt: float,
    sigma: float,
    rho: float,
    beta: float,
) -> tuple[tuple[float, float, float], tuple[float, float, float]]:
    sx = sy = sz = 0.0
    current = state
    for _ in range(steps):
        current = _rk4_step(current, dt, sigma, rho, beta)
        sx += current[0]
        sy += current[1]
        sz += current[2]
    return current, (sx / steps, sy / steps, sz / steps)


def solve(case: dict) -> dict:
    params = case["parameters"]
    initial = case["initial"]
    integration = case["integration"]
    sigma = float(params["sigma"])
    rho = float(params["rho"])
    beta = float(params["beta"])
    steps = int(integration["steps"])
    dt = float(integration["dt"])
    perturbation = float(initial["perturbation"])
    if steps < 1:
        raise ValueError("integration.steps must be >= 1")
    if dt <= 0:
        raise ValueError("integration.dt must be > 0")

    base = (float(initial["x"]), float(initial["y"]), float(initial["z"]))
    perturbed = (base[0] + perturbation, base[1], base[2])
    base_final, base_centroid = _integrate(base, steps, dt, sigma, rho, beta)
    perturbed_final, perturbed_centroid = _integrate(perturbed, steps, dt, sigma, rho, beta)
    separation = math.dist(base_final, perturbed_final)

    return {
        "base_final": list(base_final),
        "perturbed_final": list(perturbed_final),
        "centroid": list(perturbed_centroid),
        "base_centroid": list(base_centroid),
        "separation": separation,
        "perturbation": perturbation,
        "steps": steps,
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
