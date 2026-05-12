"""Input adapter for the OpenMC pin-cell SUT — Rotate90.

Mirror of `SUT/openmoc/openmoc_input_adapter_rotate_90.py`. Swaps
`geometry.x_extent_cm` with `geometry.y_extent_cm`; same MR predicting
k_eff invariance under 90° rotation, same scope note (only meaningful
on the asymmetric pin-cell sample where the two extents differ).

Invocation contract:

    python openmc_input_adapter_rotate_90.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{}'
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def transform_input(source_file: str, output_file: str, params_json: str) -> dict:
    json.loads(params_json)

    source_path = Path(source_file)
    output_path = Path(output_file)
    case = json.loads(source_path.read_text(encoding="utf-8"))

    geom = case["geometry"]
    old_x, old_y = float(geom["x_extent_cm"]), float(geom["y_extent_cm"])
    geom["x_extent_cm"], geom["y_extent_cm"] = old_y, old_x

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "Rotate90",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {},
        "log": f"Rotated 90°: swapped x_extent_cm ({old_x}) and y_extent_cm ({old_y}).",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC Rotate90 input adapter")
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
