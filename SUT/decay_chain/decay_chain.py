"""Radioactive decay-chain solver (system under test).

Solves the 3-nuclide linear decay chain A -> B -> C (Bateman equations):

    dN_A/dt = -lambda_A * N_A
    dN_B/dt =  lambda_A * N_A - lambda_B * N_B
    dN_C/dt =  lambda_B * N_B

C is the stable end nuclide. The system is **linear in the initial
conditions**, which is what the MetBench decay-chain MR (initial-quantity
scaling) relies on: scaling every initial N by a factor scales every N(t) by
the same factor, so any output quantity is monotone in the scaling factor.

Integrated with a fixed-step classical RK4 (stdlib only, no scipy/numpy
dependency -- matches the heat-equation SUT's zero-dependency style).

Invocation:

    python decay_chain.py --input case.json --output result.json

Input JSON shape (what the input adapter writes):

    {
      "initial": { "N_A": 1000.0, "N_B": 0.0, "N_C": 0.0 },
      "params":  { "lambda_A": 0.5, "lambda_B": 0.2,
                   "t_final": 10.0, "num_steps": 2000 }
    }

Output JSON shape:

    {
      "t_final":      10.0,
      "N_A_final":    <A remaining at t_final>,
      "N_B_final":    <B remaining at t_final>,
      "N_C_final":    <C accumulated at t_final>,
      "N_B_peak":     <peak B over the run>,
      "total":        <N_A + N_B + N_C at t_final; conserved>,
      "num_steps":    2000
    }
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


def _rhs(state: list[float], lam_a: float, lam_b: float) -> list[float]:
    n_a, n_b, n_c = state
    return [
        -lam_a * n_a,
        lam_a * n_a - lam_b * n_b,
        lam_b * n_b,
    ]


def _rk4_step(state: list[float], dt: float, lam_a: float, lam_b: float) -> list[float]:
    k1 = _rhs(state, lam_a, lam_b)
    s2 = [state[i] + 0.5 * dt * k1[i] for i in range(3)]
    k2 = _rhs(s2, lam_a, lam_b)
    s3 = [state[i] + 0.5 * dt * k2[i] for i in range(3)]
    k3 = _rhs(s3, lam_a, lam_b)
    s4 = [state[i] + dt * k3[i] for i in range(3)]
    k4 = _rhs(s4, lam_a, lam_b)
    return [
        state[i] + (dt / 6.0) * (k1[i] + 2.0 * k2[i] + 2.0 * k3[i] + k4[i])
        for i in range(3)
    ]


def solve(case: dict[str, Any]) -> dict[str, Any]:
    initial = case["initial"]
    params = case["params"]

    state = [float(initial["N_A"]), float(initial["N_B"]), float(initial["N_C"])]
    if min(state) < 0.0:
        raise ValueError("initial quantities must be >= 0")

    lam_a = float(params["lambda_A"])
    lam_b = float(params["lambda_B"])
    t_final = float(params["t_final"])
    num_steps = int(params["num_steps"])
    if lam_a <= 0 or lam_b <= 0:
        raise ValueError("decay constants must be > 0")
    if t_final <= 0:
        raise ValueError(f"t_final must be > 0 (got {t_final})")
    if num_steps < 1:
        raise ValueError(f"num_steps must be >= 1 (got {num_steps})")

    dt = t_final / num_steps
    n_b_peak = state[1]
    for _ in range(num_steps):
        state = _rk4_step(state, dt, lam_a, lam_b)
        n_b_peak = max(n_b_peak, state[1])

    return {
        "t_final": t_final,
        "N_A_final": state[0],
        "N_B_final": state[1],
        "N_C_final": state[2],
        "N_B_peak": n_b_peak,
        "total": state[0] + state[1] + state[2],
        "num_steps": num_steps,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Radioactive decay-chain solver")
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
