"""Input adapter for the OpenMOC pin-cell SUT — Rotate90.

Implements NOETHER candidate MR01 (m_inv / B1 geometric symmetry, C4
quarter-rotation). Swaps `geometry.x_extent_cm` with `geometry.y_extent_cm`,
which is the JSON-level realization of a 90° rotation of the pin-cell
geometry. All material cross sections and tracking / solver parameters
are byte-preserved.

The transformation is **only meaningful when x_extent_cm ≠ y_extent_cm**
in the source. On the reference symmetric `pincell.json` (1.26 × 1.26 cm)
the rotated JSON is byte-identical to the source — Phase-2 ships
`pincell-asymmetric.json` (1.00 × 1.50 cm) specifically for this MR.

Expected MR: `k_eff_followup ≈ k_eff_source within tolerance`. Both runs
describe the same physical pin-cell up to a rigid rotation that the
solver should be invariant under. A bug that hard-codes one extent
(e.g. uses `x_extent_cm` for `half_y`) will violate the invariance.

Mirror MRs (MR02/MR03) are deliberately **out of scope** here: they
require off-centre fuel placement, which is a future runner extension
(the schema currently has no fuel-offset field).

Invocation contract:

    python openmoc_input_adapter_rotate_90.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{}'
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def transform_input(source_file: str, output_file: str, params_json: str) -> dict:
    json.loads(params_json)  # validate shape (factor unused)

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
    parser = argparse.ArgumentParser(description="OpenMOC Rotate90 input adapter")
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
