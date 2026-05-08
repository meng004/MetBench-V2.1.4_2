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


def transform_input(source_file: str, output_file: str, params_json: str) -> dict:
    params = json.loads(params_json)
    multiplier = float(params["multiplier"])
    source_path = Path(source_file)
    output_path = Path(output_file)
    raw = source_path.read_text(encoding="utf-8").strip()
    if not raw:
        raise ValueError(f"Source input file is empty: {source_file}")
    value = float(raw)
    transformed = value * multiplier
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(f"{transformed}\n", encoding="utf-8")
    return {
        "transformation": "ScalarMultiply",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"multiplier": multiplier},
        "log": f"Multiplied {value} by {multiplier} -> {transformed}",
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="command", required=True)

    parse_parser = subparsers.add_parser("parse-output")
    parse_parser.add_argument("--output-file", required=True)

    transform_parser = subparsers.add_parser("transform-input")
    transform_parser.add_argument("--source-file", required=True)
    transform_parser.add_argument("--output-file", required=True)
    transform_parser.add_argument("--params", required=True)

    args = parser.parse_args()

    if args.command == "parse-output":
        print(json.dumps(parse_output(args.output_file), ensure_ascii=False))
        return 0

    if args.command == "transform-input":
        print(json.dumps(transform_input(args.source_file, args.output_file, args.params), ensure_ascii=False))
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
