"""Input adapter for the OpenMOC pin-cell SUT — MirrorX.

Implements NOETHER candidate MR02 (m_inv / B1 reflection symmetry
across the x-axis: y → -y). Flips the sign of
`geometry.fuel_offset_y_cm`; all other fields are byte-preserved.

The transformation is **only meaningful when fuel_offset_y_cm ≠ 0**
in the source (otherwise the mirrored JSON is byte-identical).
Phase-2 ships `pincell-offcentre.json` specifically for this MR
(and its MR03 twin).

Expected MR: `k_eff_followup ≈ k_eff_source within tolerance`. Both
runs describe the same physical pin-cell up to a mirror reflection
the solver must commute with. A bug that hard-codes `+offset_y`
(or uses `abs(offset_y)`, etc.) will violate the invariance.

Invocation contract:

    python openmoc_input_adapter_mirror_x.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{}'
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def transform_input(source_file: str, output_file: str, params_json: str) -> dict:
    json.loads(params_json)  # validate JSON shape (factor unused)

    source_path = Path(source_file)
    output_path = Path(output_file)
    case = json.loads(source_path.read_text(encoding="utf-8"))

    geom = case["geometry"]
    old_y = float(geom.get("fuel_offset_y_cm", 0.0))
    geom["fuel_offset_y_cm"] = -old_y

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "MirrorX",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {},
        "log": f"Mirrored across x-axis: fuel_offset_y_cm {old_y} -> {-old_y}.",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMOC MirrorX input adapter")
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
