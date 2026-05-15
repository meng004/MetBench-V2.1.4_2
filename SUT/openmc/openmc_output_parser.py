"""Output parser for the OpenMC pin-cell SUT (v2 API).

Mirror of `SUT/openmoc/openmoc_output_parser.py`. Reads the JSON written
by `openmc_runner.py` and emits MetBench-normalized {values, metadata}.

CLI:
    python openmc_output_parser.py parse --output-file <path>

stdout JSON:
    {
      "values":   { "k_eff": ..., "k_eff_std": ..., "batches": ...,
                    "particles": ..., "converged": 0.0 | 1.0 },
      "metadata": { "adapter": "openmc", "outputFile": "<absolute path>" }
    }
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
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
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    p_parse = sub.add_parser("parse")
    p_parse.add_argument("--output-file", required=True)

    args = parser.parse_args()

    if args.command == "parse":
        result = parse(args.output_file)
        json.dump(result, sys.stdout, ensure_ascii=False)
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
