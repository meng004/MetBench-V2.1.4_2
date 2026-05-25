"""1D wave equation solver (SUT for system MT).

Solves the 1D linear wave equation with homogeneous Dirichlet boundaries:

    u_tt = c^2 * u_xx,   x in [0, L],   u(0, t) = u(L, t) = 0,
    u(x, 0) = u0(x),     u_t(x, 0) = 0   (zero initial velocity)

Discretised with the standard explicit second-order central-difference
"leapfrog" scheme on a uniform spatial mesh (num_points nodes, including
both boundary nodes):

    u_i^{n+1} = 2 u_i^n - u_i^{n-1}
              + C^2 * (u_{i+1}^n - 2 u_i^n + u_{i-1}^n)

where the Courant number is C = c * dt / dx. The scheme is stable for C <= 1.

The first time step uses the symmetric initialiser derived from u_t(x, 0) = 0:

    u_i^1 = u_i^0 + (C^2 / 2) * (u_{i+1}^0 - 2 u_i^0 + u_{i-1}^0)

This is the standard half-step recipe for the zero-initial-velocity case and
preserves second-order accuracy without extra storage.

The time step dt is selected internally to give Courant 0.5 (well under the
CFL stability limit of 1.0) using the user-supplied num_steps as a lower
bound; if num_steps is smaller than what 0.5 CFL with t_final demands, the
larger step count is used. t_final stays a hard ceiling.

Initial condition: Gaussian pulse centered at `initial.center` with width
`initial.sigma` and peak height `initial.amplitude`:

    u0(x) = amplitude * exp(-(x - center)^2 / (2 * sigma^2))

Output observables:

  - peak_amplitude   maximum |u| at the final time (signed peak by largest |u|)
  - peak_location    x-coordinate of the absolute-value maximum
  - energy_proxy     0.5 * sum_i (u_i^2) * dx  -- proxy for the kinetic+strain
                     energy; with zero initial velocity and Dirichlet BC the
                     total dynamical energy is bounded and oscillates around
                     the initial-state value as the wave evolves
  - num_points       echo of input
  - num_steps        time steps actually taken
  - dt               internally selected time step (Courant = 0.5)

Invocation:

    python wave_1d.py --input case.json --output result.json

Input JSON shape:

    {
      "geometry": {
        "length":     1.0,
        "num_points": 201
      },
      "initial": {
        "amplitude": 1.0,
        "center":    0.5,
        "sigma":     0.05
      },
      "wave": {
        "speed": 1.0
      },
      "time": {
        "t_final":   0.25,
        "num_steps": 400
      }
    }

Output JSON shape:

    {
      "peak_amplitude": <max |u(x, t_final)|>,
      "peak_location":  <x at the abs-max>,
      "energy_proxy":   <0.5 * sum_i u_i^2 * dx>,
      "num_points":     <mesh nodes>,
      "num_steps":      <time steps actually taken>,
      "dt":             <internally selected time step>
    }
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path


def solve(case: dict) -> dict:
    geom = case["geometry"]
    ic = case["initial"]
    wave = case["wave"]
    t = case["time"]

    L = float(geom["length"])
    n = int(geom["num_points"])
    if n < 4:
        raise ValueError("num_points must be >= 4")
    if L <= 0:
        raise ValueError(f"length must be > 0 (got {L})")

    amplitude = float(ic["amplitude"])
    center = float(ic["center"])
    sigma = float(ic["sigma"])
    if sigma <= 0:
        raise ValueError(f"sigma must be > 0 (got {sigma})")

    c = float(wave["speed"])
    if c <= 0:
        raise ValueError("wave.speed must be > 0")

    t_final = float(t["t_final"])
    num_steps = int(t["num_steps"])
    if t_final <= 0 or num_steps < 1:
        raise ValueError("t_final must be > 0 and num_steps must be >= 1")

    dx = L / (n - 1)
    # CFL stability: c*dt/dx <= 1. Use Courant = 0.5 by default for safety.
    cfl_dt = 0.5 * dx / c
    # Force num_steps to span t_final at the CFL-safe dt:
    n_steps = max(num_steps, int(math.ceil(t_final / cfl_dt)))
    dt = t_final / n_steps
    courant = c * dt / dx
    c2 = courant * courant

    # Initial condition: Gaussian pulse, zero initial velocity.
    xs = [i * dx for i in range(n)]
    u_prev = [amplitude * math.exp(-((x - center) ** 2) / (2.0 * sigma * sigma)) for x in xs]
    # Enforce Dirichlet boundaries on the IC (Gaussian tails are tiny but be explicit):
    u_prev[0] = 0.0
    u_prev[-1] = 0.0

    # First step: u^1 from u^0 + zero initial velocity using the half-step recipe.
    u_curr = [0.0] * n
    for i in range(1, n - 1):
        u_curr[i] = u_prev[i] + 0.5 * c2 * (u_prev[i + 1] - 2.0 * u_prev[i] + u_prev[i - 1])
    u_curr[0] = 0.0
    u_curr[-1] = 0.0

    # Leapfrog time stepping for the remaining n_steps - 1 iterations.
    for _ in range(n_steps - 1):
        u_next = [0.0] * n
        for i in range(1, n - 1):
            u_next[i] = (
                2.0 * u_curr[i] - u_prev[i]
                + c2 * (u_curr[i + 1] - 2.0 * u_curr[i] + u_curr[i - 1])
            )
        # Dirichlet boundaries.
        u_next[0] = 0.0
        u_next[-1] = 0.0
        u_prev, u_curr = u_curr, u_next

    # Observables on u_curr (the final-time field).
    abs_u = [abs(v) for v in u_curr]
    peak_abs = max(abs_u)
    peak_idx = abs_u.index(peak_abs)
    peak_amplitude = u_curr[peak_idx]
    peak_location = xs[peak_idx]
    # Energy proxy: integral of u^2 over [0, L] using trapezoidal rule.
    sq = [v * v for v in u_curr]
    energy_proxy = 0.5 * dx * (0.5 * sq[0] + sum(sq[1:-1]) + 0.5 * sq[-1])

    return {
        "peak_amplitude": peak_amplitude,
        "peak_location": peak_location,
        "energy_proxy": energy_proxy,
        "num_points": n,
        "num_steps": n_steps,
        "dt": dt,
    }


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--input", required=True)
    p.add_argument("--output", required=True)
    args = p.parse_args()

    case = json.loads(Path(args.input).read_text(encoding="utf-8"))
    result = solve(case)
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
