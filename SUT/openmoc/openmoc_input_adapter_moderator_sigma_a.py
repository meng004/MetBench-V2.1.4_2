"""Input adapter for the OpenMOC pin-cell SUT — ScaleModeratorSigmaA.

Implements NOETHER candidate N07 (m_mono / B2 order, parameter
monotonicity in moderator absorption — the classic "soluble boron"
poison response). Multiplies moderator sigma_a by `factor`, with a
matching adjustment to moderator sigma_t so that

    new sigma_t_g  =  old sigma_t_g + (factor - 1) * old sigma_a_g

is the only effective change. Scattering, production, and fission
arrays for the moderator are byte-preserved (the reference moderator
in `pincell.json` already has sigma_f = nu_sigma_f = 0).

Expected MR (for factor > 1): `k_eff_followup < k_eff_source`.
Increased moderator absorption removes neutrons that would otherwise
thermalise and drive fission — standard PWR poison curve.

Invocation contract:

    python openmoc_input_adapter_moderator_sigma_a.py transform-input \
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

    moderator = case["materials"]["moderator"]
    old_sigma_a = list(moderator["sigma_a"])
    old_sigma_t = list(moderator["sigma_t"])
    delta_t = [(factor - 1.0) * a for a in old_sigma_a]
    new_sigma_a = [a * factor for a in old_sigma_a]
    new_sigma_t = [t + d for t, d in zip(old_sigma_t, delta_t)]
    moderator["sigma_a"] = new_sigma_a
    moderator["sigma_t"] = new_sigma_t

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "ScaleModeratorSigmaA",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": (
            f"Scaled moderator.sigma_a by {factor}: {old_sigma_a} -> {new_sigma_a} ; "
            f"moderator.sigma_t adjusted by delta={delta_t}"
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMOC ScaleModeratorSigmaA input adapter")
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
