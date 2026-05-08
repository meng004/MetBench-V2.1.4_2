"""1D heat-equation finite-difference solver (system under test).

Solves the 1D heat equation

    u_t = alpha * u_xx,    x in [x_min, x_max],   t in [0, t_final]

with homogeneous Dirichlet boundary conditions (u = 0 at both endpoints) using
forward Euler in time and central differences in space. The choice of zero
boundaries is deliberate: it makes the PDE linear in the initial profile, which
is what the MetBench heat-equation MR (amplitude scaling) relies on.

Stability requires `r = alpha * dt / dx**2 <= 0.5`. The solver checks this and
records the verdict; an unstable run still produces output (so the parser sees
a result), but the metadata flags `stability_violated=true` and the output
adapter surfaces it.

Invocation:

    python heat_equation.py --input case.json --output result.json

Input JSON shape (what the input adapter writes):

    {
      "initial": {
        "x_min":      0.0,
        "x_max":      1.0,
        "num_points": 101,
        "profile":    "gaussian" | "box" | "sin",
        "amplitude":  1.0,
        "center":     0.5,    # gaussian only
        "width":      0.1,    # gaussian, box
        "mode":       1       # sin only
      },
      "params": {
        "alpha":     0.01,
        "t_final":   0.5,
        "num_steps": 5000
      }
    }

Output JSON shape:

    {
      "t_final":             0.5,
      "max_u":               <peak value at t_final>,
      "min_u":               <minimum value at t_final>,
      "l2_norm":             <discrete L2 norm of u(t_final)>,
      "integral":            <int u(x, t_final) dx via trapezoid>,
      "num_steps":           5000,
      "stability_ratio":     <alpha * dt / dx**2>,
      "stability_violated":  false,
      "x":                   [...],
      "u":                   [...]
    }
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any


def _build_initial_profile(initial: dict[str, Any]) -> tuple[list[float], list[float]]:
    x_min = float(initial["x_min"])
    x_max = float(initial["x_max"])
    n = int(initial["num_points"])
    if n < 5:
        raise ValueError(f"num_points must be >= 5 (got {n})")
    if x_max <= x_min:
        raise ValueError(f"x_max must be > x_min (got {x_min}, {x_max})")

    dx = (x_max - x_min) / (n - 1)
    x = [x_min + i * dx for i in range(n)]

    profile = initial.get("profile", "gaussian")
    amplitude = float(initial["amplitude"])

    if profile == "gaussian":
        center = float(initial.get("center", 0.5 * (x_min + x_max)))
        width = float(initial.get("width", 0.1 * (x_max - x_min)))
        u = [amplitude * math.exp(-((xi - center) ** 2) / (2.0 * width * width)) for xi in x]
    elif profile == "box":
        center = float(initial.get("center", 0.5 * (x_min + x_max)))
        width = float(initial.get("width", 0.2 * (x_max - x_min)))
        half = 0.5 * width
        u = [amplitude if (center - half) <= xi <= (center + half) else 0.0 for xi in x]
    elif profile == "sin":
        mode = int(initial.get("mode", 1))
        L = x_max - x_min
        u = [amplitude * math.sin(mode * math.pi * (xi - x_min) / L) for xi in x]
    else:
        raise ValueError(f"unknown profile '{profile}'")

    # Enforce homogeneous Dirichlet BCs: zero out endpoints.
    u[0] = 0.0
    u[-1] = 0.0
    return x, u


def _step_forward_euler(u: list[float], dx: float, dt: float, alpha: float) -> list[float]:
    n = len(u)
    new_u = [0.0] * n
    coeff = alpha * dt / (dx * dx)
    # Boundary points stay at zero (homogeneous Dirichlet).
    for i in range(1, n - 1):
        new_u[i] = u[i] + coeff * (u[i + 1] - 2.0 * u[i] + u[i - 1])
    return new_u


def _trapezoid_integral(x: list[float], u: list[float]) -> float:
    total = 0.0
    for i in range(len(x) - 1):
        total += 0.5 * (u[i] + u[i + 1]) * (x[i + 1] - x[i])
    return total


def _l2_norm(u: list[float], dx: float) -> float:
    return math.sqrt(sum(v * v for v in u) * dx)


def solve(case: dict[str, Any]) -> dict[str, Any]:
    initial = case["initial"]
    params = case["params"]

    x, u = _build_initial_profile(initial)
    dx = x[1] - x[0]
    alpha = float(params["alpha"])
    t_final = float(params["t_final"])
    num_steps = int(params["num_steps"])
    if num_steps < 1:
        raise ValueError(f"num_steps must be >= 1 (got {num_steps})")
    if alpha <= 0:
        raise ValueError(f"alpha must be > 0 (got {alpha})")
    if t_final <= 0:
        raise ValueError(f"t_final must be > 0 (got {t_final})")

    dt = t_final / num_steps
    stability_ratio = alpha * dt / (dx * dx)
    stability_violated = stability_ratio > 0.5

    for _ in range(num_steps):
        u = _step_forward_euler(u, dx, dt, alpha)

    return {
        "t_final": t_final,
        "max_u": max(u),
        "min_u": min(u),
        "l2_norm": _l2_norm(u, dx),
        "integral": _trapezoid_integral(x, u),
        "num_steps": num_steps,
        "stability_ratio": stability_ratio,
        "stability_violated": stability_violated,
        "x": x,
        "u": u,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="1D heat-equation finite-difference solver")
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    case = json.loads(Path(args.input).read_text(encoding="utf-8"))
    result = solve(case)
    Path(args.output).parent.mkdir(parents=True, exist_ok=True)
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
