"""Output parser for the damped-oscillator SUT (v2 API).

Mirror of `heat_equation_output_parser.py`. Reads JSON output of
`damped_oscillator.py` and emits MetBench-normalized {values, metadata}.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    path = Path(output_file)
    payload = json.loads(path.read_text(encoding="utf-8"))

    for key in ("x_final", "v_final", "max_abs_displacement",
                "energy_final", "num_steps", "t_final"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in damped-oscillator output")

    return {
        "values": {
            "x_final":              float(payload["x_final"]),
            "v_final":              float(payload["v_final"]),
            "max_abs_displacement": float(payload["max_abs_displacement"]),
            "energy_final":         float(payload["energy_final"]),
            "num_steps":            float(payload["num_steps"]),
        },
        "metadata": {
            "program": "damped_oscillator",
            "t_final": str(payload["t_final"]),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
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
