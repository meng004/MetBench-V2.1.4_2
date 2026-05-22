"""Input adapter for the damped-oscillator SUT - ScaleInitialState.

Implements the linearity MR transformation: multiply the initial state
(x0, v0) by a configured factor. The ODE is linear and homogeneous in the
initial state, so the response x(t) -- in particular max_abs_displacement --
is scaled by the same factor. The C# layer uses GreaterThanAssertion to verify
follow-up max_abs_displacement > source when factor > 1.

Invocation:

    python damped_oscillator_input_adapter.py transform-input \
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
    old_x0 = float(initial["x0"])
    old_v0 = float(initial["v0"])
    initial["x0"] = old_x0 * factor
    initial["v0"] = old_v0 * factor

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleInitialState",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": f"Scaled initial (x0, v0) by {factor}: "
               f"({old_x0}, {old_v0}) -> ({initial['x0']}, {initial['v0']})",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Damped-oscillator ScaleInitialState input adapter")
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
