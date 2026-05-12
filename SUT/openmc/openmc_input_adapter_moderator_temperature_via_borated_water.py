"""Input adapter for the OpenMC pin-cell SUT — RaiseModeratorTemperatureViaBoratedWater.

Sister of `openmc_input_adapter_fuel_temperature_via_add_temperature.py`,
but targets the moderator and the *separate* upstream code path broken by
OpenMC PR #3662. Sets `exercise_borated_water: true` on moderator and
multiplies `temperature_kelvin` by `factor`. With the flag, the runner
additionally calls `openmc.model.borated_water(boron_ppm, temperature=t,
density=...)` — exactly the upstream call whose pre-fix code silently
drops the user-supplied temperature whenever an explicit density is also
given.

On installed buggy OpenMC the returned Material has `temperature=None`
(or != t); the runner raises with a recognizable marker. The matrix
records `status=error` for the follow-up cell, i.e. the MR detects the
bug via "follow-up cannot be computed".

On a post-fix OpenMC the call honours the requested temperature and the
follow-up completes with the regular moderator-Doppler shift — same
outcome as the existing MR-moderator-T family.

Invocation contract:

    python openmc_input_adapter_moderator_temperature_via_borated_water.py \\
        transform-input \\
        --source-file <source.json> \\
        --output-file <followup.json> \\
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

    mod = case["materials"]["moderator"]
    old_t = float(mod.get("temperature_kelvin", DEFAULT_TEMPERATURE_KELVIN))
    new_t = old_t * factor
    mod["temperature_kelvin"] = new_t
    # The flag the runner reads to opt into the buggy borated_water path.
    mod["exercise_borated_water"] = True

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "RaiseModeratorTemperatureViaBoratedWater",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": f"Raised moderator.temperature_kelvin by {factor}: {old_t} K -> "
               f"{new_t} K, and set exercise_borated_water=true to trigger "
               f"OpenMC PR #3662.",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
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
