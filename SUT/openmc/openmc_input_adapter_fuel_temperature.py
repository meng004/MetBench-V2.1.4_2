"""Input adapter for the OpenMC pin-cell SUT — RaiseFuelTemperature.

Mirror of `SUT/openmoc/openmoc_input_adapter_fuel_temperature.py`.
Same MR (k_eff drops as fuel T rises) and same default
(T_ref = 600 K when the field is absent).

Invocation contract:

    python openmc_input_adapter_fuel_temperature.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{"factor": "1.5"}'
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

DEFAULT_TEMPERATURE_KELVIN = 600.0


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
    old_t = float(fuel.get("temperature_kelvin", DEFAULT_TEMPERATURE_KELVIN))
    new_t = old_t * factor
    fuel["temperature_kelvin"] = new_t

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "RaiseFuelTemperature",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": f"Raised fuel.temperature_kelvin by {factor}: {old_t} K -> {new_t} K",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC RaiseFuelTemperature input adapter")
    sub = parser.add_subparsers(dest="command", required=True)
    p_t = sub.add_parser("transform-input")
    p_t.add_argument("--source-file", required=True)
    p_t.add_argument("--output-file", required=True)
    p_t.add_argument("--params", required=True)
    args = parser.parse_args()

    if args.command == "transform-input":
        print(json.dumps(transform_input(args.source_file, args.output_file, args.params), ensure_ascii=False))
        return 0
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
