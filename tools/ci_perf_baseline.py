"""ci_perf_baseline: parse vstest trx → enforce test suite timing budgets.

CI 性能回归 gate (F14):
  • 总时长 > total-budget-seconds → exit 1 (fail CI)
  • 单 test > per-test-warn-ms → 打印 warn 但 exit 0（保留余地）
  • 当前 baseline: ~15s total / fastest test < 1ms / slowest known ~1s (OpenMOC subprocess timeout smoke)

trx 文件结构（vstest standard）:
  <TestRun>
    <ResultSummary>
      <Counters total="N" passed=".." failed=".." />
    </ResultSummary>
    <Times start="..." finish="..." />
    <Results>
      <UnitTestResult duration="00:00:00.0123456" testName="..." outcome="Passed" />
      ...
    </Results>
  </TestRun>
"""

from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"v": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def parse_duration(d: str) -> float:
    """trx duration is 'HH:MM:SS.fffffff' or similar; return seconds."""
    parts = d.split(":")
    h, m = int(parts[0]), int(parts[1])
    s = float(parts[2])
    return h * 3600 + m * 60 + s


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--trx", required=True, help="Path to .trx test results file.")
    parser.add_argument("--total-budget-seconds", type=float, default=120.0,
                        help="Total test suite budget. Exceed → fail.")
    parser.add_argument("--per-test-warn-ms", type=float, default=2000.0,
                        help="Per-test warn threshold. Exceed → warn (not fail).")
    args = parser.parse_args()

    trx_path = Path(args.trx)
    if not trx_path.exists():
        print(f"error: trx file not found at {trx_path}", file=sys.stderr)
        return 2

    tree = ET.parse(trx_path)
    root = tree.getroot()

    # ===== Total wall-clock =====
    times_el = root.find("v:Times", NS)
    if times_el is None:
        print("error: <Times> element missing from trx", file=sys.stderr)
        return 2
    start = times_el.get("start", "")
    finish = times_el.get("finish", "")
    # ISO 8601 → ignore parsing here, use sum of durations as proxy if Times missing
    # Actually compute sum of per-test durations — more reliable than wall-clock for CI.
    total_seconds = 0.0
    slow_tests: list[tuple[str, float]] = []

    for ur in root.iter("{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult"):
        dur_str = ur.get("duration") or "00:00:00"
        secs = parse_duration(dur_str)
        total_seconds += secs
        ms = secs * 1000
        if ms > args.per_test_warn_ms:
            slow_tests.append((ur.get("testName", "<unknown>"), ms))

    print("===== CI perf baseline =====")
    print(f"  Trx file:           {trx_path}")
    print(f"  Total cumulative:   {total_seconds:.2f}s  (budget: {args.total_budget_seconds:.0f}s)")
    print(f"  Wall-clock window:  {start} → {finish}")
    print(f"  Slow tests (>{args.per_test_warn_ms:.0f}ms): {len(slow_tests)}")
    for name, ms in sorted(slow_tests, key=lambda t: -t[1])[:10]:
        print(f"    {ms:8.0f}ms  {name}")

    if total_seconds > args.total_budget_seconds:
        print(f"\n❌ FAIL: total test time {total_seconds:.1f}s exceeds budget {args.total_budget_seconds:.0f}s.",
              file=sys.stderr)
        return 1

    if slow_tests:
        print(f"\n⚠ WARN: {len(slow_tests)} test(s) slower than {args.per_test_warn_ms:.0f}ms.")
        print("  Not failing — but consider profiling or skipping for fast feedback.")

    print(f"\n✓ PASS: under {args.total_budget_seconds:.0f}s budget.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
