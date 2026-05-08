"""Output adapter for the OpenMOC pin-cell SUT.

Reads the JSON written by `openmoc_runner.py` and emits the
MetBench-normalized payload `PythonOutputAdapter` consumes.

Invocation contract:

    python openmoc_output_adapter.py parse-output --output-file <path>

stdout JSON:

    {
      "values":   { "k_eff": ..., "iterations": ..., "converged": 0.0 | 1.0 },
      "metadata": { "adapter": "openmoc", "outputFile": "<absolute path>" }
    }

`converged` is folded into the values map as a numeric 1.0/0.0 because
`PythonOutputAdapter` parses values into `Dictionary<string, double>`.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def parse_output(output_file: str) -> dict:
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
    parser = argparse.ArgumentParser()
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
