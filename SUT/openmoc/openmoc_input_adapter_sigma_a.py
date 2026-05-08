"""Input adapter for the OpenMOC pin-cell SUT - ScaleFuelSigmaA.

Implements the Stage 3+ second MR transformation:

    ScaleFuelSigmaA: multiply the fuel material's per-group absorption
                     cross section (`sigma_a`) by a configured `factor`
                     (factor > 0).

Because OpenMOC's transport solve consumes `sigma_t` and `sigma_s` rather
than `sigma_a` directly (sigma_a = sigma_t - sum(sigma_s_g'->g over g')),
this adapter implements the scaling as

    delta_t_g       = (factor - 1) * old_sigma_a_g
    new sigma_t_g   = old_sigma_t_g + delta_t_g
    new sigma_a_g   = factor       * old_sigma_a_g

Scattering, production (`nu_sigma_f`, `sigma_f`), and the fission spectrum
(`chi`) are byte-for-byte preserved, so the only effective change between
source and follow-up is fuel absorption.

Invocation contract:

    python openmoc_input_adapter_sigma_a.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{"factor": "1.5"}'

stdout JSON (consumed by `PythonInputAdapter.ParseLog`):

    {
      "transformation": "ScaleFuelSigmaA",
      "source":          "...",
      "output":          "...",
      "params":          { "factor": 1.5 },
      "log":             "Scaled fuel.sigma_a by 1.5: [...] -> [...] ; "
                         "fuel.sigma_t adjusted by [...]"
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
    old_sigma_a = list(fuel["sigma_a"])
    old_sigma_t = list(fuel["sigma_t"])
    delta_t = [(factor - 1.0) * a for a in old_sigma_a]
    new_sigma_a = [a * factor for a in old_sigma_a]
    new_sigma_t = [t + d for t, d in zip(old_sigma_t, delta_t)]
    fuel["sigma_a"] = new_sigma_a
    fuel["sigma_t"] = new_sigma_t

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleFuelSigmaA",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": (
            f"Scaled fuel.sigma_a by {factor}: {old_sigma_a} -> {new_sigma_a} ; "
            f"fuel.sigma_t adjusted by {delta_t}"
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMOC ScaleFuelSigmaA input adapter")
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
