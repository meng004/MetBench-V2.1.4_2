"""Input adapter for the OpenMOC pin-cell SUT — RefineRayTracks.

Implements Bol-Alg-01 (NOETHER MR1 / B5 limit, deterministic-MoC
convergence). Refines OpenMOC's angular discretization atomically:

    new num_azim        = max(1, round(old num_azim * factor))
    new azim_spacing_cm = old azim_spacing_cm / factor

When chained across the calibrated catalog phases (factor = 1, 3, 8), the
resulting k_eff error is expected to decrease toward the most-refined run via
the typed ErrorMonotonicPredicate. Current v2 runtime performs this transform
in C# (RefineRayTracks); this adapter remains for legacy adapter-path tests.

Invocation contract:

    python openmoc_input_adapter_refine_ray_tracks.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{"factor": "2"}'
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def transform_input(source_file: str, output_file: str, params_json: str) -> dict:
    params = json.loads(params_json)
    if "factor" not in params:
        raise KeyError("Missing required parameter 'factor'")
    factor = float(params["factor"])
    if factor <= 0:
        raise ValueError(f"factor must be > 0 (got {factor})")

    source_path = Path(source_file)
    output_path = Path(output_file)
    case = json.loads(source_path.read_text(encoding="utf-8"))

    tracking = case.setdefault("tracking", {})
    old_num_azim = int(tracking.get("num_azim", 16))
    old_spacing = float(tracking.get("azim_spacing_cm", 0.05))

    new_num_azim = max(1, int(round(old_num_azim * factor)))
    new_spacing = old_spacing / factor

    tracking["num_azim"] = new_num_azim
    tracking["azim_spacing_cm"] = new_spacing

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "RefineRayTracks",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": (
            f"Refined tracking by factor {factor}: "
            f"num_azim {old_num_azim} -> {new_num_azim}, "
            f"azim_spacing_cm {old_spacing} -> {new_spacing}"
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMOC RefineRayTracks input adapter")
    sub = parser.add_subparsers(dest="command", required=True)
    p_t = sub.add_parser("transform-input")
    p_t.add_argument("--source-file", required=True)
    p_t.add_argument("--output-file", required=True)
    p_t.add_argument("--params", required=True)
    args = parser.parse_args()

    if args.command == "transform-input":
        print(json.dumps(transform_input(args.source_file, args.output_file, args.params), ensure_ascii=False))
        return 0
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
