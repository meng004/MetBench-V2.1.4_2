"""1D single-group steady-state diffusion solver (SUT for system MT).

Solves the 1D steady-state neutron diffusion equation with vacuum boundaries
and a uniform source:

    -D * d²φ/dx² + Σ_a * φ = S,    x ∈ [0, L],  φ(0) = φ(L) = 0

Discretized with second-order central finite differences on a uniform mesh
(num_points nodes, including both boundary nodes). The resulting tridiagonal
linear system is solved with the Thomas algorithm (stdlib only — no numpy).

Output observables include the flux peak (φ_max), the centerline flux
(φ_center), and the trapezoidal integral ∫₀ᴸ φ(x) dx — all monotone in the
source S, all invariant under the rotation x → L − x.

Invocation:

    python diffusion_1d.py --input case.json --output result.json

Input JSON shape:

    {
      "geometry": {
        "length":      100.0,    # L  [cm]
        "num_points":  101       # uniform mesh nodes including boundaries
      },
      "material": {
        "D":           1.0,      # diffusion coefficient [cm]
        "sigma_a":     0.01      # macroscopic absorption [1/cm]
      },
      "source": {
        "strength":    1.0       # uniform source S [n/(cm³·s)]
      }
    }

Output JSON shape:

    {
      "phi_max":      <peak flux>,
      "phi_center":   <flux at x = L/2>,
      "phi_integral": <trapezoidal ∫₀ᴸ φ(x) dx>,
      "num_points":   <mesh nodes>,
      "L_diffusion":  <sqrt(D / Σ_a) [cm]>
    }
"""

from __future__ import annotations

import argparse
import json
import math
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
    mat = case["material"]
    src = case["source"]

    L = float(geom["length"])
    n = int(geom["num_points"])
    if n < 3:
        raise ValueError("num_points must be ≥ 3")

    D = float(mat["D"])
    sigma_a = float(mat["sigma_a"])
    S = float(src["strength"])

    dx = L / (n - 1)
    # Interior unknowns: i = 1 .. n-2  (boundaries phi[0] = phi[n-1] = 0)
    n_int = n - 2

    # Coefficients for -D * (φ_{i-1} - 2φ_i + φ_{i+1}) / dx² + Σ_a * φ_i = S
    # → (-D/dx²) φ_{i-1} + (2D/dx² + Σ_a) φ_i + (-D/dx²) φ_{i+1} = S
    a_off = -D / (dx * dx)
    a_diag = 2.0 * D / (dx * dx) + sigma_a

    lower = [a_off] * (n_int - 1)
    diag = [a_diag] * n_int
    upper = [a_off] * (n_int - 1)
    rhs = [S] * n_int

    phi_int = _solve_tridiag(lower, diag, upper, rhs)

    # Full flux vector (with zero boundaries)
    phi = [0.0] + phi_int + [0.0]

    phi_max = max(phi)
    phi_center = phi[(n - 1) // 2]
    # Trapezoidal integration
    phi_integral = dx * (0.5 * phi[0] + sum(phi[1:-1]) + 0.5 * phi[-1])

    L_diffusion = math.sqrt(D / sigma_a) if sigma_a > 0 else float("inf")

    return {
        "phi_max": phi_max,
        "phi_center": phi_center,
        "phi_integral": phi_integral,
        "num_points": n,
        "L_diffusion": L_diffusion,
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
