#!/usr/bin/env python3
"""Chain-end `/code-review ultra` invocation helper (v2 charter §6 P5).

This script operationalizes the "auto-feed cumulative diff into /code-review
ultra" promise from the v2 governance charter §3 module E and CLAUDE.md §12.4
R2. It does **not** invoke the superpowers skill itself (that requires a
Claude Code session); instead it emits a ready-to-paste invocation string +
cumulative-diff metadata + a suggested review-doc path, so chain-end ritual
runners do not have to hand-count `origin/main~N` or hand-craft the ultra
command.

Anchors:
- v2 charter: docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md §3 E, §6 P5
- CLAUDE.md §12.2 module E row, §12.4 R2
- Plan: docs/superpowers/plans/2026-05-28-p5-ultra-chainend-automation-plan.md
- Checklist consumer: docs/superpowers/templates/chain-end-review-checklist.md Step 0 / Output tail

Read-only by construction: only `git rev-parse`, `git rev-list`, `git diff`,
`git log` — no checkout, no fetch, no write.
"""
from __future__ import annotations

import argparse
import datetime as _dt
import pathlib
import subprocess
import sys


def _git(repo_root: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    """Run a git subcommand against `repo_root`, capturing stdout / stderr."""
    return subprocess.run(
        ["git", "-C", str(repo_root), *args],
        capture_output=True,
        text=True,
    )


def _resolve_ref(repo_root: pathlib.Path, ref: str) -> str:
    """Resolve a git ref to a full commit SHA. Exit 2 on failure."""
    result = _git(repo_root, "rev-parse", "--verify", f"{ref}^{{commit}}")
    if result.returncode != 0:
        msg = (
            f"chain_end_ultra_invocation: rev-parse failed for ref {ref!r}: "
            f"{result.stderr.strip() or 'unknown error'}. "
            f"Ensure full git history is available locally (this script is "
            f"author-driven; CI shallow clones may not have the base commit)."
        )
        print(msg, file=sys.stderr)
        sys.exit(2)
    return result.stdout.strip()


def _short(sha: str) -> str:
    return sha[:7] if len(sha) >= 7 else sha


def _build_output(
    repo_root: pathlib.Path,
    *,
    base_input: str,
    head_input: str,
    base_sha: str,
    head_sha: str,
    chain_name: str | None,
    review_date: str,
    diff_preview_lines: int,
) -> tuple[str, list[str]]:
    """Construct the markdown-fenced output. Returns (stdout_text, warnings)."""
    warnings: list[str] = []

    # Effective chain name
    if chain_name is None or not chain_name.strip():
        effective_chain_name = f"chain-{_short(base_sha)}-to-{_short(head_sha)}"
        warnings.append(
            "warn: provide --chain-name explicitly; "
            f"falling back to inferred slug {effective_chain_name!r} (the worked-example "
            "conventions in the P5 plan §2.2 use human-readable slugs such as "
            "'t2-t3-chain' / 'ci-cat-b-hardening-chain')."
        )
    else:
        effective_chain_name = chain_name.strip()

    # Chain length
    rev_list = _git(repo_root, "rev-list", "--count", f"{base_sha}..{head_sha}")
    if rev_list.returncode != 0:
        msg = (
            f"chain_end_ultra_invocation: rev-list failed for "
            f"{base_sha}..{head_sha}: {rev_list.stderr.strip() or 'unknown error'}"
        )
        print(msg, file=sys.stderr)
        sys.exit(2)
    chain_length = int(rev_list.stdout.strip() or "0")
    if chain_length == 0:
        print(
            "chain_end_ultra_invocation: empty chain — "
            f"base ({base_sha}) and head ({head_sha}) resolve to commits with no "
            "commits between them. Refusing to emit an invocation against an "
            "empty diff.",
            file=sys.stderr,
        )
        sys.exit(2)

    # diff --stat
    diff_stat = _git(repo_root, "diff", "--stat", f"{base_sha}..{head_sha}")
    diff_stat_text = diff_stat.stdout.rstrip()

    # name-only
    name_only = _git(repo_root, "diff", "--name-only", f"{base_sha}..{head_sha}")
    name_only_text = name_only.stdout.rstrip()

    # diff preview (first N lines)
    full_diff = _git(repo_root, "diff", f"{base_sha}..{head_sha}")
    preview_lines = full_diff.stdout.splitlines()[:diff_preview_lines]
    preview_text = "\n".join(preview_lines)

    suggested_review_doc = (
        f"docs/superpowers/specs/{review_date}-{effective_chain_name}-post-merge-review.md"
    )

    out = []
    out.append("## Chain-End Ultra Invocation (auto-generated)")
    out.append("")
    out.append(f"Base: {base_sha}  (resolved from {base_input!r})")
    out.append(f"Head: {head_sha}  (resolved from {head_input!r})")
    out.append(
        f"Chain length: {chain_length} commits  "
        f"( = `git rev-list --count {base_sha}..{head_sha}` )"
    )
    out.append(f"Chain name: {effective_chain_name}")
    out.append(f"Suggested review doc: {suggested_review_doc}")
    out.append("")
    out.append("### Invocation")
    out.append("")
    out.append("```")
    out.append(f"/code-review ultra --base {base_sha} --head {head_sha}")
    out.append("```")
    out.append("")
    out.append(
        "(If the superpowers skill's current arg form requires stdin-piped diff "
        "instead of `--base/--head`, use this fallback:)"
    )
    out.append("")
    out.append("```")
    out.append(f"git diff {base_sha}..{head_sha} | /code-review ultra")
    out.append("```")
    out.append("")
    out.append("### git diff --stat")
    out.append("")
    out.append("```")
    out.append(diff_stat_text)
    out.append("```")
    out.append("")
    out.append("### Files touched (--name-only)")
    out.append("")
    out.append("```")
    out.append(name_only_text)
    out.append("```")
    out.append("")
    out.append(f"### Diff preview (first {diff_preview_lines} lines of unified diff)")
    out.append("")
    out.append("```diff")
    out.append(preview_text)
    out.append("```")

    return "\n".join(out) + "\n", warnings


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Emit a ready-to-paste /code-review ultra invocation + cumulative-diff "
            "metadata for a chain-end holistic review (v2 charter §6 P5)."
        )
    )
    parser.add_argument("--base", required=True,
                        help="Base git ref (SHA / branch / origin/main~N). Resolved via git rev-parse.")
    parser.add_argument("--head", required=True,
                        help="Head git ref (SHA / branch). Resolved via git rev-parse.")
    parser.add_argument("--chain-name", default=None,
                        help="Human-readable chain slug (e.g. 't2-t3-chain'). "
                             "When omitted, the script infers 'chain-<base-short>-to-<head-short>' "
                             "and prints a warning recommending an explicit slug.")
    parser.add_argument("--review-date", default=None,
                        help="Review-doc date in YYYY-MM-DD. Default: today (UTC).")
    parser.add_argument("--repo-root", default=None,
                        help="Repo root directory. Default: parent of this script's directory.")
    parser.add_argument("--diff-preview-lines", type=int, default=50,
                        help="Number of leading unified-diff lines to include as preview. Default 50.")
    args = parser.parse_args()

    if args.repo_root is None:
        repo_root = pathlib.Path(__file__).resolve().parent.parent
    else:
        repo_root = pathlib.Path(args.repo_root).resolve()

    if not (repo_root / ".git").exists() and not (repo_root / ".git").is_file():
        # Not strictly required (a worktree may have .git as a file), but warn
        # softly via a rev-parse probe.
        probe = _git(repo_root, "rev-parse", "--is-inside-work-tree")
        if probe.returncode != 0:
            print(
                f"chain_end_ultra_invocation: {repo_root} does not look like a git repo: "
                f"{probe.stderr.strip() or 'unknown error'}",
                file=sys.stderr,
            )
            sys.exit(2)

    base_sha = _resolve_ref(repo_root, args.base)
    head_sha = _resolve_ref(repo_root, args.head)

    if base_sha == head_sha:
        print(
            "chain_end_ultra_invocation: empty chain — "
            f"base and head both resolve to {base_sha}. "
            "Pass distinct refs (e.g. --base <chain-base>~1 --head <chain-head>).",
            file=sys.stderr,
        )
        sys.exit(2)

    review_date = args.review_date or _dt.date.today().isoformat()

    text, warnings = _build_output(
        repo_root,
        base_input=args.base,
        head_input=args.head,
        base_sha=base_sha,
        head_sha=head_sha,
        chain_name=args.chain_name,
        review_date=review_date,
        diff_preview_lines=args.diff_preview_lines,
    )

    sys.stdout.write(text)
    for w in warnings:
        print(w, file=sys.stderr)

    return 0


if __name__ == "__main__":
    sys.exit(main())
