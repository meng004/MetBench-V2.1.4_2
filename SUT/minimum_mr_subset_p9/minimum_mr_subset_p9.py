"""Pure-stdlib live launcher surrogate for Minimum-MR-SubSet P9 OpenMC-style criticality."""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path


def run_model(data: dict) -> dict:
    params = data.get("params", {})
    particles = float(params.get("particles", 1000.0))
    absorption = float(params.get("absorption", 0.1))
    production = float(params.get("production", 0.12))
    k_eff = production / absorption
    sigma_k = 0.05 / math.sqrt(particles)
    return {
        "k_eff": k_eff,
        "sigma_k": sigma_k,
        "reaction_balance": production - absorption,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    data = json.loads(Path(args.input).read_text(encoding="utf-8"))
    Path(args.output).parent.mkdir(parents=True, exist_ok=True)
    Path(args.output).write_text(json.dumps(run_model(data), ensure_ascii=False), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
