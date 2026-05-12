"""Input adapter for the OpenMOC pin-cell SUT — MirrorY.

Implements NOETHER candidate MR03 (m_inv / B1 reflection symmetry
across the y-axis: x → -x). Flips the sign of
`geometry.fuel_offset_x_cm`; all other fields are byte-preserved.

The transformation is **only meaningful when fuel_offset_x_cm ≠ 0**
in the source.

Expected MR: `k_eff_followup ≈ k_eff_source within tolerance`.

Invocation contract:

    python openmoc_input_adapter_mirror_y.py transform-input \
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
    old_x = float(geom.get("fuel_offset_x_cm", 0.0))
    geom["fuel_offset_x_cm"] = -old_x

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "MirrorY",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {},
        "log": f"Mirrored across y-axis: fuel_offset_x_cm {old_x} -> {-old_x}.",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMOC MirrorY input adapter")
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
