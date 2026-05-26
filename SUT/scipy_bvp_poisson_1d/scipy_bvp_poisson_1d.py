"""SciPy `solve_bvp`-backed 1D Poisson equation solver (system under test).

T3C-BVP external-solver pilot. Same Poisson PDE as the pure-stdlib
`SUT/poisson_1d/poisson_1d.py`, but solved as a two-point BVP via
`scipy.integrate.solve_bvp` instead of central-difference finite difference.

    -u''(x) = f,    x in [0, L],   u(0) = u(L) = 0

Reformulated as a 1st-order system for `solve_bvp`:

    y0 = u
    y1 = u'
    y0' = y1
    y1' = -f

with Dirichlet BC `y0(0) = 0`, `y0(L) = 0`.

For constant `f` the analytic solution is `u(x) = f * x * (L - x) / 2`;
the peak amplitude is `u_max = f * L^2 / 8` at `x = L/2`. SciPy adaptively
refines its own mesh until residual is below tolerance; the user-supplied
`num_points` only seeds the initial mesh — the converged result is
independent of seed mesh resolution to within tolerance. See MR
`scipy-bvp-poisson-seed-mesh-insensitivity`.

Invocation:

    python scipy_bvp_poisson_1d.py --input case.json --output result.json

Input JSON shape:

    {
      "geometry": {
        "length":     1.0,     # L
        "num_points": 101      # initial-mesh seed nodes (>= 3)
      },
      "source": {
        "strength":   1.0      # uniform source f
      }
    }

Output JSON shape:

    {
      "u_max":      <peak amplitude on the converged mesh>,
      "u_center":   <u at x = L/2 by linear interpolation of the converged mesh>,
      "u_integral": <trapezoidal integral of u over [0, L]>,
      "num_points": <seed num_points (echo of input geometry.num_points)>,
      "L_length":   <domain length (echo of input geometry.length)>
    }
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def solve(case: dict) -> dict:
    # Imported lazily so module import does not fail when scipy is absent;
    # parser scripts are scipy-free and must keep working on systems without scipy.
    import numpy as np
    from scipy.integrate import solve_bvp, trapezoid

    geom = case["geometry"]
    src = case["source"]

    L = float(geom["length"])
    n = int(geom["num_points"])
    if n < 3:
        raise ValueError("num_points must be >= 3")
    if L <= 0:
        raise ValueError(f"length must be > 0 (got {L})")

    f = float(src["strength"])

    x_init = np.linspace(0.0, L, n)
    # Initial guess: zero everywhere. solve_bvp adapts mesh + values until convergence.
    y_init = np.zeros((2, n))

    def rhs(_x, y):
        # y[0] = u, y[1] = u'. RHS of the 1st-order system:
        # u' = y[1]; (u')' = -f
        return np.vstack((y[1], -f * np.ones_like(y[0])))

    def bc(ya, yb):
        # Dirichlet: u(0) = 0, u(L) = 0
        return np.array([ya[0], yb[0]])

    sol = solve_bvp(rhs, bc, x_init, y_init, tol=1e-9, max_nodes=20000)
    if not sol.success:
        raise RuntimeError(f"solve_bvp failed: {sol.message}")

    # Evaluate u on a dense grid for u_max + integral; sol.x is the converged adaptive mesh.
    x_dense = np.linspace(0.0, L, max(n, 1001))
    u_dense = sol.sol(x_dense)[0]

    u_max = float(u_dense.max()) if f >= 0 else float(u_dense.min())
    u_center = float(sol.sol(np.array([L / 2.0]))[0][0])
    u_integral = float(trapezoid(u_dense, x_dense))

    return {
        "u_max": u_max,
        "u_center": u_center,
        "u_integral": u_integral,
        "num_points": n,
        "L_length": L,
    }


def main() -> int:
    p = argparse.ArgumentParser(description="SciPy solve_bvp 1D Poisson solver")
    p.add_argument("--input", required=True)
    p.add_argument("--output", required=True)
    args = p.parse_args()

    case = json.loads(Path(args.input).read_text(encoding="utf-8"))
    result = solve(case)
    Path(args.output).parent.mkdir(parents=True, exist_ok=True)
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
