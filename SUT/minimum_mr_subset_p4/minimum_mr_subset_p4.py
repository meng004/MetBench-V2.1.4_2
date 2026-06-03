"""Pure-stdlib live launcher surrogate for Minimum-MR-SubSet P4 pendulum."""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path


def run_model(data: dict) -> dict:
    initial = data.get("initial", {})
    params = data.get("params", {})
    q0 = float(initial.get("q", 0.25))
    p0 = float(initial.get("p", 0.0))
    steps = int(params.get("steps", 8))
    dt = float(params.get("dt", 0.1))
    q = [q0 * math.cos(i * dt) + p0 * math.sin(i * dt) for i in range(steps)]
    p = [p0 * math.cos(i * dt) - q0 * math.sin(i * dt) for i in range(steps)]
    energy0 = 0.5 * (q0 * q0 + p0 * p0)
    return {"q": q, "p": p, "energy": energy0}


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
