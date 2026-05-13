"""Input parser for the OpenMC pin-cell SUT (v2 API).

Mirror of `openmoc_input_parser.py`. OpenMC's MetBench-side input
schema is identical to OpenMOC's (intentionally — same 2-group
pin-cell shape; differ only at runner-side rendering of the
MGXSLibrary). Parse/write are trivial JSON IO.

Invocation contracts are identical to `openmoc_input_parser.py`:
    python openmc_input_parser.py parse  --input <path>
    python openmc_input_parser.py write  --dict-file <path> --output <path>

Library API:
    from openmc_input_parser import parse, write
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(input_file: str) -> dict:
    return json.loads(Path(input_file).read_text(encoding="utf-8"))


def write(data: dict, output_file: str) -> None:
    output_path = Path(output_file)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    p_parse = sub.add_parser("parse")
    p_parse.add_argument("--input", required=True)

    p_write = sub.add_parser("write")
    p_write.add_argument("--dict-file", required=True)
    p_write.add_argument("--output", required=True)

    args = parser.parse_args()

    if args.command == "parse":
        data = parse(args.input)
        json.dump(data, sys.stdout, ensure_ascii=False)
        return 0

    if args.command == "write":
        data = json.loads(Path(args.dict_file).read_text(encoding="utf-8"))
        write(data, args.output)
        print(json.dumps({"output": str(Path(args.output).resolve())}, ensure_ascii=False))
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
