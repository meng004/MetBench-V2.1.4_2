"""Input parser for the synthetic _test_csv SUT.

Uses the metbench_io helper to read the on-disk CSV wire format and emit the
canonical params-dict shape the MetBench C# pipeline expects (JsonPointer
targets like `/params/factor` work because the underlying dict is identical
to a JSON-sourced one once the helper has decoded the CSV).
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

from metbench_io import read_input, write_input  # noqa: E402


def parse(input_file: str) -> dict:
    """Read CSV input and type-coerce the `factor` field to float so the MR
    transform pipeline (`ScaleField` on `/params/factor`) can multiply it.

    Type-coercion is the SUT-parser's responsibility (not the metbench_io
    helper's) — only the SUT author knows which columns are numeric vs
    categorical. This is the same boundary JSON-native SUTs implicitly use
    via JSON's native number type.
    """
    raw = read_input(input_file, fmt="csv-row")
    params = dict(raw.get("params", {}))
    if "factor" in params:
        params["factor"] = float(params["factor"])
    return {"params": params}


def write(data: dict, output_file: str) -> None:
    """Stringify every value before writing back to CSV so floats round-trip
    as their canonical Python `str(float)` representation.
    """
    params = data.get("params", {})
    serialized = {"params": {str(k): str(v) for k, v in params.items()}}
    write_input(serialized, output_file, fmt="csv-row")


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    p_parse = sub.add_parser("parse")
    p_parse.add_argument("--input", required=True)
    p_write = sub.add_parser("write")
    p_write.add_argument("--dict-file", required=True)
    p_write.add_argument("--output", required=True)
    args = parser.parse_args()

    if args.command == "parse":
        json.dump(parse(args.input), sys.stdout, ensure_ascii=False)
        return 0
    if args.command == "write":
        data = json.loads(Path(args.dict_file).read_text(encoding="utf-8"))
        write(data, args.output)
        print(json.dumps({"output": str(Path(args.output).resolve())}, ensure_ascii=False))
        return 0
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
