"""Output parser for the SciPy BVP 1D Poisson SUT (v2 API).

Pure stdlib (no scipy import). Reads JSON output of `scipy_bvp_poisson_1d.py`
and emits MetBench-normalized {values, metadata}.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    payload = json.loads(Path(output_file).read_text(encoding="utf-8"))

    for key in ("u_max", "u_center", "u_integral", "num_points", "L_length"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in scipy-bvp-poisson-1d output")

    return {
        "values": {
            "u_max":      float(payload["u_max"]),
            "u_center":   float(payload["u_center"]),
            "u_integral": float(payload["u_integral"]),
        },
        "metadata": {
            "program":    "scipy_bvp_poisson_1d",
            "num_points": str(payload["num_points"]),
            "L_length":   str(payload["L_length"]),
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
