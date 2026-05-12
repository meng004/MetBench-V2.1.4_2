"""Input adapter for the OpenMC pin-cell SUT — ScaleFuelSigmaS.

Mirror of `SUT/openmoc/openmoc_input_adapter_fuel_sigma_s.py`. Same
transformation: scale every entry of fuel.sigma_s (the row-major 2x2
scattering matrix) by `factor`, then recompute fuel.sigma_t to keep
fuel.sigma_a constant. Same MR (k_eff drops for factor < 1).

OpenMC reads sigma_a directly via `xsdata.set_absorption(...)`, so
holding it constant in the JSON guarantees both runners see the same
absorption physics; the only effective change is scattering.

Invocation contract:

    python openmc_input_adapter_fuel_sigma_s.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{"factor": "0.5"}'
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
    n = int(fuel["num_groups"])
    if n != 2:
        raise ValueError(f"ScaleFuelSigmaS expects num_groups=2, got {n}")

    old_sigma_s = list(fuel["sigma_s"])
    if len(old_sigma_s) != n * n:
        raise ValueError(f"fuel.sigma_s must have {n*n} entries, got {len(old_sigma_s)}")
    old_sigma_a = list(fuel["sigma_a"])
    old_sigma_t = list(fuel["sigma_t"])

    new_sigma_s = [factor * v for v in old_sigma_s]
    new_sigma_t = []
    for g_in in range(n):
        row_sum = sum(new_sigma_s[g_in * n + g_out] for g_out in range(n))
        new_sigma_t.append(old_sigma_a[g_in] + row_sum)

    fuel["sigma_s"] = new_sigma_s
    fuel["sigma_t"] = new_sigma_t

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleFuelSigmaS",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": (
            f"Scaled fuel.sigma_s by {factor}: {old_sigma_s} -> {new_sigma_s} ; "
            f"fuel.sigma_t recomputed (sigma_a held): {old_sigma_t} -> {new_sigma_t}"
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC ScaleFuelSigmaS input adapter")
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
