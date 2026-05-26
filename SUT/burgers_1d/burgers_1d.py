"""1D inviscid Burgers solver (SUT for system MT).

Solves the 1D inviscid Burgers equation in conservative form with periodic
boundaries:

    du/dt + d/dx (u^2 / 2) = 0,   x in [0, L],   periodic in x

This is the canonical scalar nonlinear hyperbolic conservation law. Smooth
initial data self-steepens on the trailing edge (where du/dx > 0) and
develops shocks in finite time; once a shock forms, the entropy solution
loses regularity and the L2 norm strictly decreases (dissipation by the
shock) while the mass integral ∫u dx is conserved exactly under conservative
discretisation + periodic boundaries.

Discretised with the first-order Lax-Friedrichs flux on a uniform mesh
(num_points cells, periodic so cell N is identified with cell 0):

    F̂_{i+1/2} = (F_i + F_{i+1}) / 2 - (dx / (2 dt)) (u_{i+1} - u_i)
    u_i^{n+1} = u_i^n - (dt / dx) (F̂_{i+1/2} - F̂_{i-1/2})

where F(u) = u^2 / 2. Lax-Friedrichs is conservative (flux differencing),
monotone, and dissipative — it captures shocks without spurious oscillations
at the cost of smearing them over a few cells.

The time step dt is chosen internally to give Courant 0.5 (well under the
CFL stability limit) using the initial peak amplitude as a conservative
upper bound on |u| (LxF dissipation only decreases max|u|, so dt stays
stable for the whole run):

    dt = 0.5 * dx / max(amplitude, 1e-12)

The user-supplied num_steps is treated as a lower bound; the actual step
count is max(num_steps, ceil(t_final / dt)), keeping t_final as a hard ceiling.

Initial condition: positive Gaussian pulse centered at `initial.center` with
width `initial.sigma` and peak height `initial.amplitude`:

    u0(x) = amplitude * exp(-(x - center)^2 / (2 * sigma^2))

A strictly-positive Gaussian gives a non-zero mass integral (≈ amplitude *
sigma * sqrt(2 pi)) so the mass-conservation MR is a non-trivial assertion.

Output observables:

  - peak_amplitude   maximum of u at the final time
  - peak_location    x-coordinate of the maximum (after nonlinear translation)
  - mass_integral    sum_i u_i * dx (periodic, no endpoint half-weight)
  - num_points       echo of input
  - num_steps        time steps actually taken (>= input num_steps)
  - dt               internally selected time step (Courant = 0.5 at t=0)

Invocation:

    python burgers_1d.py --input case.json --output result.json

Input JSON shape:

    {
      "geometry": {
        "length":     1.0,
        "num_points": 200
      },
      "initial": {
        "amplitude": 1.0,
        "center":    0.5,
        "sigma":     0.1
      },
      "time": {
        "t_final":   0.1,
        "num_steps": 200
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

    t_final = float(t["t_final"])
    num_steps = int(t["num_steps"])
    if t_final <= 0 or num_steps < 1:
        raise ValueError("t_final must be > 0 and num_steps must be >= 1")

    dx = L / n  # periodic: N intervals over N cells
    # CFL: dt * max|u| / dx <= 1. Use Courant = 0.5 with initial max|u|
    # as the bound. LxF only dissipates so max|u| is non-increasing.
    cfl_dt = 0.5 * dx / max(abs(amplitude), 1e-12)
    n_steps = max(num_steps, int(math.ceil(t_final / cfl_dt)))
    dt = t_final / n_steps

    # Initial condition: positive Gaussian pulse.
    xs = [i * dx for i in range(n)]
    u = [amplitude * math.exp(-((x - center) ** 2) / (2.0 * sigma * sigma)) for x in xs]

    # Time stepping: Lax-Friedrichs, periodic.
    # F̂_{i+1/2} = (F_i + F_{i+1})/2 - (dx/(2 dt)) (u_{i+1} - u_i)
    # u_i^{n+1} = u_i^n - (dt/dx) (F̂_{i+1/2} - F̂_{i-1/2})
    alpha = dx / (2.0 * dt)
    for _ in range(n_steps):
        # Compute fluxes at cell faces i+1/2 for i = 0..n-1 (periodic: face n-1+1/2 wraps to 0).
        f = [0.5 * v * v for v in u]
        f_hat = [0.0] * n  # f_hat[i] = F̂_{i+1/2}
        for i in range(n):
            ip = (i + 1) % n
            f_hat[i] = 0.5 * (f[i] + f[ip]) - alpha * (u[ip] - u[i])
        u_new = [0.0] * n
        for i in range(n):
            im = (i - 1) % n
            u_new[i] = u[i] - (dt / dx) * (f_hat[i] - f_hat[im])
        u = u_new

    # Observables.
    peak_amplitude = max(u)
    peak_idx = u.index(peak_amplitude)
    peak_location = xs[peak_idx]
    # Periodic: no half-weight at endpoints.
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
