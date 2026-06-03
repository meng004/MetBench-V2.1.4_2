"""Pure-stdlib point-kinetics surrogate for Minimum-MR-SubSet P5."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def _rhs(power: float, precursor: float, rho: float, beta: float, lam: float, generation_time: float) -> tuple[float, float]:
    dp = ((rho - beta) / generation_time) * power + lam * precursor
    dc = (beta / generation_time) * power - lam * precursor
    return dp, dc


def _rk4_step(power: float, precursor: float, dt: float, rho: float, beta: float, lam: float, generation_time: float) -> tuple[float, float]:
    k1p, k1c = _rhs(power, precursor, rho, beta, lam, generation_time)
    k2p, k2c = _rhs(power + 0.5 * dt * k1p, precursor + 0.5 * dt * k1c, rho, beta, lam, generation_time)
    k3p, k3c = _rhs(power + 0.5 * dt * k2p, precursor + 0.5 * dt * k2c, rho, beta, lam, generation_time)
    k4p, k4c = _rhs(power + dt * k3p, precursor + dt * k3c, rho, beta, lam, generation_time)
    next_power = power + (dt / 6.0) * (k1p + 2.0 * k2p + 2.0 * k3p + k4p)
    next_precursor = precursor + (dt / 6.0) * (k1c + 2.0 * k2c + 2.0 * k3c + k4c)
    return next_power, next_precursor


def solve(case: dict) -> dict:
    kinetics = case["kinetics"]
    time = case["time"]
    initial = case["initial"]

    rho = float(kinetics["rho"])
    beta = float(kinetics["beta"])
    lam = float(kinetics["lambda"])
    generation_time = float(kinetics["generation_time"])
    t_start = float(time["t_start"])
    t_end = float(time["t_end"])
    n_steps = int(time["n_steps"])
    if n_steps < 1:
        raise ValueError("time.n_steps must be >= 1")
    if t_end <= t_start:
        raise ValueError("time.t_end must be greater than time.t_start")

    power = float(initial["power"])
    precursor = float(initial["precursor"])
    dt = (t_end - t_start) / n_steps
    powers = [power]

    for _ in range(n_steps):
        power, precursor = _rk4_step(power, precursor, dt, rho, beta, lam, generation_time)
        powers.append(power)

    return {
        "max_power": max(powers),
        "min_power": min(powers),
        "final_power": power,
        "final_precursor": precursor,
        "rho": rho,
        "n_steps": n_steps,
        "dt": dt,
    }


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
