"""Output parser for the Minimum-MR-SubSet P9 surrogate live SUT."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def parse(output_file: str) -> dict:
    payload = json.loads(Path(output_file).read_text(encoding="utf-8"))
    for key in ("k_eff", "sigma_k", "reaction_balance", "particles", "enrichment", "surrogate"):
        if key not in payload:
            raise KeyError(f"missing '{key}' in P9 surrogate output")
    if payload["surrogate"] is not True:
        raise ValueError("P9 live runtime must remain explicitly surrogate")
    return {
        "values": {
            "k_eff": float(payload["k_eff"]),
            "sigma_k": float(payload["sigma_k"]),
            "reaction_balance": float(payload["reaction_balance"]),
        },
        "metadata": {
            "program": "minimum_mr_subset_p9_surrogate",
            "particles": str(payload["particles"]),
            "enrichment": str(payload["enrichment"]),
            "surrogate": "true",
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    parse_cmd = sub.add_parser("parse")
    parse_cmd.add_argument("--output-file", required=True)
    args = parser.parse_args()

    if args.command == "parse":
        json.dump(parse(args.output_file), sys.stdout, ensure_ascii=False)
        return 0
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
