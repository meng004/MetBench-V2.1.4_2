"""Output adapter for the decay-chain SUT.

Reads a result JSON written by `decay_chain.py` and emits the MetBench
normalized parsed-output shape:

    {
      "values":   { "N_A_final": <float>, "N_B_final": <float>,
                    "N_C_final": <float>, "N_B_peak": <float>,
                    "total": <float>, "num_steps": <int as float> },
      "metadata": { "program": "decay_chain", "t_final": "<float>" }
    }

Invocation:

    python decay_chain_output_adapter.py parse-output --output-file <result.json>
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def parse_output(output_file: str) -> dict:
    path = Path(output_file)
    payload = json.loads(path.read_text(encoding="utf-8"))

    for key in ("N_A_final", "N_B_final", "N_C_final", "N_B_peak",
                "total", "num_steps", "t_final"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in decay-chain output")

    return {
        "values": {
            "N_A_final": float(payload["N_A_final"]),
            "N_B_final": float(payload["N_B_final"]),
            "N_C_final": float(payload["N_C_final"]),
            "N_B_peak":  float(payload["N_B_peak"]),
            "total":     float(payload["total"]),
            "num_steps": float(payload["num_steps"]),
        },
        "metadata": {
            "program": "decay_chain",
            "t_final": str(payload["t_final"]),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Decay-chain output adapter")
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
