"""Output parser for the Minimum-MR-SubSet P5 live SUT."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    payload = json.loads(Path(output_file).read_text(encoding="utf-8"))
    for key in ("max_power", "min_power", "final_power", "final_precursor", "rho", "n_steps", "dt"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in P5 output")
    return {
        "values": {
            "max_power": float(payload["max_power"]),
            "min_power": float(payload["min_power"]),
            "final_power": float(payload["final_power"]),
            "final_precursor": float(payload["final_precursor"]),
        },
        "metadata": {
            "program": "minimum_mr_subset_p5",
            "rho": str(payload["rho"]),
            "n_steps": str(payload["n_steps"]),
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
