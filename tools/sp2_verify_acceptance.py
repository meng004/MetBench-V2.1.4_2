#!/usr/bin/env python3
"""SP2 acceptance verifier: reads docs/experiments/_data and checks the
SP2 spec §4 mechanically-checkable properties:

  1. matrix produced for every semantic mutant (screening said semantic
     -> a matrix.json exists with >=1 cell).
  2. Mut00 identity: zero false-positive -> no scenario cell has
     outcome == "detected".
  3. equivalent mutants survive -> no detected cell (survival is correct;
     a detected cell is recorded as an anomaly, not a hard fail here).
  4. semantic mutants detected -> each semantic mutant has >=1 cell with
     outcome == "detected"; semantic mutants with zero detections are
     reported as coverage gaps (recorded, not hidden).

Exit code 0 iff hard properties (1, 2) hold AND baseline + screening are
present. Properties 3/4 anomalies are printed and summarized but do not
flip the exit code (they are T6 findings to record per spec §4).
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

    semantic = [k for k, v in screened.items()
                if v.get("classification") == "semantic"]
    equivalent = [k for k, v in screened.items()
                  if v.get("classification") == "equivalent"]

    hard_ok = True
    anomalies: list[str] = []

    # property 1: every semantic mutant has a matrix with >=1 cell
    missing_matrix = []
    for mid in semantic:
        m = load(CAND / mid / "matrix.json")
        if m is None or not m.get("cells"):
            missing_matrix.append(mid)
    if missing_matrix:
        hard_ok = False
        print(f"FAIL(P1): semantic mutants without matrix: {missing_matrix}")
    else:
        print(f"OK(P1): {len(semantic)} semantic mutants each have a matrix")

    # property 2: Mut00 identity zero false-positive
    id_detected = []
    for mid, s in screened.items():
        if not mut_id_is_identity(mid):
            continue
        m = load(CAND / mid / "matrix.json")
        if m:
            for c in m.get("cells", []):
                if c.get("outcome") == "detected":
                    id_detected.append((mid, c.get("scenario_id")))
        if s.get("classification") == "semantic":
            id_detected.append((mid, "screening=semantic"))
    if id_detected:
        hard_ok = False
        print(f"FAIL(P2): identity mutant flagged detected: {id_detected}")
    else:
        print("OK(P2): Mut00 identity has zero false-positive detections")

    # property 3: equivalent survive (anomaly if detected)
    for mid in equivalent:
        m = load(CAND / mid / "matrix.json")
        if m and any(c.get("outcome") == "detected" for c in m.get("cells", [])):
            anomalies.append(f"equivalent mutant {mid} was detected (unexpected kill)")

    # property 4: semantic detected by >=1 MR (gap if none)
    gaps = []
    for mid in semantic:
        m = load(CAND / mid / "matrix.json")
        detected = m and any(c.get("outcome") == "detected" for c in m.get("cells", []))
        if not detected:
            gaps.append(mid)
    if gaps:
        anomalies.append(f"semantic mutants with NO MR detection (coverage gaps): {gaps}")
    print(f"INFO: semantic={len(semantic)} equivalent={len(equivalent)} "
          f"detected-gaps={len(gaps)}")

    if anomalies:
        print("ANOMALIES (recorded per spec §4, do not flip exit code):")
        for a in anomalies:
            print(f"  - {a}")

    print(f"RESULT: hard_properties_ok={hard_ok}")
    return 0 if hard_ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
