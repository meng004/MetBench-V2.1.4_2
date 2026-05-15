"""Python TDD: P7 — noether_candidates.py JSON 契约不漂移。

C# MetaPatternDiscoverer 通过 stdout JSON 调用 noether_candidates.py，
本测试锁住 (id / meta_pattern / assertion / realizable_with_current_sut)
四个字段名以及输出 shape，让 C# 端解析永不因 Python 输出改名而崩溃。

跑法：
    python3 tools/tests/test_p7_discovery_contract.py
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
SCRIPT = REPO_ROOT / "tools" / "noether_candidates.py"


def _run_noether() -> dict:
    proc = subprocess.run(
        [sys.executable, str(SCRIPT)],
        capture_output=True, text=True, check=True)
    return json.loads(proc.stdout)


def test_output_has_top_level_candidates_array():
    out = _run_noether()
    assert "candidates" in out, "missing top-level 'candidates' key"
    assert isinstance(out["candidates"], list), "candidates must be array"
    assert len(out["candidates"]) >= 1, "expected ≥1 candidate"


def test_each_candidate_has_required_fields():
    """C# MetaPatternDiscoverer.ParseProposals 需要的最小字段集合。"""
    out = _run_noether()
    required = {"id", "meta_pattern", "assertion", "realizable_with_current_sut"}
    for i, c in enumerate(out["candidates"]):
        missing = required - set(c.keys())
        assert not missing, f"candidate[{i}] missing fields: {missing}"


def test_realizable_subset_at_least_three():
    """C# discoverer 只取 realizable=True 的；至少要给出 3 个。"""
    out = _run_noether()
    realizable = [c for c in out["candidates"] if c.get("realizable_with_current_sut")]
    assert len(realizable) >= 3, f"expected ≥3 realizable; got {len(realizable)}"


def test_assertion_values_in_known_set():
    """assertion 必须落在 C# 端支持的 4 个枚举字面值之一。"""
    out = _run_noether()
    allowed = {"approx", "less", "greater", "bound"}
    for c in out["candidates"]:
        a = c.get("assertion")
        assert a in allowed, f"unknown assertion '{a}' on {c.get('id')}"


def test_metapatterns_table_present():
    out = _run_noether()
    assert "metapatterns" in out, "missing top-level 'metapatterns' key"


def main() -> int:
    print("P7 — Discovery JSON contract")
    tests = [
        test_output_has_top_level_candidates_array,
        test_each_candidate_has_required_fields,
        test_realizable_subset_at_least_three,
        test_assertion_values_in_known_set,
        test_metapatterns_table_present,
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
