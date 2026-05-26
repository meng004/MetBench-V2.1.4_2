"""SciPy `solve_ivp`-backed Lotka-Volterra predator-prey solver (system under test).

T3C-IVP external-solver pilot. Same ODE system and time-average identities as the
pure-stdlib `SUT/lotka_volterra/lotka_volterra.py`, but integrated by SciPy's
adaptive `scipy.integrate.solve_ivp` (default RK45) instead of a fixed-step RK4.

    dx/dt = alpha*x - beta*x*y      (prey)
    dy/dt = delta*x*y - gamma*y     (predator)

The classic Lotka-Volterra cycle identity used by the MRs:

    <prey>     = gamma / delta
    <predator> = alpha / beta

Time averages are evaluated by trapezoidal integration over the `t_eval` grid that
`solve_ivp` populates (size = `num_eval_points`). The continuous trajectory is
adaptive; the eval grid only controls the output resolution. Doubling
`num_eval_points` refines the trapezoidal average for the SAME continuous
trajectory and must agree to within tolerance — see MR `scipy-ivp-lv-step-convergence`.

Invocation:

    python scipy_ivp_lotka_volterra.py --input case.json --output result.json

Input JSON shape:

    {
      "initial": { "prey": 10.0, "predator": 10.0 },
      "params":  { "alpha": 1.1, "beta": 0.4, "delta": 0.1, "gamma": 0.4,
                   "t_final": 100.0, "num_eval_points": 1000 }
    }

Output JSON shape:

    {
      "t_final":          100.0,
      "mean_prey":        <trapezoidal time-average of prey on the eval grid>,
      "mean_predator":    <same for predator>,
      "peak_prey":        <max prey on the eval grid>,
      "peak_predator":    <max predator on the eval grid>,
      "prey_final":       <prey at t_final>,
      "predator_final":   <predator at t_final>,
      "num_eval_points":  1000
    }
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


def _rhs(t: float, state, alpha: float, beta: float, delta: float, gamma: float):
    x, y = state
    return [alpha * x - beta * x * y, delta * x * y - gamma * y]


def solve(case: dict[str, Any]) -> dict[str, Any]:
    # Imported lazily so module import does not fail when scipy is absent
    # (parsers under SUT/scipy_ivp_lotka_volterra/ are scipy-free and must keep
    # working even on a system without scipy).
    import numpy as np
    from scipy.integrate import solve_ivp, trapezoid

    initial = case["initial"]
    params = case["params"]

    prey0 = float(initial["prey"])
    predator0 = float(initial["predator"])
    if min(prey0, predator0) <= 0.0:
        raise ValueError("initial prey and predator must be > 0")

    alpha = float(params["alpha"])
    beta = float(params["beta"])
    delta = float(params["delta"])
    gamma = float(params["gamma"])
    t_final = float(params["t_final"])
    num_eval_points = int(params["num_eval_points"])
    if min(alpha, beta, delta, gamma) <= 0:
        raise ValueError("alpha, beta, delta, gamma must all be > 0")
    if t_final <= 0:
        raise ValueError(f"t_final must be > 0 (got {t_final})")
    if num_eval_points < 2:
        raise ValueError(f"num_eval_points must be >= 2 (got {num_eval_points})")

    t_eval = np.linspace(0.0, t_final, num_eval_points)
    sol = solve_ivp(
        fun=lambda t, y: _rhs(t, y, alpha, beta, delta, gamma),
        t_span=(0.0, t_final),
        y0=[prey0, predator0],
        t_eval=t_eval,
        rtol=1e-9,
        atol=1e-12,
        method="RK45",
    )
    if not sol.success:
        raise RuntimeError(f"solve_ivp failed: {sol.message}")

    prey = sol.y[0]
    predator = sol.y[1]

    mean_prey = float(trapezoid(prey, t_eval) / t_final)
    mean_predator = float(trapezoid(predator, t_eval) / t_final)

    return {
        "t_final": t_final,
        "mean_prey": mean_prey,
        "mean_predator": mean_predator,
        "peak_prey": float(prey.max()),
        "peak_predator": float(predator.max()),
        "prey_final": float(prey[-1]),
        "predator_final": float(predator[-1]),
        "num_eval_points": num_eval_points,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="SciPy solve_ivp Lotka-Volterra solver")
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
