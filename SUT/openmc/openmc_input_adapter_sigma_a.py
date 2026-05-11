"""Input adapter for the OpenMC pin-cell SUT — ScaleFuelSigmaA.

Mirror of `SUT/openmoc/openmoc_input_adapter_sigma_a.py`. Same MR name and
parameter shape so AC #6 cross-program scenarios can dispatch one
`MrTransformation("ScaleFuelSigmaA", {factor})` to either solver's adapter.

Scaling fuel absorption multiplies sigma_a per group by `factor > 1`.
OpenMOC's input has both `sigma_t` and `sigma_a` documented; the OpenMOC
adapter additionally bumps `sigma_t` so total - scattering matches the new
absorption (because OpenMOC's transport equation uses sigma_t and sigma_s).
OpenMC's MGXS library accepts sigma_a directly, so this adapter only needs to
update the sigma_a array; sigma_t is recomputed from absorption + scattering
inside the runner before the model is built.

Invocation:

    python openmc_input_adapter_sigma_a.py transform-input \
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
    old_sa = list(fuel["sigma_a"])
    new_sa = [v * factor for v in old_sa]
    fuel["sigma_a"] = new_sa

    # Keep sigma_t consistent with the new absorption: sigma_t_new = sigma_t_old + (factor - 1) * sigma_a_old.
    # Same correction OpenMOC's adapter applies, so both solvers see physically
    # consistent input. The OpenMC runner reads sigma_t from the case and
    # writes it into the MGXS library directly.
    old_st = list(fuel["sigma_t"])
    new_st = [old_st[g] + (factor - 1.0) * old_sa[g] for g in range(len(old_st))]
    fuel["sigma_t"] = new_st

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleFuelSigmaA",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": (
            f"Scaled fuel.sigma_a by {factor}: {old_sa} -> {new_sa}; "
            f"updated sigma_t accordingly: {old_st} -> {new_st}"
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC ScaleFuelSigmaA input adapter")
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
