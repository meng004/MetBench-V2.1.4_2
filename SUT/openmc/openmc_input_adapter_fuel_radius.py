"""Input adapter for the OpenMC pin-cell SUT — ScaleFuelRadius.

Mirror of `SUT/openmoc/openmoc_input_adapter_fuel_radius.py`. Same
transformation, same MR (k_eff rises for small factor > 1 in the
under-moderated regime), same fuel-radius < half-extent guard.

Invocation contract:

    python openmc_input_adapter_fuel_radius.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{"factor": "1.05"}'
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

    geom = case["geometry"]
    old_radius = float(geom["fuel_radius_cm"])
    new_radius = old_radius * factor

    half_extent = min(float(geom["x_extent_cm"]), float(geom["y_extent_cm"])) / 2.0
    if new_radius >= half_extent:
        raise ValueError(
            f"new fuel_radius_cm ({new_radius}) would meet or exceed "
            f"half the pin extent ({half_extent}); pick a smaller factor"
        )

    geom["fuel_radius_cm"] = new_radius

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleFuelRadius",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": f"Scaled geometry.fuel_radius_cm by {factor}: {old_radius} -> {new_radius}",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC ScaleFuelRadius input adapter")
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
