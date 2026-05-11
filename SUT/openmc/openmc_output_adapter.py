"""Output adapter for the OpenMC pin-cell SUT.

Mirror of `SUT/openmoc/openmoc_output_adapter.py`. Reads the JSON written by
`openmc_runner.py` and emits the MetBench-normalized payload
`PythonOutputAdapter` consumes.

Invocation:

    python openmc_output_adapter.py parse-output --output-file <path>

stdout JSON:

    {
      "values":   { "k_eff": ..., "k_eff_std": ..., "batches": ...,
                    "particles": ..., "converged": 0.0 | 1.0 },
      "metadata": { "adapter": "openmc", "outputFile": "<absolute path>" }
    }

`converged` is folded into `values` as 0.0 / 1.0 so the C# layer's
`Dictionary<string, double>` representation can hold it.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def parse_output(output_file: str) -> dict:
    output_path = Path(output_file)
    payload = json.loads(output_path.read_text(encoding="utf-8"))

    converged_raw = payload.get("converged", True)
    if not isinstance(converged_raw, bool):
        raise TypeError(
            f"'converged' must be a JSON bool, got {type(converged_raw).__name__}: {converged_raw!r}"
        )

    for required in ("k_eff", "batches", "particles"):
        if required not in payload:
            raise KeyError(f"missing '{required}' in OpenMC output")

    values = {
        "k_eff": float(payload["k_eff"]),
        "k_eff_std": float(payload.get("k_eff_std", 0.0)),
        "batches": float(payload["batches"]),
        "particles": float(payload["particles"]),
        "converged": 1.0 if converged_raw else 0.0,
    }

    return {
        "values": values,
        "metadata": {
            "adapter": "openmc",
            "outputFile": str(output_path.resolve()),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC output adapter")
    subparsers = parser.add_subparsers(dest="command", required=True)
    parse_parser = subparsers.add_parser("parse-output")
    parse_parser.add_argument("--output-file", required=True)
    args = parser.parse_args()

    if args.command == "parse-output":
        print(json.dumps(parse_output(args.output_file), ensure_ascii=False))
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
