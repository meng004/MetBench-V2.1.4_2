"""Output parser for the Minimum-MR-SubSet P8 live SUT."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    payload = json.loads(Path(output_file).read_text(encoding="utf-8"))
    for key in ("norm_initial", "norm_final", "norm_drift", "time_steps"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in P8 output")
    return {
        "values": {
            "norm_initial": float(payload["norm_initial"]),
            "norm_final": float(payload["norm_final"]),
            "norm_drift": float(payload["norm_drift"]),
            "probability_density_l1": float(payload["probability_density_l1"]),
        },
        "metadata": {
            "program": "minimum_mr_subset_p8",
            "time_steps": str(payload["time_steps"]),
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
