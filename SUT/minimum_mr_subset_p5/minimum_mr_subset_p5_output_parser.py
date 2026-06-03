from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    p_parse = sub.add_parser("parse")
    p_parse.add_argument("--output-file", required=True)
    args = parser.parse_args()
    data = json.loads(Path(args.output_file).read_text(encoding="utf-8"))
    values = {key: value for key, value in data.items() if isinstance(value, (int, float))}
    json.dump({"values": values, "metadata": {"adapter": "minimum-mr-subset-p5"}}, sys.stdout, ensure_ascii=False)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
