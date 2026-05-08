"""Input adapter for the OpenMOC pin-cell SUT.

Implements the single transformation required by Stage 3:

    ScaleNuSigmaF: multiply the fuel material's per-group `nu_sigma_f`
                   array by a configured `factor` (factor > 0).

The C# `PythonInputAdapter` invokes this script as

    python openmoc_input_adapter.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{"factor": "1.5"}'

The script writes a follow-up JSON identical to the source except for
`materials.fuel.nu_sigma_f`, which is element-wise scaled by `factor`.
All other cross-section arrays (sigma_a, sigma_t, sigma_s, sigma_f,
chi) and geometry/tracking/solver sections are preserved exactly.

stdout JSON (consumed by `PythonInputAdapter.ParseLog`):

    {
      "transformation": "ScaleNuSigmaF",
      "source": "...",
      "output": "...",
      "params":  { "factor": 1.5 },
      "log":     "Scaled fuel.nu_sigma_f by 1.5: [...] -> [...]"
    }
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
    before = list(fuel["nu_sigma_f"])
    after = [v * factor for v in before]
    fuel["nu_sigma_f"] = after

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleNuSigmaF",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": f"Scaled fuel.nu_sigma_f by {factor}: {before} -> {after}",
    }


def main() -> int:
    parser = argparse.ArgumentParser()
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
