"""Lotka-Volterra predator-prey solver (system under test).

Solves the nonlinear ODE system

    dx/dt = alpha*x - beta*x*y      (prey)
    dy/dt = delta*x*y - gamma*y     (predator)

A classic result for the Lotka-Volterra cycle is that the time-averaged
populations equal the coordinates of the coexistence fixed point:

    <prey>     = gamma / delta
    <predator> = alpha / beta

The MetBench Lotka-Volterra MR exploits the first identity: scaling the
predator death rate `gamma` by a factor scales the time-averaged prey
population by the same factor, so `mean_prey` is monotone in `gamma`.

Integrated with a fixed-step classical RK4 (stdlib only, no scipy/numpy
dependency). The time averages use the trapezoid rule along the trajectory.

Invocation:

    python lotka_volterra.py --input case.json --output result.json

Input JSON shape (what the input adapter writes):

    {
      "initial": { "prey": 10.0, "predator": 10.0 },
      "params":  { "alpha": 1.1, "beta": 0.4, "delta": 0.1, "gamma": 0.4,
                   "t_final": 100.0, "num_steps": 20000 }
    }

Output JSON shape:

    {
      "t_final":         100.0,
      "mean_prey":       <time-averaged prey>,
      "mean_predator":   <time-averaged predator>,
      "peak_prey":       <max prey over the run>,
      "peak_predator":   <max predator over the run>,
      "prey_final":      <prey at t_final>,
      "predator_final":  <predator at t_final>,
      "num_steps":       20000
    }
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


def _rhs(state: list[float], a: float, b: float, d: float, g: float) -> list[float]:
    x, y = state
    return [a * x - b * x * y, d * x * y - g * y]


def _rk4_step(state: list[float], dt: float,
              a: float, b: float, d: float, g: float) -> list[float]:
    k1 = _rhs(state, a, b, d, g)
    s2 = [state[i] + 0.5 * dt * k1[i] for i in range(2)]
    k2 = _rhs(s2, a, b, d, g)
    s3 = [state[i] + 0.5 * dt * k2[i] for i in range(2)]
    k3 = _rhs(s3, a, b, d, g)
    s4 = [state[i] + dt * k3[i] for i in range(2)]
    k4 = _rhs(s4, a, b, d, g)
    return [
        state[i] + (dt / 6.0) * (k1[i] + 2.0 * k2[i] + 2.0 * k3[i] + k4[i])
        for i in range(2)
    ]


def solve(case: dict[str, Any]) -> dict[str, Any]:
    initial = case["initial"]
    params = case["params"]

    state = [float(initial["prey"]), float(initial["predator"])]
    if min(state) <= 0.0:
        raise ValueError("initial prey and predator must be > 0")

    a = float(params["alpha"])
    b = float(params["beta"])
    d = float(params["delta"])
    g = float(params["gamma"])
    t_final = float(params["t_final"])
    num_steps = int(params["num_steps"])
    if min(a, b, d, g) <= 0:
        raise ValueError("alpha, beta, delta, gamma must all be > 0")
    if t_final <= 0:
        raise ValueError(f"t_final must be > 0 (got {t_final})")
    if num_steps < 1:
        raise ValueError(f"num_steps must be >= 1 (got {num_steps})")

    dt = t_final / num_steps
    prey_integral = 0.0
    pred_integral = 0.0
    peak_prey = state[0]
    peak_pred = state[1]
    for _ in range(num_steps):
        prev = state
        state = _rk4_step(state, dt, a, b, d, g)
        prey_integral += 0.5 * (prev[0] + state[0]) * dt
        pred_integral += 0.5 * (prev[1] + state[1]) * dt
        peak_prey = max(peak_prey, state[0])
        peak_pred = max(peak_pred, state[1])

    return {
        "t_final": t_final,
        "mean_prey": prey_integral / t_final,
        "mean_predator": pred_integral / t_final,
        "peak_prey": peak_prey,
        "peak_predator": peak_pred,
        "prey_final": state[0],
        "predator_final": state[1],
        "num_steps": num_steps,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Lotka-Volterra predator-prey solver")
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
