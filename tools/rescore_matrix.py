"""Recompute cell outcomes against the current SCENARIOS / evaluate_mr config.

After changes to scenarios (e.g. adding `noise_aware: True`) or to
`evaluate_mr` itself, the cached `outcome` fields in
`docs/experiments/_data/candidates/*/matrix.json` reflect the old
config. This tool walks every matrix.json, re-evaluates each "ran"
cell against the current scenario rules, and rewrites the outcome
(and `assertion_passed`) in place.

It does NOT re-run the solvers — the underlying k_eff and k_eff_std
numbers in each cell are preserved. This is the right thing when the
*decision rule* changed, not the *measurements*.

Usage:
    python3 tools/rescore_matrix.py
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
CANDIDATES_DIR = REPO_ROOT / "docs" / "experiments" / "_data" / "candidates"

sys.path.insert(0, str(REPO_ROOT / "tools"))
from mutation_study import SCENARIOS, evaluate_mr

scenario_by_id = {sc["id"]: sc for sc in SCENARIOS}

total_cells = 0
flipped = []  # (mutation_id, scenario_id, old_outcome, new_outcome)

for mut_dir in sorted(CANDIDATES_DIR.iterdir()):
    matrix_path = mut_dir / "matrix.json"
    if not matrix_path.exists():
        continue
    data = json.loads(matrix_path.read_text())
    changed = False
    for cell in data["cells"]:
        if cell.get("status") != "ran":
            continue
        sc = scenario_by_id.get(cell["scenario_id"])
        if sc is None:
            continue
        total_cells += 1
        old_outcome = cell.get("outcome")
        passed = evaluate_mr(cell, sc)
        new_outcome = "missed" if passed else "detected"
        if new_outcome != old_outcome:
            flipped.append((data["mutation_id"], cell["scenario_id"], old_outcome, new_outcome))
            cell["outcome"] = new_outcome
            cell["assertion_passed"] = passed
            changed = True
    if changed:
        matrix_path.write_text(json.dumps(data, indent=2) + "\n")

print(f"Rescored {total_cells} 'ran' cells across all mutations.")
print(f"Outcome flips: {len(flipped)}")
for mid, sid, old, new in flipped:
    print(f"  {mid:60s} {sid:38s} {old} → {new}")
