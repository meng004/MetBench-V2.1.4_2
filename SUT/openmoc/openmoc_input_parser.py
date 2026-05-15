"""Input parser for the OpenMOC pin-cell SUT (v2 API).

Replaces the per-MR `openmoc_input_adapter_*.py` files for v2 architecture.
Responsibility is **format conversion only** — read SUT-native input
JSON into a Python dict, and write a Python dict back to SUT-native
JSON. MR input transformation is **NOT** done here; that is the job
of the C# pipeline's IMRTransformation.

This honors the cleaner v2 separation of concerns:

    Input file  ─ parse  ─→ dict  ─ transform (C#) ─→ dict  ─ write ─→ Input file
       │                                                                     │
       └────── parser (this file) ─────────────────────────────────────────┘
                                                MR-specific logic absent

Invocation contracts (CLI):

    python openmoc_input_parser.py parse --input <path>
        → stdout: JSON of the parsed dict

    python openmoc_input_parser.py write --dict-file <path> --output <path>
        → writes dict-file (JSON) as SUT-native file at <output>
        → stdout: JSON {"output": "<absolute>"}

Library API (for direct Python use):

    from openmoc_input_parser import parse, write
    data = parse("source.json")          # returns dict
    write(data, "followup.json")          # serializes back

OpenMOC's native input format is already JSON, so parse/write are
trivial json.load / json.dump. For other SUTs (MCNP/SCALE/Fortran) the
parser must do format-specific work.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(input_file: str) -> dict:
    """Read SUT-native input file → Python dict."""
    return json.loads(Path(input_file).read_text(encoding="utf-8"))


def write(data: dict, output_file: str) -> None:
    """Write Python dict → SUT-native input file."""
    output_path = Path(output_file)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    p_parse = sub.add_parser("parse", help="Read input file → JSON dict on stdout")
    p_parse.add_argument("--input", required=True)

    p_write = sub.add_parser("write", help="Read dict JSON → write SUT-native file")
    p_write.add_argument("--dict-file", required=True,
                         help="Path to JSON file holding the dict (from C# pipeline)")
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
