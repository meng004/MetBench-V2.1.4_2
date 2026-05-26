"""Synthetic test SUT runner for the metbench_io helper integration.

Reads a single-record key/value CSV input (one column `factor` required),
echoes the factor as a scalar output named `echo_value`, and emits the
result to a CSV output file via the metbench_io helper.

This SUT exists exclusively to prove that the metbench_io helper round-trips
through the unchanged MetBench launcher / pipeline / catalog without forcing
the framework to know anything about CSV. It has no scientific meaning.

Invocation:

    python _test_csv_runner.py --input case.csv --output result.csv
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

# Add the sibling _shared directory to sys.path so `metbench_io` resolves
# regardless of the working directory the launcher chose.
_HERE = Path(__file__).resolve().parent
_SHARED = _HERE.parent / "_shared"
if str(_SHARED) not in sys.path:
    sys.path.insert(0, str(_SHARED))

from metbench_io import read_input, write_input  # noqa: E402


def solve(case: dict) -> dict:
    params = case.get("params", {})
    factor_raw = params.get("factor")
    if factor_raw is None:
        raise ValueError("_test_csv: input 'params/factor' is required")
    factor = float(factor_raw)
    if factor <= 0.0:
        raise ValueError(f"_test_csv: factor must be > 0, got {factor}")
    return {"params": {"echo_value": str(factor)}}


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--input", required=True)
    p.add_argument("--output", required=True)
    args = p.parse_args()

    case = read_input(args.input, fmt="csv-row")
    result = solve(case)
    write_input(result, args.output, fmt="csv-row")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
