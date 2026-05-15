"""build_paper_package: 打包 MetBench 的"论文复现包" → tarball。

包含：
  • metbench/catalog/ 全部 .feature + JSON
  • docs/design/diagrams/*.svg + *.png
  • docs/superpowers/plans/*.md
  • tools/migrate_*_to_v2.py 迁移脚本
  • README.md / CLAUDE.md / AGENTS.md
  • 若提供 --litedb，导出 catalog 表 (MRs/MRBindings/MetaPatterns) 为 JSON

输出：dist/metbench-paper-{date}.tar.gz

CLI:
    python3 tools/build_paper_package.py --output dist/metbench-paper-2026-05-13.tar.gz
"""

from __future__ import annotations

import argparse
import datetime
import os
import sys
import tarfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent

INCLUDED_PATHS = [
    "metbench/catalog",
    "docs/design/diagrams",
    "docs/superpowers/plans",
    "tools/migrate_real_bugs_to_v2.py",
    "tools/migrate_mutations_to_v2.py",
    "tools/feature_to_db.py",
    "tools/validate_feature_sync.py",
    "tools/noether_candidates.py",
    "README.md",
    "CLAUDE.md",
    "AGENTS.md",
]


def build_package(output_path: Path,
                  include_extra: list[Path] | None = None) -> dict:
    """打包指定路径列表 → tarball；返回 manifest。"""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    manifest: dict[str, object] = {
        "generated_at": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "files": [],
        "missing": [],
    }
    files_list: list[dict[str, object]] = manifest["files"]  # type: ignore[assignment]
    missing_list: list[str] = manifest["missing"]            # type: ignore[assignment]

    paths_to_add = list(INCLUDED_PATHS)
    if include_extra:
        paths_to_add.extend(str(p) for p in include_extra)

    with tarfile.open(output_path, "w:gz") as tar:
        for rel in paths_to_add:
            abs_path = (REPO_ROOT / rel).resolve()
            if not abs_path.exists():
                missing_list.append(rel)
                continue
            tar.add(abs_path, arcname=rel)
            if abs_path.is_file():
                files_list.append({"path": rel, "size": abs_path.stat().st_size})
            else:
                for root, _, files in os.walk(abs_path):
                    for f in files:
                        full = Path(root) / f
                        rel_inside = str(full.relative_to(REPO_ROOT))
                        files_list.append({"path": rel_inside, "size": full.stat().st_size})

    return manifest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", required=False,
                        help="Output tarball path (default dist/metbench-paper-<YYYY-MM-DD>.tar.gz)")
    parser.add_argument("--extra", nargs="*", default=[],
                        help="Additional repo-relative paths to include")
    args = parser.parse_args()

    output = Path(args.output) if args.output else \
        REPO_ROOT / "dist" / f"metbench-paper-{datetime.date.today().isoformat()}.tar.gz"

    manifest = build_package(output, [Path(p) for p in args.extra])

    print(f"wrote {output}")
    files_count = len(manifest["files"])  # type: ignore[arg-type]
    print(f"  packaged {files_count} files")
    if manifest["missing"]:
        print(f"  WARN missing: {manifest['missing']}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
