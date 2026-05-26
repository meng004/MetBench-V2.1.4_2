"""Output parser for the 1D Burgers SUT (v2 API)."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    payload = json.loads(Path(output_file).read_text(encoding="utf-8"))

    for key in ("peak_amplitude", "peak_location", "mass_integral", "num_points", "num_steps", "dt"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in burgers-1d output")

    return {
        "values": {
            "peak_amplitude": float(payload["peak_amplitude"]),
            "peak_location":  float(payload["peak_location"]),
            "mass_integral":  float(payload["mass_integral"]),
        },
        "metadata": {
            "program":    "burgers_1d",
            "num_points": str(payload["num_points"]),
            "num_steps":  str(payload["num_steps"]),
            "dt":         str(payload["dt"]),
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
