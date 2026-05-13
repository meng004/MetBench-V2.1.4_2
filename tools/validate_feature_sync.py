"""validate_feature_sync: 检查 .feature 文件与 JSON catalog 是否一致。

CI 防漂移工具：
* 用 feature_to_db 把 .feature 目录 → 临时 JSON
* 与 LiteDB 导出的 catalog JSON 对比
* 报告 added / removed / changed 的 MRSchema + MRBinding
* exit code 0 = 一致；1 = 有漂移；2 = 工具错误

CLI:
    python tools/validate_feature_sync.py \\
        --features metbench/catalog/features/ \\
        --catalog catalog-from-db.json
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

# Reuse logic from feature_to_db
sys.path.insert(0, str(Path(__file__).parent))
from feature_to_db import parse_directory  # noqa: E402


def diff_catalogs(features_catalog: dict[str, Any],
                  db_catalog: dict[str, Any]) -> dict[str, Any]:
    """对比两份 catalog → 报告 schema/binding 差异。"""
    feat_schemas = {s["code"]: s for s in features_catalog.get("schemas", [])}
    db_schemas = {s["code"]: s for s in db_catalog.get("schemas", [])}

    added_in_features = sorted(feat_schemas.keys() - db_schemas.keys())
    added_in_db = sorted(db_schemas.keys() - feat_schemas.keys())

    changed: list[dict[str, Any]] = []
    for code in sorted(feat_schemas.keys() & db_schemas.keys()):
        fs = feat_schemas[code]
        ds = db_schemas[code]
        diff_fields = []
        for key in ("meta_pattern_code", "assertion_type_code", "value_name",
                    "noise_aware", "tolerance_rel"):
            if fs.get(key) != ds.get(key):
                diff_fields.append({
                    "field": key,
                    "feature_value": fs.get(key),
                    "db_value": ds.get(key),
                })
        if diff_fields:
            changed.append({"code": code, "diff": diff_fields})

    feat_bindings = {(b.get("mr_code"), b.get("sut", "")): b
                     for b in features_catalog.get("bindings", [])}
    db_bindings = {(b.get("mr_code"), b.get("sut", "")): b
                   for b in db_catalog.get("bindings", [])}
    added_bindings_in_features = sorted(feat_bindings.keys() - db_bindings.keys())
    added_bindings_in_db = sorted(db_bindings.keys() - feat_bindings.keys())

    return {
        "schemas": {
            "in_features_only": added_in_features,
            "in_db_only": added_in_db,
            "changed": changed,
        },
        "bindings": {
            "in_features_only": [{"mr_code": c, "sut": s} for c, s in added_bindings_in_features],
            "in_db_only": [{"mr_code": c, "sut": s} for c, s in added_bindings_in_db],
        },
    }


def is_in_sync(diff: dict[str, Any]) -> bool:
    """判断 diff 是否表明一致。"""
    return (not diff["schemas"]["in_features_only"]
            and not diff["schemas"]["in_db_only"]
            and not diff["schemas"]["changed"]
            and not diff["bindings"]["in_features_only"]
            and not diff["bindings"]["in_db_only"])


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--features", required=True, help="Directory of .feature files")
    parser.add_argument("--catalog", required=True, help="JSON catalog (from LiteDB)")
    parser.add_argument("--json", action="store_true",
                        help="Print diff as JSON instead of human-readable")
    args = parser.parse_args()

    features_dir = Path(args.features)
    catalog_path = Path(args.catalog)
    if not features_dir.is_dir():
        print(f"error: {features_dir} not a directory", file=sys.stderr)
        return 2
    if not catalog_path.is_file():
        print(f"error: {catalog_path} not found", file=sys.stderr)
        return 2

    features_catalog = parse_directory(features_dir)
    db_catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
    diff = diff_catalogs(features_catalog, db_catalog)

    in_sync = is_in_sync(diff)

    if args.json:
        print(json.dumps(diff, indent=2, ensure_ascii=False))
    else:
        if in_sync:
            print("✓ .feature files and LiteDB catalog are in sync.")
        else:
            print("✗ Drift detected:")
            for c in diff["schemas"]["in_features_only"]:
                print(f"  + features has MR '{c}' missing in DB")
            for c in diff["schemas"]["in_db_only"]:
                print(f"  - DB has MR '{c}' missing in features")
            for c in diff["schemas"]["changed"]:
                print(f"  ! MR '{c['code']}' changed:")
                for f in c["diff"]:
                    print(f"      {f['field']}: feature={f['feature_value']!r} db={f['db_value']!r}")
            for b in diff["bindings"]["in_features_only"]:
                print(f"  + binding ({b['mr_code']} × {b['sut']}) in features only")
            for b in diff["bindings"]["in_db_only"]:
                print(f"  - binding ({b['mr_code']} × {b['sut']}) in DB only")

    return 0 if in_sync else 1


if __name__ == "__main__":
    raise SystemExit(main())
