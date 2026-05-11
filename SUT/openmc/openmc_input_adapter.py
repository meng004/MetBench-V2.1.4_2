"""Input adapter for the OpenMC pin-cell SUT — ScaleNuSigmaF.

The transformation **and** parameter shape are intentionally identical to
`SUT/openmoc/openmoc_input_adapter.py`. Same MR name, same `factor` parameter,
same JSON field touched (`materials.fuel.nu_sigma_f`). Cross-program AC #6
relies on this alignment: the same `MrTransformation("ScaleNuSigmaF", {factor})`
flows through the launcher to either solver's adapter and produces a
follow-up case the corresponding solver can consume.

Invocation:

    python openmc_input_adapter.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{"factor": "1.5"}'
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def transform_input(source_file: str, output_file: str, params_json: str) -> dict:
    params = json.loads(params_json)
    if "factor" not in params:
        raise KeyError("Missing required parameter 'factor'")
    factor = float(params["factor"])
    if factor <= 0:
        raise ValueError(f"factor must be > 0 (got {factor})")

    source_path = Path(source_file)
    output_path = Path(output_file)
    case = json.loads(source_path.read_text(encoding="utf-8"))

    fuel = case["materials"]["fuel"]
    old_nsf = list(fuel["nu_sigma_f"])
    new_nsf = [v * factor for v in old_nsf]
    fuel["nu_sigma_f"] = new_nsf

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleNuSigmaF",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": f"Scaled fuel.nu_sigma_f by {factor}: {old_nsf} -> {new_nsf}",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC ScaleNuSigmaF input adapter")
    subparsers = parser.add_subparsers(dest="command", required=True)
    transform_parser = subparsers.add_parser("transform-input")
    transform_parser.add_argument("--source-file", required=True)
    transform_parser.add_argument("--output-file", required=True)
    transform_parser.add_argument("--params", required=True)
    args = parser.parse_args()

    if args.command == "transform-input":
        print(json.dumps(transform_input(args.source_file, args.output_file, args.params), ensure_ascii=False))
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
