"""Input parser for the projectile SUT (v2 pipeline API).

Contract:
    parse  --input <file>            → JSON {"v0": f, "theta": f, "g": f}
    write  --dict-file <f> --output <o>  → writes back as "v0 theta g"
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(input_file: str) -> dict:
    parts = Path(input_file).read_text(encoding="utf-8").strip().split()
    if len(parts) != 3:
        raise ValueError(f"Expected 3 whitespace-separated values (v0 theta g), got {len(parts)}")
    return {"v0": float(parts[0]), "theta": float(parts[1]), "g": float(parts[2])}


def write(data: dict, output_file: str) -> None:
    output_path = Path(output_file)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(f"{data['v0']} {data['theta']} {data['g']}\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
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
        data = json.loads(Path(getattr(args, "dict_file")).read_text(encoding="utf-8"))
        write(data, args.output)
        print(json.dumps({"output": str(Path(args.output).resolve())}, ensure_ascii=False))
        return 0
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
