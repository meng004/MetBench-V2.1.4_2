"""Output parser for the 1D diffusion SUT (v2 API)."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    payload = json.loads(Path(output_file).read_text(encoding="utf-8"))

    for key in ("phi_max", "phi_center", "phi_integral", "num_points", "L_diffusion"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in diffusion-1d output")

    return {
        "values": {
            "phi_max":      float(payload["phi_max"]),
            "phi_center":   float(payload["phi_center"]),
            "phi_integral": float(payload["phi_integral"]),
        },
        "metadata": {
            "program":     "diffusion_1d",
            "num_points":  str(payload["num_points"]),
            "L_diffusion": str(payload["L_diffusion"]),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    p_parse = sub.add_parser("parse")
    p_parse.add_argument("--output-file", required=True)
    args = parser.parse_args()

    if args.command == "parse":
        json.dump(parse(args.output_file), sys.stdout, ensure_ascii=False)
        return 0
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
