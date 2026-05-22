"""Damped harmonic oscillator solver (system under test).

Solves the linear 2nd-order ODE

    x'' + 2*zeta*omega*x' + omega**2 * x = 0

written as a first-order system

    dx/dt = v
    dv/dt = -2*zeta*omega*v - omega**2 * x

The equation is **linear and homogeneous in the initial state (x0, v0)**: the
response x(t) scales by the same factor as the initial state. This is what the
MetBench damped-oscillator MR (initial-state scaling) relies on -- the peak
absolute displacement is monotone in the scaling factor.

Integrated with a fixed-step classical RK4 (stdlib only, no scipy/numpy
dependency).

Invocation:

    python damped_oscillator.py --input case.json --output result.json

Input JSON shape (what the input adapter writes):

    {
      "initial": { "x0": 1.0, "v0": 0.0 },
      "params":  { "omega": 2.0, "zeta": 0.1,
                   "t_final": 20.0, "num_steps": 4000 }
    }

Output JSON shape:

    {
      "t_final":           20.0,
      "x_final":           <displacement at t_final>,
      "v_final":           <velocity at t_final>,
      "max_abs_displacement": <peak |x| over the run>,
      "energy_final":      <0.5*v**2 + 0.5*omega**2*x**2 at t_final>,
      "num_steps":         4000
    }
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


def _rhs(state: list[float], omega: float, zeta: float) -> list[float]:
    x, v = state
    return [v, -2.0 * zeta * omega * v - omega * omega * x]


def _rk4_step(state: list[float], dt: float, omega: float, zeta: float) -> list[float]:
    k1 = _rhs(state, omega, zeta)
    s2 = [state[i] + 0.5 * dt * k1[i] for i in range(2)]
    k2 = _rhs(s2, omega, zeta)
    s3 = [state[i] + 0.5 * dt * k2[i] for i in range(2)]
    k3 = _rhs(s3, omega, zeta)
    s4 = [state[i] + dt * k3[i] for i in range(2)]
    k4 = _rhs(s4, omega, zeta)
    return [
        state[i] + (dt / 6.0) * (k1[i] + 2.0 * k2[i] + 2.0 * k3[i] + k4[i])
        for i in range(2)
    ]


def solve(case: dict[str, Any]) -> dict[str, Any]:
    initial = case["initial"]
    params = case["params"]

    state = [float(initial["x0"]), float(initial["v0"])]
    omega = float(params["omega"])
    zeta = float(params["zeta"])
    t_final = float(params["t_final"])
    num_steps = int(params["num_steps"])
    if omega <= 0:
        raise ValueError(f"omega must be > 0 (got {omega})")
    if zeta < 0:
        raise ValueError(f"zeta must be >= 0 (got {zeta})")
    if t_final <= 0:
        raise ValueError(f"t_final must be > 0 (got {t_final})")
    if num_steps < 1:
        raise ValueError(f"num_steps must be >= 1 (got {num_steps})")

    dt = t_final / num_steps
    max_abs = abs(state[0])
    for _ in range(num_steps):
        state = _rk4_step(state, dt, omega, zeta)
        max_abs = max(max_abs, abs(state[0]))

    x, v = state
    return {
        "t_final": t_final,
        "x_final": x,
        "v_final": v,
        "max_abs_displacement": max_abs,
        "energy_final": 0.5 * v * v + 0.5 * omega * omega * x * x,
        "num_steps": num_steps,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Damped harmonic oscillator solver")
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
