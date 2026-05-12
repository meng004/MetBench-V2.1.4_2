"""OpenMOC pin-cell SUT.

Reads a JSON case description, builds a 2D pin-cell `Geometry` with
reflective boundaries, computes k_eff with `CPUSolver`, and writes the
result to a JSON output file.

Invocation:

    python openmoc_runner.py --input <source.json> --output <result.json>

Input schema is the one defined in
`docs/superpowers/plans/2026-05-08-stage3-openmoc.md` and exemplified by
`SUT/openmoc/sample/pincell.json`. Two materials (`fuel`, `moderator`)
with 2-energy-group cross sections, plus geometry/tracking/solver
sections.

Output schema:

    {
      "k_eff":      <float>,
      "iterations": <int>,
      "converged":  <bool>,
      "metadata":   { "runner": "openmoc" }
    }

The runner does not interpret MR semantics; it computes `k_eff` and
reports whether the solver hit `max_iters` (`converged == False`) or
reached the configured threshold first (`converged == True`).
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def _doppler_factor(temperature_kelvin: float) -> float:
    """Phase-3 Doppler-style absorption-broadening factor.

    Real Doppler broadening requires resonance cross-section data we don't
    carry in the multi-group library; this is a fictitious-but-monotone
    surrogate that exercises the temperature plumbing code path so MRs in
    the m_mono(T) family have something physical to assert. T_ref = 600 K
    so existing samples (which omit the field and default to 600) get
    factor = 1.0 and behave identically to before.
    """
    import math
    if temperature_kelvin <= 0:
        return 1.0
    return 1.0 + 0.05 * math.log(temperature_kelvin / 600.0)


def _build_material(name: str, mat: dict):
    import openmoc

    # OpenMOC's transport equation uses sigma_t and sigma_s; absorption is
    # derived as sigma_t - sum(sigma_s_g'). The Python Material class
    # exposes setSigmaA only as a per-group setter, but the bulk solve does
    # not require it - the case JSON's sigma_a is for documentation /
    # cross-checks only.
    #
    # Phase-3 Doppler: scale **only** the absorption part of sigma_t
    # (i.e. the difference sigma_t - row_sum_sigma_s) by the Doppler
    # factor, not all of sigma_t. Scaling raw sigma_t amplifies through
    # the sigma_a derivation (a 2% sigma_t bump becomes a ~40% sigma_a
    # bump because most of sigma_t is scattering), which puts OpenMOC's
    # CPUSolver into one of its narrow non-physical convergence basins
    # documented in historical-bugs.md Case 4.
    t_kelvin = float(mat.get("temperature_kelvin", 600.0))
    doppler = _doppler_factor(t_kelvin)
    n = int(mat["num_groups"])
    sigma_s_flat = list(mat["sigma_s"])
    if doppler != 1.0:
        sigma_t = []
        for g_in in range(n):
            row_sum = sum(sigma_s_flat[g_in * n + g_out] for g_out in range(n))
            old_sigma_a = float(mat["sigma_t"][g_in]) - row_sum
            sigma_t.append(doppler * old_sigma_a + row_sum)
    else:
        sigma_t = list(mat["sigma_t"])

    m = openmoc.Material(name=name)
    m.setNumEnergyGroups(n)
    m.setSigmaT(sigma_t)
    m.setSigmaS(mat["sigma_s"])
    m.setNuSigmaF(mat["nu_sigma_f"])
    m.setSigmaF(mat["sigma_f"])
    m.setChi(mat["chi"])
    return m


def solve(case: dict) -> tuple[float, int, bool]:
    import openmoc
    import openmoc.log as omlog

    omlog.set_log_level("ERROR")

    g = case["geometry"]
    t = case["tracking"]
    sv = case["solver"]
    half_x = g["x_extent_cm"] / 2.0
    half_y = g["y_extent_cm"] / 2.0
    half_z = g["z_extent_cm"] / 2.0

    fuel_mat = _build_material("fuel", case["materials"]["fuel"])
    mod_mat = _build_material("moderator", case["materials"]["moderator"])

    xmin = openmoc.XPlane(x=-half_x)
    xmax = openmoc.XPlane(x=half_x)
    ymin = openmoc.YPlane(y=-half_y)
    ymax = openmoc.YPlane(y=half_y)
    zmin = openmoc.ZPlane(z=-half_z)
    zmax = openmoc.ZPlane(z=half_z)
    for s in (xmin, xmax, ymin, ymax, zmin, zmax):
        s.setBoundaryType(openmoc.REFLECTIVE)
    # Phase-2 MR02/MR03: optional fuel-offset (defaults to 0 → centred fuel,
    # backward compatible with all existing samples).
    fuel_cyl = openmoc.ZCylinder(
        x=float(g.get("fuel_offset_x_cm", 0.0)),
        y=float(g.get("fuel_offset_y_cm", 0.0)),
        radius=float(g["fuel_radius_cm"]),
    )

    fuel_cell = openmoc.Cell(name="fuel")
    fuel_cell.setFill(fuel_mat)
    fuel_cell.addSurface(-1, fuel_cyl)
    mod_cell = openmoc.Cell(name="moderator")
    mod_cell.setFill(mod_mat)
    mod_cell.addSurface(+1, fuel_cyl)
    for cell in (fuel_cell, mod_cell):
        cell.addSurface(+1, xmin)
        cell.addSurface(-1, xmax)
        cell.addSurface(+1, ymin)
        cell.addSurface(-1, ymax)
        cell.addSurface(+1, zmin)
        cell.addSurface(-1, zmax)

    root = openmoc.Universe(name="root")
    root.addCell(fuel_cell)
    root.addCell(mod_cell)
    geom = openmoc.Geometry()
    geom.setRootUniverse(root)
    geom.initializeFlatSourceRegions()

    tg = openmoc.TrackGenerator(geom, num_azim=int(t["num_azim"]), azim_spacing=float(t["azim_spacing_cm"]))
    tg.setZCoord(float(t["z_coord_cm"]))
    tg.generateTracks()

    solver = openmoc.CPUSolver(tg)
    solver.setNumThreads(int(sv["num_threads"]))
    solver.setConvergenceThreshold(float(sv["convergence_threshold"]))
    max_iters = int(sv["max_iters"])
    solver.computeEigenvalue(max_iters)

    iters = int(solver.getNumIterations())
    return float(solver.getKeff()), iters, iters < max_iters


def _require_section(case: dict, key: str) -> dict:
    if key not in case:
        raise ValueError(f"Case JSON is missing required section '{key}'")
    return case[key]


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMOC pin-cell SUT for MetBench Stage 3")
    parser.add_argument("--input", required=True, help="Path to the case JSON")
    parser.add_argument("--output", required=True, help="Path to write the result JSON")
    args = parser.parse_args()

    case = json.loads(Path(args.input).read_text(encoding="utf-8"))
    _require_section(case, "geometry")
    _require_section(case, "tracking")
    _require_section(case, "solver")
    materials = _require_section(case, "materials")
    if "fuel" not in materials or "moderator" not in materials:
        raise ValueError("Case JSON 'materials' must define 'fuel' and 'moderator'")

    k, iters, converged = solve(case)

    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(
            {
                "k_eff": k,
                "iterations": iters,
                "converged": converged,
                "metadata": {"runner": "openmoc"},
            },
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
