"""Python TDD: P6.3 analyze_anomaly_commonalities."""

from __future__ import annotations

import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(REPO_ROOT / "tools"))

from analyze_anomaly_commonalities import analyze


def test_empty_list_returns_zero():
    r = analyze([])
    assert r["Total"] == 0
    assert "No anomalies" in r["Hypothesis"]


def test_severity_counts_correct():
    r = analyze([
        {"Severity": "major"},
        {"Severity": "major"},
        {"Severity": "noise"},
    ])
    assert r["Total"] == 3
    assert r["BySeverity"]["major"] == 2
    assert r["BySeverity"]["noise"] == 1
    assert r["DominantSeverity"] == "major"
    assert r["MajorOrCriticalCount"] == 2
    assert r["NoiseFloorCount"] == 1


def test_category_counts_correct():
    r = analyze([
        {"Category": "basin"},
        {"Category": "basin"},
        {"Category": "mc-floor"},
    ])
    assert r["DominantCategory"] == "basin"
    assert "convergence basin" in r["Hypothesis"]


def test_mcfloor_hypothesis_when_dominant():
    r = analyze([
        {"Category": "mc-floor"},
        {"Category": "mc-floor"},
    ])
    assert "particle count" in r["Hypothesis"]


def test_cross_program_hypothesis_when_dominant():
    r = analyze([
        {"Category": "cross-program"},
        {"Category": "cross-program"},
    ])
    assert "m_cmp" in r["Hypothesis"]


def test_linked_known_bug_counted():
    r = analyze([
        {"Severity": "major", "LinkedKnownBugId": 6},
        {"Severity": "major", "LinkedKnownBugId": 6},
        {"Severity": "major"},
        {"Severity": "major", "LinkedKnownBugId": None},
    ])
    assert r["LinkedToKnownBugCount"] == 2


def test_critical_counted_as_major_or_critical():
    r = analyze([
        {"Severity": "critical"},
        {"Severity": "critical"},
        {"Severity": "noise"},
    ])
    assert r["MajorOrCriticalCount"] == 2


def main() -> int:
    tests = [
        test_empty_list_returns_zero,
        test_severity_counts_correct,
        test_category_counts_correct,
        test_mcfloor_hypothesis_when_dominant,
        test_cross_program_hypothesis_when_dominant,
        test_linked_known_bug_counted,
        test_critical_counted_as_major_or_critical,
    ]
    failures = []
    for t in tests:
        try:
            t()
            print(f"  ✓ {t.__name__}")
        except Exception as e:
            print(f"  ✗ {t.__name__}: {e}")
            failures.append((t.__name__, e))
    if failures:
        print(f"\n{len(failures)} / {len(tests)} tests FAILED")
        return 1
    print(f"\n{len(tests)} / {len(tests)} tests PASSED")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
