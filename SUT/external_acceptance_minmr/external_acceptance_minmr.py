"""Pure-stdlib local SUT slices for external MR acceptance tests."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path


def _run_toy_sort(case: dict) -> dict:
    values = [float(v) for v in case["values"]]
    sorted_values = sorted(values)
    checksum = sum((idx + 1) * value for idx, value in enumerate(sorted_values))
    return {
        "values": {
            "sorted_checksum": checksum,
            "sorted_min": sorted_values[0],
            "sorted_max": sorted_values[-1],
        },
        "metadata": {"kind": "toy_sort"},
    }


def _run_p1_heat(case: dict) -> dict:
    params = case["params"]
    alpha = float(params["alpha"])
    num_points = int(round(float(params["num_points"])))
    num_steps = int(round(float(params["num_steps"])))
    t_final = float(params["t_final"])
    amplitude = float(params["amplitude"])
    if num_points < 5:
        raise ValueError("params.num_points must be >= 5")
    if num_steps < 1:
        raise ValueError("params.num_steps must be >= 1")

    dx = 1.0 / (num_points - 1)
    dt = t_final / num_steps
    courant = alpha * dt / (dx * dx)
    if courant > 0.45:
        raise ValueError(f"unstable explicit heat step: courant={courant}")

    u = [amplitude * math.sin(math.pi * i * dx) for i in range(num_points)]
    u[0] = 0.0
    u[-1] = 0.0
    for _ in range(num_steps):
        nxt = u[:]
        for i in range(1, num_points - 1):
            nxt[i] = u[i] + courant * (u[i - 1] - 2.0 * u[i] + u[i + 1])
        u = nxt

    max_u = max(u)
    mass = sum(u) * dx
    l2_norm = math.sqrt(sum(value * value for value in u) * dx)
    return {
        "values": {
            "max_u": max_u,
            "mass": mass,
            "l2_norm": l2_norm,
        },
        "metadata": {
            "kind": "p1_heat",
            "alpha": str(alpha),
            "num_points": str(num_points),
            "num_steps": str(num_steps),
            "courant": str(courant),
        },
    }


def solve(case: dict) -> dict:
    kind = case.get("kind")
    if kind == "toy_sort":
        return _run_toy_sort(case)
    if kind == "p1_heat":
        return _run_p1_heat(case)
    raise ValueError(f"unknown external acceptance case kind: {kind}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    case = json.loads(Path(args.input).read_text(encoding="utf-8"))
    result = solve(case)
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
