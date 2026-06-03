"""Pure-stdlib live launcher surrogate for Minimum-MR-SubSet P5 point kinetics."""
from __future__ import annotations

import argparse
import json
from pathlib import Path


def _load(path: str) -> dict:
    return json.loads(Path(path).read_text(encoding="utf-8"))


def run_model(data: dict) -> dict:
    params = data.get("params", {})
    reactivity = float(params.get("reactivity", 0.01))
    precursor0 = float(params.get("precursor0", 1.0))
    steps = int(params.get("steps", 8))
    dt = float(params.get("dt", 0.1))
    times = [i * dt for i in range(steps)]
    power = [1.0 + reactivity * (i + 1) for i in range(steps)]
    precursor = [precursor0 / (1.0 + reactivity * (i + 1)) for i in range(steps)]
    extrema = max(power) - min(power)
    return {
        "t": times,
        "power": power,
        "precursor": precursor,
        "power_extrema": extrema,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    result = run_model(_load(args.input))
    Path(args.output).parent.mkdir(parents=True, exist_ok=True)
    Path(args.output).write_text(json.dumps(result, ensure_ascii=False), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
