"""Output parser for the synthetic _test_csv SUT.

Reads the CSV wire format produced by `_test_csv_runner.py` through the
metbench_io helper and emits the MetBench-normalised
`{"values": {...}, "metadata": {...}}` shape that the pipeline expects.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

_HERE = Path(__file__).resolve().parent
_SHARED = _HERE.parent / "_shared"
if str(_SHARED) not in sys.path:
    sys.path.insert(0, str(_SHARED))

from metbench_io import read_input  # noqa: E402


def parse(output_file: str) -> dict:
    raw = read_input(output_file, fmt="csv-row")
    params = raw.get("params", {})
    if "echo_value" not in params:
        raise KeyError("_test_csv output missing 'echo_value' in CSV body")
    return {
        "values": {
            "echo_value": float(params["echo_value"]),
        },
        "metadata": {
            "program": "_test_csv",
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
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
