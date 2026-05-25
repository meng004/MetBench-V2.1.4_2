"""1D linear advection solver (SUT for system MT).

Solves the 1D linear advection equation with periodic boundaries:

    du/dt + v * du/dx = 0,   x in [0, L],   periodic in x

The analytic solution is pure translation: u(x, t) = u0(x - v*t mod L).

Discretised with the first-order upwind finite-difference scheme on a uniform
mesh (num_points nodes, periodic so node N is identified with node 0):

    u_i^{n+1} = u_i^n - C * (u_i^n - u_{i-1}^n)         (for v > 0)
    u_i^{n+1} = u_i^n - C * (u_{i+1}^n - u_i^n)         (for v < 0)

where the Courant number is C = v * dt / dx. The scheme is exactly mass-
conservative for periodic boundaries (the per-step integral sum_i u_i*dx is
preserved to floating-point precision regardless of dx).

The time step dt is chosen internally to give Courant 0.5 (well under the
CFL stability limit of 1.0) using the user-supplied num_steps as an upper
bound; if num_steps is larger than what 0.5 CFL with t_final demands, the
extra steps are still taken but each at the same dt — t_final stays a hard
ceiling.

Initial condition: Gaussian pulse centered at `initial.center` with width
`initial.sigma` and peak height `initial.amplitude`:

    u0(x) = amplitude * exp(-(x - center)^2 / (2 * sigma^2))

Output observables:

  - peak_amplitude   maximum of u at the final time
  - peak_location    x-coordinate of the maximum
  - mass_integral    trapezoidal sum_i u_i * dx (periodic, no endpoint half-weight)
  - num_points       echo of input
  - num_steps        echo of input
  - dt               internally selected time step (Courant = 0.5)

Invocation:

    python advection_1d.py --input case.json --output result.json

Input JSON shape:

    {
      "geometry": {
        "length":     1.0,
        "num_points": 200
      },
      "initial": {
        "amplitude": 1.0,
        "center":    0.25,
        "sigma":     0.05
      },
      "advection": {
        "speed": 1.0
      },
      "time": {
        "t_final":   0.5,
        "num_steps": 400
      }
    }

Output JSON shape:

    {
      "peak_amplitude": <max u(x, t_final)>,
      "peak_location":  <x at the maximum>,
      "mass_integral":  <sum_i u_i * dx>,
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
    adv = case["advection"]
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

    v = float(adv["speed"])
    if v == 0:
        raise ValueError("advection.speed must be non-zero")

    t_final = float(t["t_final"])
    num_steps = int(t["num_steps"])
    if t_final <= 0 or num_steps < 1:
        raise ValueError("t_final must be > 0 and num_steps must be >= 1")

    dx = L / n  # periodic: N intervals over N nodes
    # CFL stability: |v|*dt/dx <= 1. Use Courant = 0.5 by default for safety.
    cfl_dt = 0.5 * dx / abs(v)
    # Force num_steps to at least span t_final at the CFL-safe dt:
    n_steps = max(num_steps, int(math.ceil(t_final / cfl_dt)))
    dt = t_final / n_steps
    courant = v * dt / dx

    # Initial condition: Gaussian pulse.
    xs = [i * dx for i in range(n)]
    u = [amplitude * math.exp(-((x - center) ** 2) / (2.0 * sigma * sigma)) for x in xs]

    # Time stepping: first-order upwind, periodic.
    if courant >= 0:
        for _ in range(n_steps):
            u_new = [0.0] * n
            for i in range(n):
                im = (i - 1) % n
                u_new[i] = u[i] - courant * (u[i] - u[im])
            u = u_new
    else:
        for _ in range(n_steps):
            u_new = [0.0] * n
            for i in range(n):
                ip = (i + 1) % n
                u_new[i] = u[i] - courant * (u[ip] - u[i])
            u = u_new

    # Observables.
    peak_amplitude = max(u)
    peak_idx = u.index(peak_amplitude)
    peak_location = xs[peak_idx]
    # Periodic trapezoidal (no half-weight at endpoints; all cells share dx).
    mass_integral = dx * sum(u)

    return {
        "peak_amplitude": peak_amplitude,
        "peak_location": peak_location,
        "mass_integral": mass_integral,
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
