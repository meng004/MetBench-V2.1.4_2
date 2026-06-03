from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


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
        json.dump(json.loads(Path(args.input).read_text(encoding="utf-8")), sys.stdout, ensure_ascii=False)
        return 0
    data = json.loads(Path(args.dict_file).read_text(encoding="utf-8"))
    Path(args.output).parent.mkdir(parents=True, exist_ok=True)
    Path(args.output).write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")
    print(json.dumps({"output": str(Path(args.output).resolve())}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
