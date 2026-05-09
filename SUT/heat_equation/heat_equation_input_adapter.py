"""Input adapter for the 1D heat-equation SUT - ScaleAmplitude.

Implements the linearity MR transformation for the heat equation: multiply the
initial profile's `amplitude` by a configured factor. Because the PDE is linear
in the initial condition (with homogeneous Dirichlet BCs), the solution at
t_final is also scaled by the same factor; in particular `max_u` is monotone in
`amplitude` (factor > 0). The C# layer uses the existing GreaterThanAssertion to
verify follow-up max_u > source max_u when factor > 1.

Invocation:

    python heat_equation_input_adapter.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{"factor": "2.0"}'
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

    initial = case["initial"]
    old_amplitude = float(initial["amplitude"])
    new_amplitude = old_amplitude * factor
    initial["amplitude"] = new_amplitude

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleAmplitude",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": f"Scaled initial.amplitude by {factor}: {old_amplitude} -> {new_amplitude}",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Heat-equation ScaleAmplitude input adapter")
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
