"""Output parser for the 1D subchannel SUT (v2 API).

Reads JSON output of subchannel_1d.py and emits MetBench-normalized
{values, metadata}.

Invocation:

    python subchannel_1d_output_parser.py parse --output-file <path>
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    path = Path(output_file)
    payload = json.loads(path.read_text(encoding="utf-8"))

    for key in ("T_out", "delta_T", "heat_input", "delta_p", "channel_length"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in subchannel-1d output")

    return {
        "values": {
            "T_out":      float(payload["T_out"]),
            "delta_T":    float(payload["delta_T"]),
            "heat_input": float(payload["heat_input"]),
            "delta_p":    float(payload["delta_p"]),
        },
        "metadata": {
            "program":        "subchannel_1d",
            "channel_length": str(payload["channel_length"]),
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
