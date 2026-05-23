"""Output parser for the projectile SUT (v2 pipeline API).

Contract:
    parse  --output-file <file>  → JSON {"values": {"v0": f, "theta": f, "g": f, "range": f}, "metadata": {...}}
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    values: dict[str, float] = {}
    for line in Path(output_file).read_text(encoding="utf-8").splitlines():
        if "=" not in line:
            continue
        key, raw = line.split("=", 1)
        values[key.strip()] = float(raw.strip())
    return {
        "values": values,
        "metadata": {
            "adapter": "projectile",
            "outputFile": str(Path(output_file).resolve()),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    p_parse = sub.add_parser("parse")
    p_parse.add_argument("--output-file", required=True)
    args = parser.parse_args()

    if args.command == "parse":
        json.dump(parse(getattr(args, "output_file")), sys.stdout, ensure_ascii=False)
        return 0
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
