"""db_to_feature: 从 JSON catalog 描述生成 .feature 文件骨架。

v2 设计中 LiteDB 是真理源，`.feature` 是视图；本工具是 feature_to_db 的反向：
读 JSON catalog (从 C# 端导出或 P5.3 迁移产出)，按 MR Schema 生成
`metbench/catalog/features/<metapattern>/<code>-<name>.feature` 骨架。

骨架包含：
* @tags 头（metapattern / assertion / value / noise_aware / tolerance_rel）
* Feature: <code> — <name>
* Background: <description-placeholder>
* Scenario Outline + 通用 5 个 step
* Examples 表（按 bindings 数据生成）

留空待人工补：物理推导 (Physics rationale) 段。

CLI:
    python tools/db_to_feature.py --catalog catalog.json --output metbench/catalog/features/
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


_FEATURE_TEMPLATE = """\
@metapattern:{metapattern} @assertion:{assertion} @value:{value_name}
@noise_aware:{noise_aware} @tolerance_rel:{tolerance_rel}
Feature: {code} — {name}

  Background:
{description}

  Scenario Outline: Apply {code} to <sut> with factor <factor>
    Given the MR Schema "{code}" is bound to SUT "<sut>"
    And the binding uses sample case "<sample>"
    And the parameter mapping for "{abstract_field}" is configured
    When the MT pipeline runs with parameter "factor"="<factor>"
    Then the {noise_aware_prefix}"{assertion_short}" assertion holds on "{value_name}"

    Examples:
{examples_table}
"""


def _format_description(description: str) -> str:
    """缩进 description 2 空格。空时填占位。"""
    if not description.strip():
        return "    TODO: Add physics rationale (e.g., Doppler effect, symmetry argument)."
    return "\n".join("    " + line for line in description.splitlines())


def _format_examples(bindings: list[dict[str, str]], headers: list[str]) -> str:
    """生成 Examples 表 Markdown 风格。"""
    if not bindings:
        # 占位行
        return "      | sut       | sample              | factor |\n" \
               "      | TODO-sut  | TODO-sample.json    | 1.5    |"
    # 表头
    header_line = "      | " + " | ".join(headers) + " |"
    rows = []
    for b in bindings:
        row = "      | " + " | ".join(str(b.get(h, "")) for h in headers) + " |"
        rows.append(row)
    return "\n".join([header_line, *rows])


def _short_assertion(code: str) -> str:
    """less-noise-aware → less。"""
    return code.replace("-noise-aware", "").replace("noise-aware-", "")


def schema_to_feature(schema: dict[str, Any], bindings: list[dict[str, str]]) -> str:
    """根据 schema + bindings 渲染 .feature 文本。"""
    assertion_code = schema.get("assertion_type_code", "less")
    noise_aware = schema.get("noise_aware", False)
    headers_seen: list[str] = []
    for b in bindings:
        for k in b:
            if k != "mr_code" and k not in headers_seen:
                headers_seen.append(k)
    if not headers_seen:
        headers_seen = ["sut", "sample", "factor"]

    abstract_field = schema.get("abstract_field_name", "<TODO-abstract-field>")

    return _FEATURE_TEMPLATE.format(
        metapattern=schema.get("meta_pattern_code", "m_mono"),
        assertion=assertion_code,
        value_name=schema.get("value_name", "k_eff"),
        noise_aware=str(noise_aware).lower(),
        tolerance_rel=schema.get("tolerance_rel", 0.0),
        code=schema.get("code", "TODO"),
        name=schema.get("name", "TODO"),
        description=_format_description(schema.get("description", "")),
        abstract_field=abstract_field,
        noise_aware_prefix="noise-aware " if noise_aware else "",
        assertion_short=_short_assertion(assertion_code),
        examples_table=_format_examples(bindings, headers_seen),
    )


def render_catalog(catalog: dict[str, Any], output_dir: Path) -> list[Path]:
    """渲染整个 catalog 到 output_dir，按 MetaPattern 分子目录。"""
    written: list[Path] = []
    bindings_by_mr: dict[str, list[dict[str, str]]] = {}
    for b in catalog.get("bindings", []):
        mr_code = b.get("mr_code", "")
        if mr_code:
            bindings_by_mr.setdefault(mr_code, []).append(
                {k: v for k, v in b.items() if k != "mr_code"})

    for schema in catalog.get("schemas", []):
        mp = schema.get("meta_pattern_code", "uncategorized")
        code = schema.get("code", "UNKNOWN")
        name = schema.get("name", "").replace(" ", "-").replace("/", "-")
        # 安全的文件名：仅取 code + 简化 name
        safe_name = "".join(c if c.isalnum() or c in "-_." else "-" for c in (name or "MR"))
        filename = f"{code}-{safe_name}.feature"

        target_dir = output_dir / mp
        target_dir.mkdir(parents=True, exist_ok=True)
        target = target_dir / filename

        feature_text = schema_to_feature(schema, bindings_by_mr.get(code, []))
        target.write_text(feature_text, encoding="utf-8")
        written.append(target)

    return written


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--catalog", required=True, help="Input JSON catalog file")
    parser.add_argument("--output", required=True, help="Output directory for .feature files")
    args = parser.parse_args()

    catalog_path = Path(args.catalog)
    if not catalog_path.is_file():
        print(f"error: catalog file {catalog_path} not found", file=sys.stderr)
        return 2

    catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)

    written = render_catalog(catalog, output_dir)
    print(f"wrote {len(written)} .feature files to {output_dir}")
    for w in written:
        print(f"  {w}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
