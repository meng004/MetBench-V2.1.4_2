"""Input parser for the Minimum-MR-SubSet P9 surrogate live SUT."""

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
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    parse_cmd = sub.add_parser("parse")
    parse_cmd.add_argument("--input", required=True)
    write_cmd = sub.add_parser("write")
    write_cmd.add_argument("--dict-file", required=True)
    write_cmd.add_argument("--output", required=True)
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
