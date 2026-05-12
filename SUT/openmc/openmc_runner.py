"""OpenMC pin-cell SUT.

Mirror of `SUT/openmoc/openmoc_runner.py`: reads the same JSON case description
(2-energy-group cross sections + 2D pin-cell geometry + reflective boundaries)
and computes k_eff using OpenMC's Monte Carlo solver in multi-group mode.

Same MR semantics as OpenMOC: scaling fuel `nu_sigma_f` increases k_eff;
scaling fuel `sigma_a` decreases k_eff. The two solvers share input format,
share MR transformations, and produce comparable k_eff (within Monte Carlo
statistical uncertainty for OpenMC + spatial discretization error for both).

Invocation:

    python openmc_runner.py --input <source.json> --output <result.json>

Input schema is identical to OpenMOC's (see `SUT/openmoc/sample/pincell.json`).
The runner additionally honours these optional `solver` keys for OpenMC:

    "solver": {
      "batches":   60,        # optional, default 60
      "inactive":  20,        # optional, default 20
      "particles": 5000       # optional, default 5000
    }

Output schema (compatible with `openmc_output_adapter.py`):

    {
      "k_eff":      <float>,            # mean of active batches
      "k_eff_std":  <float>,            # 1-sigma standard deviation
      "batches":    <int>,
      "particles":  <int>,
      "converged":  <bool>,             # always True if openmc.run() exits cleanly
      "metadata":   { "runner": "openmc", "energy_mode": "multi-group" }
    }
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
import tempfile
from pathlib import Path


def _resolve_openmc_exec() -> str:
    """Find the openmc binary. Try (1) a sibling of the current python
    (typical for conda envs), (2) PATH lookup, (3) fall back to "openmc"
    and let subprocess raise if absent."""
    sibling = Path(sys.executable).with_name("openmc")
    if sibling.exists():
        return str(sibling)
    on_path = shutil.which("openmc")
    if on_path:
        return on_path
    return "openmc"


def _build_mgxs_library(case: dict, library_path: Path) -> None:
    """Write a 2-group HDF5 cross-section library from the JSON case."""
    import h5py
    import numpy as np
    import openmc

    # 2-group thermal cutoff at 0.625 eV (standard PWR analysis split).
    group_edges = np.array([1e-5, 0.625, 2.0e7], dtype=np.float64)

    library = openmc.MGXSLibrary(openmc.mgxs.EnergyGroups(group_edges))

    for name, mat in case["materials"].items():
        n = int(mat["num_groups"])
        if n != 2:
            raise ValueError(f"OpenMC runner expects num_groups=2, got {n} for material '{name}'")

        # Phase-3: optional Doppler-style absorption broadening. T_ref = 600 K
        # so existing samples (no field) get factor = 1.0 and behave identically.
        # Mirrors the formula in SUT/openmoc/openmoc_runner.py::_doppler_factor.
        import math as _math
        t_kelvin = float(mat.get("temperature_kelvin", 600.0))
        doppler = 1.0 + 0.05 * _math.log(t_kelvin / 600.0) if t_kelvin > 0 else 1.0

        xsdata = openmc.XSdata(name, library.energy_groups)
        xsdata.order = 0  # P0 scattering, isotropic
        xsdata.set_total(np.array(mat["sigma_t"], dtype=np.float64) * doppler)
        xsdata.set_absorption(np.array(mat["sigma_a"], dtype=np.float64) * doppler)

        # Phase-3 / Case 2 live-trigger plumbing.
        # When the case JSON sets `materials.<name>.exercise_add_temperature: true`
        # AND a non-default `temperature_kelvin`, we additionally call
        # `xsdata.add_temperature(t_kelvin)` — exactly the upstream code path
        # broken by OpenMC PR #3712 (`temperatures.tolist().append(T)` returns
        # None). On installed buggy OpenMC the call crashes with TypeError;
        # the runner re-raises with a recognizable marker so the matrix can
        # attribute the failure to the upstream bug rather than to a synthetic
        # mutation. Default is to skip this call so existing scenarios remain
        # untouched.
        if mat.get("exercise_add_temperature") and t_kelvin > 0 and abs(t_kelvin - 600.0) > 1e-9:
            try:
                xsdata.add_temperature(t_kelvin)
            except TypeError as e:
                raise RuntimeError(
                    f"OpenMC PR #3712 (add_temperature → None) triggered on "
                    f"{name}: {e}"
                ) from None

        # Phase-3 / Case 5 live-trigger plumbing.
        # When the case JSON sets `materials.<name>.exercise_borated_water: true`
        # AND a non-default `temperature_kelvin`, we additionally call
        # `openmc.model.borated_water(boron_ppm, temperature=t, density=...)` —
        # the path broken by OpenMC PR #3662 (when density is supplied, the
        # user-given temperature is silently dropped from the returned
        # Material). On installed buggy OpenMC the returned material has
        # `temperature is None` (or != t); the runner raises with a
        # recognizable marker so the matrix attributes the failure to the
        # upstream bug. Default is to skip so existing scenarios are untouched.
        if mat.get("exercise_borated_water") and t_kelvin > 0 and abs(t_kelvin - 600.0) > 1e-9:
            import openmc.model
            boron_ppm = float(mat.get("boron_ppm", 500.0))
            density_g_cc = float(mat.get("density_g_cc", 0.7))
            bw_mat = openmc.model.borated_water(
                boron_ppm=boron_ppm, temperature=t_kelvin, density=density_g_cc,
            )
            actual_t = bw_mat.temperature
            if actual_t is None or abs(float(actual_t) - t_kelvin) > 1e-6:
                raise RuntimeError(
                    f"OpenMC PR #3662 (borated_water drops temperature when "
                    f"density set) triggered on {name}: requested T={t_kelvin}, "
                    f"got mat.temperature={actual_t}"
                )
        # OpenMOC stores sigma_s as a 4-element row-major matrix [g_in -> g_out].
        # OpenMC's set_scatter_matrix wants shape (num_groups, num_groups, num_legendre_moments).
        # With xsdata.order = 0 (P0, isotropic), num_legendre_moments = 1.
        sig_s_flat = np.array(mat["sigma_s"], dtype=np.float64)
        if sig_s_flat.size != n * n:
            raise ValueError(f"sigma_s for '{name}' must have {n*n} entries, got {sig_s_flat.size}")
        scatter_matrix = sig_s_flat.reshape((n, n, 1))
        xsdata.set_scatter_matrix(scatter_matrix)
        xsdata.set_fission(np.array(mat["sigma_f"], dtype=np.float64))
        xsdata.set_nu_fission(np.array(mat["nu_sigma_f"], dtype=np.float64))
        xsdata.set_chi(np.array(mat["chi"], dtype=np.float64))

        library.add_xsdata(xsdata)

    library.export_to_hdf5(str(library_path))


def _build_model(case: dict, library_path: Path) -> "openmc.Model":
    import openmc

    g = case["geometry"]
    half_x = g["x_extent_cm"] / 2.0
    half_y = g["y_extent_cm"] / 2.0

    # Materials use macroscopic cross sections that reference the MGXS library.
    fuel = openmc.Material(name="fuel")
    fuel.set_density("macro", 1.0)
    fuel.add_macroscopic("fuel")

    moderator = openmc.Material(name="moderator")
    moderator.set_density("macro", 1.0)
    moderator.add_macroscopic("moderator")

    materials = openmc.Materials([fuel, moderator])
    materials.cross_sections = str(library_path)

    # Geometry: 2D pin cell with reflective boundaries.
    # Phase-2 MR02/MR03: optional fuel-offset (defaults to 0 → centred fuel,
    # backward compatible with all existing samples).
    fuel_outer = openmc.ZCylinder(
        x0=float(g.get("fuel_offset_x_cm", 0.0)),
        y0=float(g.get("fuel_offset_y_cm", 0.0)),
        r=float(g["fuel_radius_cm"]),
    )
    xmin = openmc.XPlane(x0=-half_x, boundary_type="reflective")
    xmax = openmc.XPlane(x0=+half_x, boundary_type="reflective")
    ymin = openmc.YPlane(y0=-half_y, boundary_type="reflective")
    ymax = openmc.YPlane(y0=+half_y, boundary_type="reflective")

    box = +xmin & -xmax & +ymin & -ymax
    fuel_cell = openmc.Cell(name="fuel", fill=fuel, region=-fuel_outer & box)
    mod_cell = openmc.Cell(name="moderator", fill=moderator, region=+fuel_outer & box)
    geometry = openmc.Geometry(openmc.Universe(cells=[fuel_cell, mod_cell]))

    # Phase-3: per-cell scalar flux tally for tally-symmetry MRs.
    # Two energy groups (matching the MGXS library cutoff at 0.625 eV).
    import numpy as np
    cell_filter = openmc.CellFilter([fuel_cell, mod_cell])
    energy_filter = openmc.EnergyFilter(np.array([1e-5, 0.625, 2.0e7], dtype=np.float64))
    flux_tally = openmc.Tally(name="flux_per_cell")
    flux_tally.filters = [cell_filter, energy_filter]
    flux_tally.scores = ["flux"]
    tallies = openmc.Tallies([flux_tally])

    # Settings: multi-group mode, eigenvalue calculation.
    sv = case.get("solver", {})
    settings = openmc.Settings()
    settings.energy_mode = "multi-group"
    settings.run_mode = "eigenvalue"
    settings.batches = int(sv.get("batches", 60))
    settings.inactive = int(sv.get("inactive", 20))
    settings.particles = int(sv.get("particles", 5000))
    settings.source = openmc.IndependentSource(
        space=openmc.stats.Box([-half_x, -half_y, -1.0], [half_x, half_y, 1.0])
    )
    settings.output = {"summary": False, "tallies": False}
    settings.verbosity = 1  # warnings only; MetBench captures stdout separately

    model = openmc.Model(geometry=geometry, materials=materials, settings=settings)
    model.tallies = tallies
    # Stash cell IDs so solve() can resolve them in the statepoint tally.
    model._fuel_cell_id = fuel_cell.id  # type: ignore[attr-defined]
    model._mod_cell_id = mod_cell.id    # type: ignore[attr-defined]
    return model


def solve(case: dict) -> dict:
    """Run OpenMC in an isolated working directory, return the result dict."""
    import openmc

    # OpenMC writes XML inputs and statepoint.h5 to the current working
    # directory, so isolate per call so concurrent runs don't collide.
    with tempfile.TemporaryDirectory(prefix="openmc-pincell-") as tmpdir:
        library_path = Path(tmpdir) / "mg_cross_sections.h5"
        _build_mgxs_library(case, library_path)

        model = _build_model(case, library_path)

        openmc_exec = _resolve_openmc_exec()
        cwd = os.getcwd()
        try:
            os.chdir(tmpdir)
            # model.run() handles export_to_xml() + openmc.run() in one call,
            # and respects openmc_exec for non-default conda env locations.
            model.run(output=False, openmc_exec=openmc_exec)
            sp_files = sorted(Path(tmpdir).glob("statepoint.*.h5"))
            if not sp_files:
                raise RuntimeError("OpenMC did not produce a statepoint file")
            with openmc.StatePoint(str(sp_files[-1])) as sp:
                k = sp.keff
                k_mean = float(k.nominal_value)
                k_std = float(k.std_dev)
                batches = int(sp.n_batches)

                # Per-cell flux from the Phase-3 tally. The tally has two
                # filters (cell × energy-group) and a single "flux" score.
                # OpenMC reports energy filter bins in ascending-energy order
                # (group 0 = thermal, group 1 = fast). MetBench's convention
                # everywhere else (matching OpenMOC) is group 0 = fast,
                # group 1 = thermal — so we reverse the energy axis here.
                flux_per_cell = {}
                try:
                    t = sp.get_tally(name="flux_per_cell")
                    arr = t.mean.reshape(2, 2)  # (cell, energy) — order matches filter declaration
                    bins = t.filters[0].bins
                    fuel_cell_id = getattr(model, "_fuel_cell_id", None)
                    mod_cell_id = getattr(model, "_mod_cell_id", None)
                    for i, cell_id in enumerate(bins):
                        cid = int(cell_id)
                        bucket = ("fuel" if cid == fuel_cell_id else
                                  "moderator" if cid == mod_cell_id else f"cell_{cid}")
                        # reverse: bin[0] is thermal, bin[1] is fast; MetBench wants [fast, thermal]
                        flux_per_cell[bucket] = [float(arr[i, 1]), float(arr[i, 0])]
                except Exception as e:  # don't kill the run if tally extraction fails
                    flux_per_cell = {"error": f"tally extraction failed: {type(e).__name__}: {e}"}
        finally:
            os.chdir(cwd)
            # Clean up any large data files OpenMC may have written outside tmpdir.
            for stray in Path(cwd).glob("statepoint.*.h5"):
                try:
                    stray.unlink()
                except OSError:
                    pass
            for stray in Path(cwd).glob("summary.h5"):
                try:
                    stray.unlink()
                except OSError:
                    pass

    sv = case.get("solver", {})
    return {
        "k_eff": k_mean,
        "k_eff_std": k_std,
        "batches": batches,
        "particles": int(sv.get("particles", 5000)),
        "converged": True,
        "flux_per_cell": flux_per_cell,
        "metadata": {"runner": "openmc", "energy_mode": "multi-group"},
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC pin-cell SUT runner")
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
