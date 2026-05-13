"""Output parser for the OpenMOC pin-cell SUT (v2 API).

Replaces `openmoc_output_adapter.py` in v2 naming. Reads the JSON
written by `openmoc_runner.py` and emits the MetBench-normalized
{values, metadata} payload that v2 Pipeline consumes.

Invocation contract (CLI):

    python openmoc_output_parser.py parse --output-file <path>

    stdout JSON:
    {
      "values":   { "k_eff": ..., "iterations": ..., "converged": 0.0 | 1.0 },
      "metadata": { "adapter": "openmoc", "outputFile": "<absolute path>" }
    }

`converged` is folded into the values map as a numeric 1.0/0.0 because
the v2 Result schema uses Dictionary<string, double>.

Library API:

    from openmoc_output_parser import parse
    payload = parse("source.out.json")
    k_eff = payload["values"]["k_eff"]
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    """Read SUT-native output → MetBench-normalized {values, metadata}."""
    output_path = Path(output_file)
    payload = json.loads(output_path.read_text(encoding="utf-8"))
    converged_raw = payload["converged"]
    if not isinstance(converged_raw, bool):
        raise TypeError(
            f"'converged' must be a JSON bool, got {type(converged_raw).__name__}: {converged_raw!r}"
        )
    values = {
        "k_eff": float(payload["k_eff"]),
        "iterations": float(payload["iterations"]),
        "converged": 1.0 if converged_raw else 0.0,
    }
    return {
        "values": values,
        "metadata": {
            "adapter": "openmoc",
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
