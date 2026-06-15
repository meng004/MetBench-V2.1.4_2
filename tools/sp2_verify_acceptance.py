#!/usr/bin/env python3
"""SP2 acceptance verifier: reads docs/experiments/_data and checks the
SP2 spec §4 properties against the mutant author's INTENT
(predicted_classification), not the source-only screening label.

Why predicted_classification: screening runs only the source case, so
adapter mutants (which only bite during the follow-up transform) are
screening-classified "equivalent" even when they are intended semantic
bugs. Keying the equivalent/semantic split off predicted_classification
avoids mislabelling those correct kills as anomalies.

Properties:
  1. (hard) every applicable semantic-intent mutant has a matrix.json
     with >=1 cell (matrix --all ran all mutants).
  2. (hard) Mut00 identity has zero false-positive: no detected cell and
     screening did not call it semantic.
  3. equivalent-intent mutants (true no-ops/identity) that get detected
     are anomalies (MR over-sensitivity / MC noise) -> recorded.
  4. semantic-intent mutants with NO MR detection are coverage gaps
     -> recorded.
  Drifted mutants (screening classification == "error", patch precondition
  no longer matches the SUT source) are reported as inapplicable.

Exit code 0 iff hard properties (1, 2) hold AND baseline + screening are
present. Properties 3/4 + drift are printed and summarized but do not flip
the exit code (they are T6 findings to record per spec §4).
"""
from __future__ import annotations

import json
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
DATA = REPO / "docs" / "experiments" / "_data"
CAND = DATA / "candidates"


def load(p: Path):
    return json.loads(p.read_text(encoding="utf-8")) if p.exists() else None


def mut_id_is_identity(mid: str) -> bool:
    return mid.lower().startswith("mut00")


def detected_cells(matrix: dict | None) -> int:
    if not matrix:
        return 0
    return sum(1 for c in matrix.get("cells", []) if c.get("outcome") == "detected")


def main() -> int:
    baseline = load(DATA / "baseline.json")
    if baseline is None:
        print("FAIL: baseline.json missing")
        return 1
    if not CAND.exists():
        print("FAIL: candidates/ missing")
        return 1

    screened = {}
    for d in sorted(CAND.iterdir()):
        if not d.is_dir():
            continue
        s = load(d / "screening.json")
        if s is not None:
            screened[d.name] = s
    if not screened:
        print("FAIL: no screening.json found")
        return 1

    inapplicable = [k for k, v in screened.items()
                    if v.get("classification") == "error"]
    applicable = {k: v for k, v in screened.items() if k not in inapplicable}

    semantic = [k for k, v in applicable.items()
                if v.get("predicted_classification") == "semantic"]
    equivalent = [k for k, v in applicable.items()
                  if v.get("predicted_classification") == "equivalent"]

    hard_ok = True
    anomalies: list[str] = []

    # P1: every applicable semantic-intent mutant has a matrix with >=1 cell
    missing_matrix = []
    for mid in semantic:
        m = load(CAND / mid / "matrix.json")
        if m is None or not m.get("cells"):
            missing_matrix.append(mid)
    if missing_matrix:
        hard_ok = False
        print(f"FAIL(P1): semantic-intent mutants without matrix: {missing_matrix}")
    else:
        print(f"OK(P1): {len(semantic)} semantic-intent mutants each have a matrix")

    # P2: Mut00 identity zero false-positive
    id_detected = []
    for mid, s in screened.items():
        if not mut_id_is_identity(mid):
            continue
        if detected_cells(load(CAND / mid / "matrix.json")) > 0:
            id_detected.append((mid, "matrix detected"))
        if s.get("classification") == "semantic":
            id_detected.append((mid, "screening=semantic"))
    if id_detected:
        hard_ok = False
        print(f"FAIL(P2): identity mutant flagged: {id_detected}")
    else:
        print("OK(P2): Mut00 identity has zero false-positive detections")

    # P3: equivalent-intent mutants detected -> anomaly
    eq_killed = [mid for mid in equivalent
                 if detected_cells(load(CAND / mid / "matrix.json")) > 0]
    if eq_killed:
        anomalies.append(
            f"equivalent-intent mutants detected (MR over-sensitivity / MC noise): {eq_killed}")

    # P4: semantic-intent mutants with no detection -> coverage gap
    gaps = [mid for mid in semantic
            if detected_cells(load(CAND / mid / "matrix.json")) == 0]
    if gaps:
        anomalies.append(f"semantic-intent mutants with NO MR detection (coverage gaps): {gaps}")

    print(f"INFO: applicable={len(applicable)} semantic-intent={len(semantic)} "
          f"equivalent-intent={len(equivalent)} inapplicable(drifted)={len(inapplicable)}")
    if inapplicable:
        print(f"INFO: drifted/inapplicable mutants (patch precondition no longer matches SUT): "
              f"{inapplicable}")

    if anomalies:
        print("ANOMALIES (recorded per spec §4, do not flip exit code):")
        for a in anomalies:
            print(f"  - {a}")

    print(f"RESULT: hard_properties_ok={hard_ok}")
    return 0 if hard_ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
