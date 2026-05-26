"""Output parser for the SciPy IVP Lotka-Volterra SUT (v2 API).

Pure stdlib (no scipy import). Reads JSON output of `scipy_ivp_lotka_volterra.py`
and emits MetBench-normalized {values, metadata}.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    path = Path(output_file)
    payload = json.loads(path.read_text(encoding="utf-8"))

    for key in ("mean_prey", "mean_predator", "peak_prey", "peak_predator",
                "prey_final", "predator_final", "num_eval_points", "t_final"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in scipy-ivp-lotka-volterra output")

    return {
        "values": {
            "mean_prey":        float(payload["mean_prey"]),
            "mean_predator":    float(payload["mean_predator"]),
            "peak_prey":        float(payload["peak_prey"]),
            "peak_predator":    float(payload["peak_predator"]),
            "prey_final":       float(payload["prey_final"]),
            "predator_final":   float(payload["predator_final"]),
            "num_eval_points":  float(payload["num_eval_points"]),
        },
        "metadata": {
            "program": "scipy_ivp_lotka_volterra",
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
