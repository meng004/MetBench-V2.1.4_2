"""Input adapter for the Lotka-Volterra SUT - ScaleGamma.

Implements the MR transformation: multiply the predator death rate `gamma` by
a configured factor. By the Lotka-Volterra time-average identity
<prey> = gamma / delta, scaling gamma scales the time-averaged prey population
by the same factor, so `mean_prey` is monotone in the factor. The C# layer
uses GreaterThanAssertion to verify follow-up mean_prey > source when
factor > 1.

Invocation:

    python lotka_volterra_input_adapter.py transform-input \
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

    sut_params = case["params"]
    old_gamma = float(sut_params["gamma"])
    new_gamma = old_gamma * factor
    sut_params["gamma"] = new_gamma

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleGamma",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": f"Scaled params.gamma by {factor}: {old_gamma} -> {new_gamma}",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Lotka-Volterra ScaleGamma input adapter")
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
