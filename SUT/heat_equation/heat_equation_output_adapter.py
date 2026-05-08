"""Output adapter for the 1D heat-equation SUT.

Reads a result JSON written by `heat_equation.py` and emits the MetBench
normalized parsed-output shape:

    {
      "values":   { "max_u": <float>, "min_u": <float>, "l2_norm": <float>,
                    "integral": <float>, "stability_violated": 0|1,
                    "num_steps": <int as float>, "stability_ratio": <float> },
      "metadata": { "program": "heat_equation", "t_final": "<float>" }
    }

Invocation:

    python heat_equation_output_adapter.py parse-output --output-file <result.json>
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def parse_output(output_file: str) -> dict:
    path = Path(output_file)
    payload = json.loads(path.read_text(encoding="utf-8"))

    for key in ("max_u", "min_u", "l2_norm", "integral",
                "num_steps", "stability_ratio", "stability_violated", "t_final"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in heat-equation output")

    stability_violated = payload["stability_violated"]
    if not isinstance(stability_violated, bool):
        raise TypeError(f"'stability_violated' must be bool, got {type(stability_violated).__name__}")

    return {
        "values": {
            "max_u":              float(payload["max_u"]),
            "min_u":              float(payload["min_u"]),
            "l2_norm":            float(payload["l2_norm"]),
            "integral":           float(payload["integral"]),
            "num_steps":          float(payload["num_steps"]),
            "stability_ratio":    float(payload["stability_ratio"]),
            "stability_violated": 1.0 if stability_violated else 0.0,
        },
        "metadata": {
            "program":   "heat_equation",
            "t_final":   str(payload["t_final"]),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Heat-equation output adapter")
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
