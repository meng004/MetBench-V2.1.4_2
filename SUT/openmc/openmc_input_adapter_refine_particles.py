"""Input adapter for the OpenMC pin-cell SUT — RefineParticles.

Implements NOETHER candidate MR12 (m_conv / B5 limit, Monte-Carlo
convergence rate). Multiplies `solver.particles` by `factor`. Also
ensures the relevant solver section exists (the SUT runner falls back
to defaults if the JSON is silent).

The expected MR is the textbook 1/√N law:

    k_eff_std_followup / k_eff_std_source  ≈  1 / sqrt(factor)

The runner reports `k_eff_std`. A bug that double-counts samples,
treats inactive batches as active, or omits the variance reduction
entirely will violate this scaling.

OpenMC-only: OpenMOC is deterministic and reports no statistical
uncertainty.

Invocation contract:

    python openmc_input_adapter_refine_particles.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{"factor": "10"}'
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

DEFAULT_PARTICLES = 5000  # matches the OpenMC runner's fallback


def transform_input(source_file: str, output_file: str, params_json: str) -> dict:
    params = json.loads(params_json)
    if "factor" not in params:
        raise KeyError("Missing required parameter 'factor'")
    factor = float(params["factor"])
    if factor <= 0:
        raise ValueError(f"factor must be > 0 (got {factor})")

    source_path = Path(source_file)
    output_path = Path(output_file)
    case = json.loads(source_path.read_text(encoding="utf-8"))

    case.setdefault("solver", {})
    old_particles = int(case["solver"].get("particles", DEFAULT_PARTICLES))
    new_particles = max(1, int(round(old_particles * factor)))
    case["solver"]["particles"] = new_particles

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "RefineParticles",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {"factor": factor},
        "log": f"Refined solver.particles by {factor}: {old_particles} -> {new_particles}",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMC RefineParticles input adapter")
    sub = parser.add_subparsers(dest="command", required=True)
    p_t = sub.add_parser("transform-input")
    p_t.add_argument("--source-file", required=True)
    p_t.add_argument("--output-file", required=True)
    p_t.add_argument("--params", required=True)
    args = parser.parse_args()

    if args.command == "transform-input":
        print(json.dumps(transform_input(args.source_file, args.output_file, args.params), ensure_ascii=False))
        return 0
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
