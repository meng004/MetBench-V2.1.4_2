"""Output adapter for the damped-oscillator SUT.

Reads a result JSON written by `damped_oscillator.py` and emits the MetBench
normalized parsed-output shape:

    {
      "values":   { "x_final": <float>, "v_final": <float>,
                    "max_abs_displacement": <float>, "energy_final": <float>,
                    "num_steps": <int as float> },
      "metadata": { "program": "damped_oscillator", "t_final": "<float>" }
    }

Invocation:

    python damped_oscillator_output_adapter.py parse-output --output-file <result.json>
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def parse_output(output_file: str) -> dict:
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
    parser = argparse.ArgumentParser(description="Damped-oscillator output adapter")
    subparsers = parser.add_subparsers(dest="command", required=True)
    parse_parser = subparsers.add_parser("parse-output")
    parse_parser.add_argument("--output-file", required=True)
    args = parser.parse_args()

    if args.command == "parse-output":
        print(json.dumps(parse_output(args.output_file), ensure_ascii=False))
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
