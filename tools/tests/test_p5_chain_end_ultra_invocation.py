"""Python TDD: P5 chain-end `/code-review ultra` invocation helper.

Covers `tools/chain_end_ultra_invocation.py` landed in v2 charter P5 (see
docs/superpowers/plans/2026-05-28-p5-ultra-chainend-automation-plan.md and
docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md §3 module E
+ §6 P5).

跑法：
    python3 tools/tests/test_p5_chain_end_ultra_invocation.py

或与 pytest：
    pytest tools/tests/test_p5_chain_end_ultra_invocation.py -v

不依赖 pytest 也能跑（手工 main 调度），与
`tools/tests/test_p2_spec_freshness_orphan.py` 保持同形态。
"""

from __future__ import annotations

import os
import subprocess
import sys
import tempfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
SCRIPT = REPO_ROOT / "tools" / "chain_end_ultra_invocation.py"


def _git(cwd: Path, *args: str, env: dict | None = None) -> subprocess.CompletedProcess:
    base_env = os.environ.copy()
    # Force deterministic identity so commits don't depend on the host user.
    base_env.setdefault("GIT_AUTHOR_NAME", "test")
    base_env.setdefault("GIT_AUTHOR_EMAIL", "test@example.com")
    base_env.setdefault("GIT_COMMITTER_NAME", "test")
    base_env.setdefault("GIT_COMMITTER_EMAIL", "test@example.com")
    if env:
        base_env.update(env)
    return subprocess.run(
        ["git", "-C", str(cwd), *args],
        capture_output=True, text=True, check=True, env=base_env,
    )


def _init_repo_with_two_commits(tmp: Path) -> None:
    """Create a temp git repo with two commits so HEAD~1..HEAD has 1 commit."""
    _git(tmp, "init", "-q", "-b", "main")
    _git(tmp, "config", "user.email", "test@example.com")
    _git(tmp, "config", "user.name", "test")
    # Defeat any host-global commit signing so the temp repo is hermetic
    # (some environments enforce gpg.format=ssh + commit.gpgsign=true globally,
    # which would otherwise reach into the temp repo and fail every commit).
    _git(tmp, "config", "commit.gpgsign", "false")
    _git(tmp, "config", "tag.gpgsign", "false")
    (tmp / "a.txt").write_text("first\n", encoding="utf-8")
    _git(tmp, "add", "a.txt")
    _git(tmp, "commit", "-q", "-m", "first commit")
    (tmp / "b.txt").write_text("second\n", encoding="utf-8")
    _git(tmp, "add", "b.txt")
    _git(tmp, "commit", "-q", "-m", "second commit")


def _run_script(repo_root: Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(
        [sys.executable, str(SCRIPT), "--repo-root", str(repo_root), *args],
        capture_output=True, text=True,
    )


def test_happy_path_two_commit_chain():
    """Two-commit repo + --base HEAD~1 --head HEAD → exit 0 + complete output."""
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp)
        _init_repo_with_two_commits(repo)
        result = _run_script(
            repo,
            "--base", "HEAD~1",
            "--head", "HEAD",
            "--chain-name", "demo-chain",
            "--review-date", "2026-05-28",
        )
        assert result.returncode == 0, (
            f"expected exit 0; got rc={result.returncode}, stderr={result.stderr!r}"
        )
        out = result.stdout
        assert "Chain length: 1 commits" in out, f"missing chain length: {out!r}"
        assert "/code-review ultra --base " in out, f"missing ultra invocation: {out!r}"
        assert "### git diff --stat" in out, f"missing diff --stat section: {out!r}"
        assert "demo-chain" in out, f"missing chain name: {out!r}"
        assert (
            "docs/superpowers/specs/2026-05-28-demo-chain-post-merge-review.md"
            in out
        ), f"missing suggested review doc path: {out!r}"


def test_base_ref_invalid():
    """Unknown base ref → exit 2 + stderr 'rev-parse failed'."""
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp)
        _init_repo_with_two_commits(repo)
        result = _run_script(
            repo,
            "--base", "bad-sha-xxx",
            "--head", "HEAD",
            "--chain-name", "demo-chain",
        )
        assert result.returncode == 2, (
            f"expected exit 2; got rc={result.returncode}, stderr={result.stderr!r}"
        )
        assert "rev-parse failed" in result.stderr, (
            f"stderr should mention rev-parse failure; got {result.stderr!r}"
        )


def test_empty_chain_same_sha():
    """--base HEAD --head HEAD → exit 2 + stderr 'empty chain'."""
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp)
        _init_repo_with_two_commits(repo)
        result = _run_script(
            repo,
            "--base", "HEAD",
            "--head", "HEAD",
            "--chain-name", "demo-chain",
        )
        assert result.returncode == 2, (
            f"expected exit 2; got rc={result.returncode}, stderr={result.stderr!r}"
        )
        assert "empty chain" in result.stderr, (
            f"stderr should mention empty chain; got {result.stderr!r}"
        )


def test_chain_name_inference():
    """Without --chain-name → stdout has 'chain-<short>-to-<short>' + stderr warn."""
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp)
        _init_repo_with_two_commits(repo)
        # Resolve HEAD / HEAD~1 to short SHAs for assertion.
        head_full = _git(repo, "rev-parse", "HEAD").stdout.strip()
        base_full = _git(repo, "rev-parse", "HEAD~1").stdout.strip()
        head_short = head_full[:7]
        base_short = base_full[:7]

        result = _run_script(
            repo,
            "--base", "HEAD~1",
            "--head", "HEAD",
            "--review-date", "2026-05-28",
        )
        assert result.returncode == 0, (
            f"expected exit 0; got rc={result.returncode}, stderr={result.stderr!r}"
        )
        inferred_slug = f"chain-{base_short}-to-{head_short}"
        assert inferred_slug in result.stdout, (
            f"expected inferred slug {inferred_slug!r} in stdout; got {result.stdout!r}"
        )
        assert "provide --chain-name" in result.stderr, (
            f"expected warning on stderr; got {result.stderr!r}"
        )


# =============== Manual main ===============

def main() -> int:
    tests = [
        test_happy_path_two_commit_chain,
        test_base_ref_invalid,
        test_empty_chain_same_sha,
        test_chain_name_inference,
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
