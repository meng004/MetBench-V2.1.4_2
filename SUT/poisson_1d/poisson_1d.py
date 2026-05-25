"""1D Poisson equation solver (SUT for system MT).

Solves the 1D Poisson equation with homogeneous Dirichlet boundaries and a
uniform source:

    -u''(x) = f,    x in [0, L],   u(0) = u(L) = 0

Discretised with the standard second-order central finite-difference stencil
on a uniform mesh (num_points nodes, including both boundary nodes). The
resulting symmetric tridiagonal linear system is solved with the Thomas
algorithm (stdlib only — no numpy / scipy).

For constant f the analytic solution is u(x) = f * x * (L - x) / 2; the peak
amplitude is u_max = f * L^2 / 8 at x = L/2. The numerical solution converges
to this with O(dx^2) FD truncation error.

Output observables:

  - u_max        peak amplitude of u (interior max)
  - u_center     u at x = L/2 (exact midpoint when num_points is odd)
  - u_integral   trapezoidal integral of u over [0, L]
  - num_points   mesh nodes
  - L_length     domain length (echo of input geometry.length)

Invocation:

    python poisson_1d.py --input case.json --output result.json

Input JSON shape:

    {
      "geometry": {
        "length":     1.0,     # L
        "num_points": 101      # uniform mesh nodes including boundaries
      },
      "source": {
        "strength":   1.0      # uniform source f
      }
    }

Output JSON shape:

    {
      "u_max":      <peak amplitude>,
      "u_center":   <u at x = L/2>,
      "u_integral": <trapezoidal integral over [0, L]>,
      "num_points": <mesh nodes>,
      "L_length":   <domain length>
    }
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def _solve_tridiag(lower: list, diag: list, upper: list, rhs: list) -> list:
    """Thomas algorithm for tridiagonal Ax = b. In-place style on local copies."""
    n = len(diag)
    c = list(upper) + [0.0]
    d = list(rhs)
    b = list(diag)
    for i in range(1, n):
        m = lower[i - 1] / b[i - 1]
        b[i] = b[i] - m * c[i - 1]
        d[i] = d[i] - m * d[i - 1]
    x = [0.0] * n
    x[n - 1] = d[n - 1] / b[n - 1]
    for i in range(n - 2, -1, -1):
        x[i] = (d[i] - c[i] * x[i + 1]) / b[i]
    return x


def solve(case: dict) -> dict:
    geom = case["geometry"]
    src = case["source"]

    L = float(geom["length"])
    n = int(geom["num_points"])
    if n < 3:
        raise ValueError("num_points must be >= 3")
    if L <= 0:
        raise ValueError(f"length must be > 0 (got {L})")

    f = float(src["strength"])
    dx = L / (n - 1)

    # Interior unknowns: i = 1 .. n-2 (boundaries u[0] = u[n-1] = 0).
    n_int = n - 2

    # Discretisation: -(u_{i-1} - 2 u_i + u_{i+1}) / dx^2 = f
    #   => ( 1/dx^2) u_{i-1} + (-2/dx^2) u_i + ( 1/dx^2) u_{i+1} = -f
    # Multiply both sides by -1 to keep coefficients positive on the diagonal:
    #   => (-1/dx^2) u_{i-1} + ( 2/dx^2) u_i + (-1/dx^2) u_{i+1} =  f
    a_off = -1.0 / (dx * dx)
    a_diag = 2.0 / (dx * dx)

    lower = [a_off] * (n_int - 1)
    diag = [a_diag] * n_int
    upper = [a_off] * (n_int - 1)
    rhs = [f] * n_int

    u_int = _solve_tridiag(lower, diag, upper, rhs)

    # Full solution vector (with zero boundaries).
    u = [0.0] + u_int + [0.0]

    # u_max from interior only — boundaries are forced to zero; for f < 0 the
    # interior is negative and we want the signed peak magnitude.
    u_max = max(u_int) if f >= 0 else min(u_int)
    u_center = u[(n - 1) // 2]
    u_integral = dx * (0.5 * u[0] + sum(u[1:-1]) + 0.5 * u[-1])

    return {
        "u_max": u_max,
        "u_center": u_center,
        "u_integral": u_integral,
        "num_points": n,
        "L_length": L,
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
