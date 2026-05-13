"""Python TDD: P8.3 build_paper_package.py 行为验证。

跑法:
    python3 tools/tests/test_p8_paper_package.py
"""

from __future__ import annotations

import sys
import tarfile
import tempfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(REPO_ROOT / "tools"))


def test_package_includes_catalog_dir():
    from build_paper_package import build_package
    with tempfile.TemporaryDirectory() as tmp:
        out = Path(tmp) / "pkg.tar.gz"
        manifest = build_package(out)
        assert out.exists(), "tarball not created"
        assert manifest["files"], "manifest 'files' is empty"
        with tarfile.open(out, "r:gz") as tar:
            names = tar.getnames()
        assert any("metbench/catalog" in n for n in names), \
            "catalog dir missing from tarball"


def test_package_includes_diagrams():
    from build_paper_package import build_package
    with tempfile.TemporaryDirectory() as tmp:
        out = Path(tmp) / "pkg.tar.gz"
        build_package(out)
        with tarfile.open(out, "r:gz") as tar:
            names = tar.getnames()
        assert any("docs/design/diagrams" in n for n in names), \
            "diagrams dir missing"


def test_package_includes_markdown_docs():
    from build_paper_package import build_package
    with tempfile.TemporaryDirectory() as tmp:
        out = Path(tmp) / "pkg.tar.gz"
        build_package(out)
        with tarfile.open(out, "r:gz") as tar:
            names = tar.getnames()
        for md in ("README.md", "CLAUDE.md", "AGENTS.md"):
            assert any(n.endswith(md) for n in names), f"missing {md}"


def test_extra_paths_appended():
    from build_paper_package import build_package
    extra_file = REPO_ROOT / "tools" / "noether_candidates.py"
    with tempfile.TemporaryDirectory() as tmp:
        out = Path(tmp) / "pkg.tar.gz"
        manifest = build_package(out, [Path("tools/noether_filter_calibration.py")])
        # Both default included (noether_candidates.py) and extra (noether_filter_calibration.py)
        files = [str(f["path"]) for f in manifest["files"]]
        assert any("noether_filter_calibration" in f for f in files)


def test_missing_paths_reported():
    from build_paper_package import build_package
    with tempfile.TemporaryDirectory() as tmp:
        out = Path(tmp) / "pkg.tar.gz"
        manifest = build_package(out, [Path("nonexistent/abc.def")])
        assert manifest["missing"], "expected 'missing' to be non-empty"
        assert "nonexistent/abc.def" in manifest["missing"]


def main() -> int:
    print("P8 — build_paper_package")
    tests = [
        test_package_includes_catalog_dir,
        test_package_includes_diagrams,
        test_package_includes_markdown_docs,
        test_extra_paths_appended,
        test_missing_paths_reported,
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
