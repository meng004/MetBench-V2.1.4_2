import argparse
import json
from pathlib import Path


def parse_output(output_file: str) -> dict:
    output_path = Path(output_file)
    values = {}
    for line in output_path.read_text(encoding="utf-8").splitlines():
        if "=" not in line:
            continue
        key, raw_value = line.split("=", 1)
        values[key.strip()] = float(raw_value.strip())
    return {
        "values": values,
        "metadata": {
            "adapter": "example",
            "outputFile": str(output_path.resolve()),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="command", required=True)
    parse_parser = subparsers.add_parser("parse-output")
    parse_parser.add_argument("--output-file", required=True)
    args = parser.parse_args()

    if args.command == "parse-output":
        print(json.dumps(parse_output(args.output_file), ensure_ascii=False))
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
