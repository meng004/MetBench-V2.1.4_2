"""Input adapter for the OpenMOC pin-cell SUT — PermuteEnergyGroups.

Implements NOETHER candidate MR04 (m_inv / B1 symmetry on the energy-group
index permutation group). Swaps the per-group entries of every cross
section array in `materials.fuel` and `materials.moderator`:

    sigma_t   : [a, b]               -> [b, a]
    sigma_a   : [a, b]               -> [b, a]
    nu_sigma_f: [a, b]               -> [b, a]
    sigma_f   : [a, b]               -> [b, a]
    chi       : [a, b]               -> [b, a]
    sigma_s   : [s00, s01, s10, s11] -> [s11, s10, s01, s00]
                  ^row-major g_in -> g_out, so swap groups means
                  swap both axes of the 2x2 matrix.

The expected MR is `k_eff_followup ≈ k_eff_source within tolerance`: the
eigenvalue is independent of which integer index the developer chose for
each group, provided every per-group array is permuted consistently. A
runner that hard-codes "group 0 is fast" anywhere will violate it.

Limited to num_groups == 2 (the only case the SUT supports).

Invocation contract — identical to other OpenMOC adapters:

    python openmoc_input_adapter_group_permute.py transform-input \
        --source-file <source.json> \
        --output-file <followup.json> \
        --params '{}'
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def _swap2(arr: list) -> list:
    if len(arr) != 2:
        raise ValueError(f"PermuteEnergyGroups requires num_groups=2, got len={len(arr)}: {arr}")
    return [arr[1], arr[0]]


def _swap_scatter_2g(s: list) -> list:
    # row-major 2x2: [s00, s01, s10, s11], indices = [g_in*2 + g_out].
    # Swap groups 0<->1 across both axes: index (i,j) -> (1-i, 1-j).
    if len(s) != 4:
        raise ValueError(f"PermuteEnergyGroups expects 4-element scatter (2x2), got len={len(s)}")
    return [s[3], s[2], s[1], s[0]]


def _permute_material(mat: dict) -> dict:
    if int(mat.get("num_groups", 0)) != 2:
        raise ValueError("PermuteEnergyGroups: only num_groups=2 supported on this SUT")
    out = dict(mat)
    out["sigma_t"]    = _swap2(mat["sigma_t"])
    out["sigma_a"]    = _swap2(mat["sigma_a"])
    out["nu_sigma_f"] = _swap2(mat["nu_sigma_f"])
    out["sigma_f"]    = _swap2(mat["sigma_f"])
    out["chi"]        = _swap2(mat["chi"])
    out["sigma_s"]    = _swap_scatter_2g(mat["sigma_s"])
    return out


def transform_input(source_file: str, output_file: str, params_json: str) -> dict:
    # No factor parameter for a pure permutation; accept and ignore an
    # empty params object so the orchestrator can pass `{}` uniformly.
    json.loads(params_json)  # validate JSON shape

    source_path = Path(source_file)
    output_path = Path(output_file)
    case = json.loads(source_path.read_text(encoding="utf-8"))

    materials = case["materials"]
    materials["fuel"]      = _permute_material(materials["fuel"])
    materials["moderator"] = _permute_material(materials["moderator"])

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(case, indent=2) + "\n", encoding="utf-8")

    return {
        "transformation": "PermuteEnergyGroups",
        "source": str(source_path.resolve()),
        "output": str(output_path.resolve()),
        "params": {},
        "log": "Permuted energy groups 0<->1 across all per-group arrays in fuel and moderator.",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="OpenMOC PermuteEnergyGroups input adapter")
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
