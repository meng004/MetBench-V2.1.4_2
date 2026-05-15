"""Input adapter for the OpenMC pin-cell SUT — ScaleFuelSigmaT.

Mirror of `SUT/openmoc/openmoc_input_adapter_fuel_sigma_t.py`. Same
transformation: scale fuel sigma_t by `factor`; bump fuel sigma_a by
the matching delta to keep `sigma_a = sigma_t - Σ sigma_s` consistent;
leave sigma_s untouched. Same MR (k_eff drops for factor > 1).

Note that OpenMC's runner reads `sigma_a` directly via
`xsdata.set_absorption(...)`, whereas OpenMOC derives it from
`sigma_t - Σ sigma_s` — but the JSON edits we apply here are
equivalent on both runners because we update both fields in the JSON.

Invocation contract:

    python openmc_input_adapter_fuel_sigma_t.py transform-input \
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
    old_sigma_t = list(fuel["sigma_t"])
    old_sigma_a = list(fuel["sigma_a"])
    delta_t = [(factor - 1.0) * t for t in old_sigma_t]
    new_sigma_t = [t * factor for t in old_sigma_t]
    new_sigma_a = [a + d for a, d in zip(old_sigma_a, delta_t)]
    fuel["sigma_t"] = new_sigma_t
    fuel["sigma_a"] = new_sigma_a

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleFuelSigmaT",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": (
            f"Scaled fuel.sigma_t by {factor}: {old_sigma_t} -> {new_sigma_t} ; "
            f"fuel.sigma_a adjusted by delta={delta_t}"
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC ScaleFuelSigmaT input adapter")
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
