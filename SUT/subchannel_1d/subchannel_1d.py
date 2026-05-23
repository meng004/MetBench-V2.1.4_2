"""1D single-channel subchannel thermal-hydraulic solver (SUT for system MT).

Simplified 1D, steady-state, incompressible single-channel model — the canonical
"introductory" reduction of the Navier-Stokes equations under (a) steady flow,
(b) constant cross-section, (c) constant fluid properties, (d) prescribed wall
heat flux. Conservation laws collapse to closed-form algebra:

    Mass:       G = const                                  (specified inlet)
    Momentum:   dp/dz = -f * G^2 / (2 * rho * D_h)         (Darcy friction)
                => Delta_p = f * L * G^2 / (2 * rho * D_h)
    Energy:     G * c_p * dT/dz = q'' * P_h / A_xs         (uniform heating)
                => T_out = T_in + q'' * P_h * L / (G * A_xs * c_p)
                        = T_in + Q_total / (G * A_xs * c_p)

This is *not* a CFD code — it is a textbook 1D subchannel that lets MetBench
exercise system-MT against Navier-Stokes-derived conservation invariants.

Invocation:

    python subchannel_1d.py --input case.json --output result.json

Input JSON shape:

    {
      "geometry": {
        "channel_length":          1.0,           # L  [m]
        "cross_section_area":      1.0e-4,        # A_xs [m^2]
        "heated_perimeter":        0.04,          # P_h [m]
        "hydraulic_diameter":      0.012          # D_h [m]
      },
      "fluid": {
        "density":                 750.0,         # rho [kg/m^3]
        "specific_heat":           5200.0,        # c_p [J/(kg K)]
        "friction_factor":         0.02           # Darcy f [-]
      },
      "boundary": {
        "mass_flux":               4000.0,        # G [kg/(m^2 s)]
        "inlet_temperature":       560.0,         # T_in [K]
        "heat_flux":               5.0e5          # q'' [W/m^2]
      }
    }

Output JSON shape:

    {
      "T_out":           <outlet temperature [K]>,
      "delta_T":         <T_out - T_in [K]>,
      "heat_input":      <q'' * P_h ; *per-unit-length* heat input [W/m]>,
      "delta_p":         <friction pressure drop [Pa]>,
      "channel_length":  <L echoed back [m]>
    }

Note: `heat_input` is W/m (not total W). To recover total power, multiply by
`channel_length`. This is intentional — keeping the per-unit-length form makes
energy-conservation MRs trivially expressible without dividing by L.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def solve(case: dict) -> dict:
    geom = case["geometry"]
    fluid = case["fluid"]
    bnd = case["boundary"]

    L = float(geom["channel_length"])
    A_xs = float(geom["cross_section_area"])
    P_h = float(geom["heated_perimeter"])
    D_h = float(geom["hydraulic_diameter"])

    rho = float(fluid["density"])
    cp = float(fluid["specific_heat"])
    f = float(fluid["friction_factor"])

    G = float(bnd["mass_flux"])  # kg/(m^2 s)
    T_in = float(bnd["inlet_temperature"])
    qpp = float(bnd["heat_flux"])  # W/m^2

    # Sanity guard: explicit ValueError beats silent inf/NaN in JSON output
    if G <= 0:
        raise ValueError(f"mass_flux must be > 0 (got {G})")
    if cp <= 0 or A_xs <= 0:
        raise ValueError(f"cp and A_xs must be > 0 (got cp={cp}, A_xs={A_xs})")
    if rho <= 0 or D_h <= 0:
        raise ValueError(f"density and hydraulic_diameter must be > 0 (got rho={rho}, D_h={D_h})")

    # Energy: T_out = T_in + q'' * P_h * L / (G * A_xs * c_p)
    heat_input_per_unit_length = qpp * P_h  # W/m
    delta_T = (heat_input_per_unit_length * L) / (G * A_xs * cp)
    T_out = T_in + delta_T

    # Momentum: Delta_p = f * L * G^2 / (2 * rho * D_h)
    delta_p = f * L * G * G / (2.0 * rho * D_h)

    return {
        "T_out": T_out,
        "delta_T": delta_T,
        "heat_input": heat_input_per_unit_length,
        "delta_p": delta_p,
        "channel_length": L,
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
