"""migrate_mutations_to_v2: Stage 5 mutations.py 48 entries → v2 MutationOperators + Mutants JSON.

输入：`tools/mutations.py::ALL_MUTATIONS`（48 个 Mutation dataclass）
输出：v2 迁移包，含两个集合：
  • MutationOperators (mutation 类型定义，去重)
  • Mutants (按 SUT 的具体应用)

由于 v1 Mutation 把 operator 和 mutant 信息混在一起（每行 id 是
"MutNN-<sut>-runner-<descr>" 或 "MutNN-<descr>"），本脚本把 id 拆出
operator code 与 target SUT，按 category 推断。

CLI:
    python tools/migrate_mutations_to_v2.py --output metbench/catalog/migration/mutations.json
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO_ROOT / "tools"))
from mutations import ALL_MUTATIONS  # noqa: E402


def _infer_category(target_file: str) -> str:
    """从 target_file 推断 Mutation Category。"""
    if "_runner.py" in target_file:
        return "runner-level"
    if "_input_adapter" in target_file or "_input_parser" in target_file:
        return "input-adapter-level"
    if "_output_adapter" in target_file or "_output_parser" in target_file:
        return "output-adapter-level"
    return "other"


def _infer_application(target_file: str) -> str | None:
    """从 target_file 路径推断 SUT 名（None 表示通用 / 不绑定）。"""
    if "openmoc" in target_file.lower():
        return "openmoc"
    if "openmc" in target_file.lower():
        return "openmc"
    if "heat_equation" in target_file.lower() or "heat-equation" in target_file.lower():
        return "heat-equation"
    return None


def migrate() -> dict[str, Any]:
    operators: list[dict[str, Any]] = []
    mutants: list[dict[str, Any]] = []

    for m in ALL_MUTATIONS:
        category = _infer_category(m.target_file)
        sut = _infer_application(m.target_file)
        # 一个 MutationOperator + 一个 Mutant；这里不做 operator 跨 SUT 合并
        # （v1 的 mutations 都是 per-SUT specific，对应 v2 一对一关系）
        operators.append({
            "code": m.id,
            "category": category,
            "target_file_type": Path(m.target_file).name,
            "application_spec": m.description,  # apply 是 lambda，序列化不了
            "predicted_class": m.predicted_classification,
            "related_meta_pattern": (m.predicted_detector[0]
                                     if m.predicted_detector else None),
            "description": m.rationale,
        })
        mutants.append({
            "operator_code": m.id,
            "application_sut": sut,
            "applied_diff": "",  # v1 没存 diff；保留空字符串
            "status": "active",
        })

    return {
        "source": "mutations.py::ALL_MUTATIONS",
        "total": len(ALL_MUTATIONS),
        "operators": operators,
        "mutants": mutants,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", required=False)
    args = parser.parse_args()

    pkg = migrate()
    out = json.dumps(pkg, indent=2, ensure_ascii=False)
    if args.output:
        Path(args.output).write_text(out + "\n", encoding="utf-8")
        print(f"wrote {args.output}: {len(pkg['operators'])} operators, "
              f"{len(pkg['mutants'])} mutants")
    else:
        print(out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
