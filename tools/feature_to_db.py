"""feature_to_db: 解析 .feature 文件 → JSON catalog 描述（含 MRSchema + MRBindings）。

v2 设计中 `.feature` 文件是 MR 的人类可读视图，LiteDB 是真理源；
本工具把 `.feature` 文件转成 JSON catalog 描述，供 C# 端 import 工具
upsert 到 LiteDB collection。

输入：.feature 文件目录（递归扫描）
输出：JSON catalog 描述（stdout 或 --output）

`.feature` tags 提取的字段（按 docs/design/v2-system-mt-architecture.md §6.1）：
  @metapattern:<code>           → MRSchema.MetaPatternCode
  @assertion:<code>             → MRSchema.AssertionTypeCode
  @value:<name>                 → MRSchema.ValueName
  @noise_aware:<true|false>     → MRSchema.NoiseAware
  @tolerance_rel:<float>        → MRSchema.ToleranceRel
  Feature: <code> — <name>      → MRSchema.Code + .Name
  Background 段                  → MRSchema.Description (整段)
  Scenario Outline Examples 表  → MRBinding 行（sut + sample + factor）

CLI:
    python tools/feature_to_db.py --features metbench/catalog/features/ --output catalog.json
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any


_TAG_RE = re.compile(r"@(\w+):([^\s]+)")
_FEATURE_HEADER_RE = re.compile(r"^Feature:\s*([\w-]+)\s*—\s*(.+?)\s*$", re.MULTILINE)
# Fallback: Feature: <line>  (没有 dash)
_FEATURE_HEADER_FALLBACK_RE = re.compile(r"^Feature:\s*(.+?)\s*$", re.MULTILINE)


def parse_feature_file(path: Path) -> dict[str, Any]:
    """解析单个 .feature 文件 → MRSchema dict + MRBindings list。"""
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()

    # 1. 提取 tags（在 Feature: 行之前的 @tag 行）
    tags: dict[str, str] = {}
    for line in lines:
        line = line.strip()
        if line.startswith("Feature:"):
            break
        for m in _TAG_RE.finditer(line):
            tags[m.group(1)] = m.group(2)

    # 2. 提取 Feature: <code> — <name>
    m_header = _FEATURE_HEADER_RE.search(text)
    if m_header:
        code = m_header.group(1).strip()
        name = m_header.group(2).strip()
    else:
        m_fb = _FEATURE_HEADER_FALLBACK_RE.search(text)
        if not m_fb:
            raise ValueError(f"{path}: missing Feature: header")
        code = path.stem  # 用文件名 fallback
        name = m_fb.group(1).strip()

    # 3. 提取 Background: 段（作为 Description）
    description = _extract_block(text, r"^\s*Background:\s*\n", r"^\s*(Scenario|Scenario Outline|Example):")

    # 4. 提取 Scenario Outline Examples
    bindings = _extract_examples(text)

    return {
        "schema": {
            "code": code,
            "name": name,
            "meta_pattern_code": tags.get("metapattern", ""),
            "assertion_type_code": tags.get("assertion", ""),
            "value_name": tags.get("value", "k_eff"),
            "noise_aware": tags.get("noise_aware", "false").lower() == "true",
            "tolerance_rel": float(tags.get("tolerance_rel", "0.0")),
            "description": description,
            "feature_file_path": str(path),
        },
        "bindings": bindings,
    }


def _extract_block(text: str, start_re: str, end_re: str) -> str:
    """提取两个 anchor 之间的行（不含 anchor 自身）。"""
    start_match = re.search(start_re, text, re.MULTILINE)
    if not start_match:
        return ""
    start_pos = start_match.end()
    end_match = re.search(end_re, text[start_pos:], re.MULTILINE)
    end_pos = start_pos + end_match.start() if end_match else len(text)
    block = text[start_pos:end_pos].strip()
    # 去掉每行起始的双空格缩进
    return "\n".join(line[2:] if line.startswith("  ") else line
                     for line in block.splitlines())


def _extract_examples(text: str) -> list[dict[str, str]]:
    """提取 Scenario Outline 的 Examples 表，每行成为一个 binding。"""
    bindings: list[dict[str, str]] = []
    # 找 Examples: 关键字后第一个表格
    m = re.search(r"^\s*Examples:\s*$", text, re.MULTILINE)
    if not m:
        return bindings

    body = text[m.end():].strip()
    table_lines = [line.strip() for line in body.splitlines()
                   if line.strip().startswith("|")]
    if len(table_lines) < 2:
        return bindings

    # 表头第一行
    headers = [c.strip() for c in table_lines[0].strip("|").split("|")]
    for row in table_lines[1:]:
        values = [c.strip() for c in row.strip("|").split("|")]
        if len(values) != len(headers):
            continue
        bindings.append(dict(zip(headers, values)))

    return bindings


def parse_directory(features_dir: Path) -> dict[str, Any]:
    """扫描目录下所有 .feature 文件 → catalog 字典。"""
    catalog: dict[str, Any] = {"schemas": [], "bindings": []}
    for feature_path in sorted(features_dir.rglob("*.feature")):
        parsed = parse_feature_file(feature_path)
        catalog["schemas"].append(parsed["schema"])
        for binding in parsed["bindings"]:
            catalog["bindings"].append({
                "mr_code": parsed["schema"]["code"],
                **binding,
            })
    return catalog


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--features", required=True, help="Directory containing .feature files")
    parser.add_argument("--output", required=False, help="Output JSON file (defaults to stdout)")
    args = parser.parse_args()

    features_dir = Path(args.features)
    if not features_dir.is_dir():
        print(f"error: {features_dir} is not a directory", file=sys.stderr)
        return 2

    catalog = parse_directory(features_dir)

    out_text = json.dumps(catalog, indent=2, ensure_ascii=False)
    if args.output:
        Path(args.output).write_text(out_text + "\n", encoding="utf-8")
    else:
        print(out_text)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
