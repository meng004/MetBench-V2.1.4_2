"""Projectile range calculator (SUT for system-level MT demo).

Implements the explicit projectile-motion equation:

    R = v0**2 * sin(2 * theta) / g

where:
    R     - horizontal range (m)
    v0    - initial speed magnitude (m/s)
    theta - launch angle above the horizontal (degrees)
    g     - gravitational acceleration (m/s^2)

Assumptions:
    - vacuum (no air resistance)
    - launch height equals impact height
    - flat planet, point projectile

Input file format (whitespace-separated, one record per line):

    v0 theta g

Example:
    20  45  9.81

Output file format (key=value lines):

    v0=20.0
    theta=45.0
    g=9.81
    range=40.78...

The output is what the Python output adapter parses and what C# MR
assertions evaluate.
"""

from __future__ import annotations

import argparse
import math
from pathlib import Path


def compute_range(v0: float, theta_deg: float, g: float) -> float:
    """Return the horizontal range in metres for an ideal projectile."""
    if g <= 0:
        raise ValueError(f"g must be positive (got {g})")
    if v0 < 0:
        raise ValueError(f"v0 must be non-negative (got {v0})")
    theta_rad = math.radians(theta_deg)
    return (v0 ** 2) * math.sin(2.0 * theta_rad) / g


def main() -> int:
    parser = argparse.ArgumentParser(description="Projectile range calculator")
    parser.add_argument("--input", required=True, help="Path to whitespace-separated v0 theta g")
    parser.add_argument("--output", required=True, help="Path where key=value results are written")
    args = parser.parse_args()

    input_path = Path(args.input)
    output_path = Path(args.output)

    raw = input_path.read_text(encoding="utf-8").strip()
    if not raw:
        raise ValueError(f"Input file is empty: {args.input}")

    parts = raw.split()
    if len(parts) != 3:
        raise ValueError(
            f"Expected 3 whitespace-separated numbers (v0 theta g), got {len(parts)}: {raw!r}"
        )

    v0 = float(parts[0])
    theta = float(parts[1])
    g = float(parts[2])
    r = compute_range(v0, theta, g)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        f"v0={v0}\n"
        f"theta={theta}\n"
        f"g={g}\n"
        f"range={r}\n",
        encoding="utf-8",
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
