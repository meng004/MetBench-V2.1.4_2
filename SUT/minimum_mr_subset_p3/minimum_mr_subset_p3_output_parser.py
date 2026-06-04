"""Output parser for the Minimum-MR-SubSet P3 live SUT."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    payload = json.loads(Path(output_file).read_text(encoding="utf-8"))
    for key in ("separation", "perturbation", "steps", "dt", "centroid"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in P3 output")
    centroid = payload["centroid"]
    return {
        "values": {
            "separation": float(payload["separation"]),
            "centroid_x": float(centroid[0]),
            "centroid_y": float(centroid[1]),
            "centroid_z": float(centroid[2]),
        },
        "metadata": {
            "program": "minimum_mr_subset_p3",
            "perturbation": str(payload["perturbation"]),
            "steps": str(payload["steps"]),
            "dt": str(payload["dt"]),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    parse_cmd = sub.add_parser("parse")
    parse_cmd.add_argument("--output-file", required=True)
    args = parser.parse_args()

    if args.command == "parse":
        json.dump(parse(args.output_file), sys.stdout, ensure_ascii=False)
        return 0
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
