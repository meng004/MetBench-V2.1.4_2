import argparse
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    input_path = Path(args.input)
    output_path = Path(args.output)
    value = float(input_path.read_text(encoding="utf-8").strip())
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(f"result={value}\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
