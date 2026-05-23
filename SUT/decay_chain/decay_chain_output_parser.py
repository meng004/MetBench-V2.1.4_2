"""Output parser for the decay-chain SUT (v2 API).

Mirror of `heat_equation_output_parser.py`. Reads JSON output of
`decay_chain.py` and emits MetBench-normalized {values, metadata}.

Invocation:

    python decay_chain_output_parser.py parse --output-file <path>
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
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
