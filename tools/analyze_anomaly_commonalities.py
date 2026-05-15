"""analyze_anomaly_commonalities: Anomaly 共性分析（Python helper）。

C# 端 `AnomalyService.AnalyzeCommonalities` 的镜像 — 给 dashboard.html /
ad-hoc 调研脚本提供同样的统计。

输入：JSON 文件，含 Anomaly 列表（与 LiteDB 导出的 Anomaly collection 兼容）
输出：CommonalityReport JSON（Severity/Category/Status 分布 + dominant + hypothesis）

CLI:
    python tools/analyze_anomaly_commonalities.py --anomalies anomalies.json --output report.json

库 API:
    from analyze_anomaly_commonalities import analyze
    report = analyze(anomalies_list)
"""

from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from pathlib import Path
from typing import Any


def analyze(anomalies: list[dict[str, Any]]) -> dict[str, Any]:
    """统计 Anomaly 列表 → CommonalityReport dict（与 C# CommonalityReport 字段对应）。"""
    total = len(anomalies)
    if total == 0:
        return _empty_report()

    by_sev = Counter(a.get("Severity", "unknown") for a in anomalies)
    by_cat = Counter(a.get("Category", "unknown") for a in anomalies)
    by_status = Counter(a.get("Status", "unknown") for a in anomalies)

    dominant_sev = by_sev.most_common(1)[0][0] if by_sev else None
    dominant_cat = by_cat.most_common(1)[0][0] if by_cat else None

    noise_count = by_sev.get("noise", 0)
    major_critical = by_sev.get("major", 0) + by_sev.get("critical", 0)
    linked_count = sum(1 for a in anomalies if a.get("LinkedKnownBugId") is not None)

    hypothesis = _build_hypothesis(
        total, dominant_sev, dominant_cat, noise_count, major_critical, linked_count)

    return {
        "Total": total,
        "BySeverity": dict(by_sev),
        "ByCategory": dict(by_cat),
        "ByStatus": dict(by_status),
        "DominantSeverity": dominant_sev,
        "DominantCategory": dominant_cat,
        "NoiseFloorCount": noise_count,
        "MajorOrCriticalCount": major_critical,
        "LinkedToKnownBugCount": linked_count,
        "Hypothesis": hypothesis,
    }


def _empty_report() -> dict[str, Any]:
    return {
        "Total": 0,
        "BySeverity": {},
        "ByCategory": {},
        "ByStatus": {},
        "DominantSeverity": None,
        "DominantCategory": None,
        "NoiseFloorCount": 0,
        "MajorOrCriticalCount": 0,
        "LinkedToKnownBugCount": 0,
        "Hypothesis": "No anomalies to analyze.",
    }


def _build_hypothesis(total: int, dom_sev: str | None, dom_cat: str | None,
                      noise: int, major: int, linked: int) -> str:
    parts = [f"{total} anomalies analyzed."]
    if noise > 0:
        parts.append(f"{noise}/{total} are MC noise-floor — not real bugs.")
    if major > 0:
        parts.append(f"{major}/{total} are major/critical (real-bug candidates).")
    if linked > 0:
        parts.append(f"{linked}/{total} reproduce known bugs (LinkedKnownBugId set).")
    if dom_sev and total > 0:
        parts.append(f"Dominant severity: {dom_sev}.")
    if dom_cat and total > 0:
        parts.append(f"Dominant category: {dom_cat}.")

    # 启发式分类
    if dom_cat == "basin":
        parts.append("→ Hypothesis: SUT has non-physical convergence basin at specific parameter slivers.")
    elif dom_cat == "mc-floor":
        parts.append("→ Hypothesis: MR sensitivity floor reached; consider raising particle count.")
    elif dom_cat == "cross-program":
        parts.append("→ Hypothesis: Solver implementation difference — investigate m_cmp.")

    return " ".join(parts)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--anomalies", required=True, help="JSON file with Anomaly list")
    parser.add_argument("--output", required=False, help="Output JSON file (defaults to stdout)")
    args = parser.parse_args()

    anomalies_path = Path(args.anomalies)
    if not anomalies_path.is_file():
        print(f"error: {anomalies_path} not found", file=sys.stderr)
        return 2

    raw = json.loads(anomalies_path.read_text(encoding="utf-8"))
    # 兼容两种输入：list 或 dict 含 "anomalies" 字段
    anomalies = raw if isinstance(raw, list) else raw.get("anomalies", [])
    report = analyze(anomalies)

    out_text = json.dumps(report, indent=2, ensure_ascii=False)
    if args.output:
        Path(args.output).write_text(out_text + "\n", encoding="utf-8")
        print(f"wrote {args.output}: Total={report['Total']}, "
              f"Major+Critical={report['MajorOrCriticalCount']}, Noise={report['NoiseFloorCount']}")
    else:
        print(out_text)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
