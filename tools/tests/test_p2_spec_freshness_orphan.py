"""Python TDD: P2 orphan-spec auditor verification.

Covers `tools/spec_freshness_audit.py` orphan-spec audit extension landed in
v2 charter P2 (see docs/superpowers/plans/2026-05-28-p2-spec-freshness-orphan-spec-plan.md
and docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md §3 module D).

跑法：
    python3 tools/tests/test_p2_spec_freshness_orphan.py

或与 pytest：
    pytest tools/tests/test_p2_spec_freshness_orphan.py -v

不依赖 pytest 也能跑（手工 main 调度），与 tools/tests/test_p5_sync_tools.py
保持同形态。
"""

from __future__ import annotations

import subprocess
import sys
import tempfile
import textwrap
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(REPO_ROOT / "tools"))


def _scaffold_fake_repo(tmpdir: Path, *, plan_index_text: str,
                       spec_basenames: list[str],
                       allowlist_text: str | None = None) -> tuple[Path, Path, Path]:
    """Build a minimal fake repo layout under tmpdir.

    Returns (repo_root, plan_index_path, allowlist_path).
    """
    spec_dir = tmpdir / "docs" / "superpowers" / "specs"
    spec_dir.mkdir(parents=True)
    for bn in spec_basenames:
        (spec_dir / bn).write_text(f"# {bn}\n", encoding="utf-8")

    plan_dir = tmpdir / "docs" / "superpowers" / "plans"
    plan_dir.mkdir(parents=True)
    plan_index_path = plan_dir / "2026-05-25-metbench-active-plan-index.md"
    plan_index_path.write_text(plan_index_text, encoding="utf-8")

    allowlist_dir = tmpdir / ".github" / "governance"
    allowlist_dir.mkdir(parents=True)
    allowlist_path = allowlist_dir / "orphan-spec-allowlist.txt"
    if allowlist_text is not None:
        allowlist_path.write_text(allowlist_text, encoding="utf-8")
    # Else: leave the file absent so load_orphan_allowlist's fail-open
    # behavior is exercised when the caller wants that.
    return tmpdir, plan_index_path, allowlist_path


def test_spec_referenced_by_basename_is_not_orphan():
    """Spec whose basename appears in plan-index is NOT reported."""
    from spec_freshness_audit import audit_orphan_specs

    with tempfile.TemporaryDirectory() as tmp:
        repo, plan_index, allowlist = _scaffold_fake_repo(
            Path(tmp),
            plan_index_text="See [foo](../specs/2026-05-07-foo.md) for details.\n",
            spec_basenames=["2026-05-07-foo.md"],
            allowlist_text="",
        )
        orphans = audit_orphan_specs(repo, plan_index, allowlist)
        assert orphans == [], f"expected no orphans, got {orphans}"


def test_spec_not_referenced_and_not_allowlisted_is_orphan():
    """Spec absent from plan-index AND absent from allowlist IS reported."""
    from spec_freshness_audit import audit_orphan_specs

    with tempfile.TemporaryDirectory() as tmp:
        repo, plan_index, allowlist = _scaffold_fake_repo(
            Path(tmp),
            plan_index_text="No spec references here.\n",
            spec_basenames=["2026-05-07-orphan.md"],
            allowlist_text="",
        )
        orphans = audit_orphan_specs(repo, plan_index, allowlist)
        assert len(orphans) == 1, f"expected 1 orphan, got {orphans}"
        assert orphans[0]["spec_file"].endswith("2026-05-07-orphan.md")
        assert "not referenced" in orphans[0]["reason"]
        assert "spec_age_days" in orphans[0]


def test_spec_in_allowlist_is_not_orphan():
    """Allowlisted spec is NOT reported even when absent from plan-index."""
    from spec_freshness_audit import audit_orphan_specs

    with tempfile.TemporaryDirectory() as tmp:
        allowlist_body = textwrap.dedent("""\
            # comment line
            2026-05-07-allowlisted.md
            """)
        repo, plan_index, allowlist = _scaffold_fake_repo(
            Path(tmp),
            plan_index_text="No spec references here.\n",
            spec_basenames=["2026-05-07-allowlisted.md"],
            allowlist_text=allowlist_body,
        )
        orphans = audit_orphan_specs(repo, plan_index, allowlist)
        assert orphans == [], f"allowlist should suppress, got {orphans}"


def test_missing_spec_dir_fails_closed():
    """Missing spec dir → process exits non-zero (fail-closed)."""
    script = REPO_ROOT / "tools" / "spec_freshness_audit.py"
    with tempfile.TemporaryDirectory() as tmp:
        # Plan-index exists; spec dir does NOT.
        plan_dir = Path(tmp) / "docs" / "superpowers" / "plans"
        plan_dir.mkdir(parents=True)
        (plan_dir / "2026-05-25-metbench-active-plan-index.md").write_text("x\n", encoding="utf-8")
        result = subprocess.run(
            [sys.executable, str(script), "--check", "orphan", "--repo-root", tmp],
            capture_output=True, text=True,
        )
        assert result.returncode != 0, (
            f"expected non-zero exit for missing spec dir; got rc={result.returncode}, "
            f"stdout={result.stdout!r}, stderr={result.stderr!r}"
        )
        assert "spec dir not found" in result.stderr, result.stderr


def test_missing_plan_index_fails_closed():
    """Missing plan-index → process exits non-zero (fail-closed)."""
    script = REPO_ROOT / "tools" / "spec_freshness_audit.py"
    with tempfile.TemporaryDirectory() as tmp:
        # Spec dir exists; plan-index does NOT.
        spec_dir = Path(tmp) / "docs" / "superpowers" / "specs"
        spec_dir.mkdir(parents=True)
        (spec_dir / "2026-05-07-foo.md").write_text("# foo\n", encoding="utf-8")
        result = subprocess.run(
            [sys.executable, str(script), "--check", "orphan", "--repo-root", tmp],
            capture_output=True, text=True,
        )
        assert result.returncode != 0, (
            f"expected non-zero exit for missing plan-index; got rc={result.returncode}, "
            f"stdout={result.stdout!r}, stderr={result.stderr!r}"
        )
        assert "plan-index not found" in result.stderr, result.stderr


def test_inline_markdown_link_form_matches_basename():
    """Markdown inline-link `[label](../specs/foo.md)` matches via basename."""
    from spec_freshness_audit import audit_orphan_specs

    with tempfile.TemporaryDirectory() as tmp:
        plan_text = (
            "Some prose. See [the design](../specs/2026-05-07-link-form.md). "
            "Trailing prose.\n"
        )
        repo, plan_index, allowlist = _scaffold_fake_repo(
            Path(tmp),
            plan_index_text=plan_text,
            spec_basenames=["2026-05-07-link-form.md"],
            allowlist_text="",
        )
        orphans = audit_orphan_specs(repo, plan_index, allowlist)
        assert orphans == [], f"inline-link form should match by basename, got {orphans}"


# =============== Manual main ===============

def main() -> int:
    tests = [
        test_spec_referenced_by_basename_is_not_orphan,
        test_spec_not_referenced_and_not_allowlisted_is_orphan,
        test_spec_in_allowlist_is_not_orphan,
        test_missing_spec_dir_fails_closed,
        test_missing_plan_index_fails_closed,
        test_inline_markdown_link_form_matches_basename,
    ]
    failures = []
    for t in tests:
        try:
            t()
            print(f"  ok {t.__name__}")
        except Exception as e:
            print(f"  FAIL {t.__name__}: {e}")
            failures.append((t.__name__, e))

    if failures:
        print(f"\n{len(failures)} / {len(tests)} tests FAILED")
        return 1
    print(f"\n{len(tests)} / {len(tests)} tests PASSED")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
