"""Output adapter for the Lotka-Volterra SUT.

Reads a result JSON written by `lotka_volterra.py` and emits the MetBench
normalized parsed-output shape:

    {
      "values":   { "mean_prey": <float>, "mean_predator": <float>,
                    "peak_prey": <float>, "peak_predator": <float>,
                    "prey_final": <float>, "predator_final": <float>,
                    "num_steps": <int as float> },
      "metadata": { "program": "lotka_volterra", "t_final": "<float>" }
    }

Invocation:

    python lotka_volterra_output_adapter.py parse-output --output-file <result.json>
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def parse_output(output_file: str) -> dict:
    path = Path(output_file)
    payload = json.loads(path.read_text(encoding="utf-8"))

    for key in ("mean_prey", "mean_predator", "peak_prey", "peak_predator",
                "prey_final", "predator_final", "num_steps", "t_final"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in lotka-volterra output")

    return {
        "values": {
            "mean_prey":      float(payload["mean_prey"]),
            "mean_predator":  float(payload["mean_predator"]),
            "peak_prey":      float(payload["peak_prey"]),
            "peak_predator":  float(payload["peak_predator"]),
            "prey_final":     float(payload["prey_final"]),
            "predator_final": float(payload["predator_final"]),
            "num_steps":      float(payload["num_steps"]),
        },
        "metadata": {
            "program": "lotka_volterra",
            "t_final": str(payload["t_final"]),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Lotka-Volterra output adapter")
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
