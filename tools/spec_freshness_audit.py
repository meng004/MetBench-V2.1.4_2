#!/usr/bin/env python3
"""Phase 3 of the CI Cat B hardening plan: scan spec docs for "top-1 candidate" /
"next gap-fill" / "recommended MR" claims and check whether the claimed MR id
is actually present in the current catalog.

Output: a JSON list of stale claims (claimed MR id + spec file + line + spec age in days).
Exit code 0 always (informational; workflow decides whether to open issues).

Usage:
    python3 tools/spec_freshness_audit.py [--age-days N] [--repo-root .]
                                          [--check {stale,orphan,all}]
                                          [--plan-index PATH]

The workflow .github/workflows/spec-freshness-monitor.yml consumes the JSON
output to create / update GitHub issues with the label 'governance:stale-spec'
(stale-claim findings) or 'governance:orphan-spec' (orphan-spec findings).

P2 extension (v2 charter §3 module D + §6 P2): in addition to stale-claim
detection, this script also audits "orphan specs" — spec docs that exist
under docs/superpowers/specs/ but are not referenced by the active-plan-index
(neither by basename nor by repo-relative path). See
docs/superpowers/plans/2026-05-28-p2-spec-freshness-orphan-spec-plan.md.
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import re
import subprocess
import sys
import time

# Patterns the audit recognizes as MR-id claims. Each pattern's first
# capture group is the claimed MR id. Patterns are intentionally narrow —
# they match the explicit wording the post-merge-review doc lessons-learned
# §5 codified, not free-form prose.
CLAIM_PATTERNS = [
    re.compile(r"top-1\s+(?:candidate|recommendation)[^`\n]*[`']([\w-]+)[`']", re.IGNORECASE),
    re.compile(r"next\s+gap-fill[^`\n]*[`']([\w-]+)[`']", re.IGNORECASE),
    re.compile(r"recommended[^`\n]*MR[^`\n]*[`']([\w-]+)[`']", re.IGNORECASE),
]

# Spec docs that explicitly retract a recommendation. Lines containing these
# tokens NEAR a claim suppress the stale finding — they document the divergence
# per CLAUDE.md §12.4 R3.
RETRACTION_TOKENS = ("REJECTED", "REPLACED", "supersed", "no longer", "已被替代")


def find_claims(spec_path: pathlib.Path) -> list[tuple[int, str, str]]:
    """Return (line_number, claimed_mr_id, line_text) for each MR-id claim
    in the spec file. Lines within 3 lines of a retraction token are skipped."""
    text = spec_path.read_text(encoding="utf-8")
    lines = text.splitlines()
    out: list[tuple[int, str, str]] = []
    for i, line in enumerate(lines, start=1):
        # Window check: any retraction token within +/- 3 lines?
        window = lines[max(0, i - 4): min(len(lines), i + 3)]
        retracted = any(any(tok in w for tok in RETRACTION_TOKENS) for w in window)
        if retracted:
            continue
        for pattern in CLAIM_PATTERNS:
            for match in pattern.finditer(line):
                mr_id = match.group(1)
                # Skip stop-words that aren't actually MR ids
                if mr_id.lower() in ("x", "y", "n", "name", "id"):
                    continue
                out.append((i, mr_id, line.strip()))
    return out


def collect_known_mr_ids(repo_root: pathlib.Path) -> set[str]:
    """Return the set of MR ids known to exist on `main`. Sources:
    - SUT/*/catalog.json `mr_id` JSON fields
    - MetBench_BLL.Core/SystemMT/Launcher/LegacyCatalogFactory.cs `Id: "<mr-id>"` ctor args"""
    ids: set[str] = set()

    # JSON catalog files
    for catalog_file in repo_root.glob("SUT/*/catalog.json"):
        try:
            txt = catalog_file.read_text(encoding="utf-8")
        except OSError:
            continue
        for match in re.finditer(r'"mr_id"\s*:\s*"([\w-]+)"', txt):
            ids.add(match.group(1))

    # Hardcoded factory
    factory = repo_root / "MetBench_BLL.Core" / "SystemMT" / "Launcher" / "LegacyCatalogFactory.cs"
    if factory.is_file():
        for match in re.finditer(r'Id:\s*"([\w-]+)"', factory.read_text(encoding="utf-8")):
            ids.add(match.group(1))

    return ids


def spec_age_days(spec_path: pathlib.Path, repo_root: pathlib.Path) -> int:
    """Days since the spec file's last commit on `main`. Falls back to filesystem
    mtime if git is unavailable."""
    try:
        result = subprocess.run(
            ["git", "-C", str(repo_root), "log", "-1", "--format=%ct", "--", str(spec_path)],
            capture_output=True, text=True, check=True, timeout=10,
        )
        ts = result.stdout.strip()
        if ts:
            return int((time.time() - int(ts)) / 86400)
    except (subprocess.CalledProcessError, subprocess.TimeoutExpired, FileNotFoundError, ValueError):
        pass
    try:
        return int((time.time() - spec_path.stat().st_mtime) / 86400)
    except OSError:
        return 0


def load_orphan_allowlist(allowlist_path: pathlib.Path) -> set[str]:
    """Load the orphan-spec auditor allowlist (P2 extension).

    Lines starting with '#' and blank lines are ignored. Each remaining line is
    a single entry — either a basename (e.g. `2026-05-07-example.md`) or a
    repo-relative path (e.g. `docs/superpowers/specs/2026-05-07-example.md`).
    The return set contains both raw entries and, for path-form entries, the
    derived basename, so callers can match by either form.

    Missing file → empty set (fail-open for allowlist; the orphan auditor
    itself is gated on `plan_index_path.is_file()` so a missing allowlist does
    not mask a missing plan-index).
    """
    out: set[str] = set()
    if not allowlist_path.is_file():
        return out
    for raw in allowlist_path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        out.add(line)
        # If the entry looks like a path, also register its basename so
        # callers can match either form.
        if "/" in line:
            out.add(line.rsplit("/", 1)[-1])
    return out


def audit_orphan_specs(
    repo_root: pathlib.Path,
    plan_index_path: pathlib.Path,
    allowlist_path: pathlib.Path | None = None,
) -> list[dict]:
    """Audit spec docs under docs/superpowers/specs/ that are NOT referenced by
    the active-plan-index at `plan_index_path`.

    A spec is referenced if either its basename (e.g. `2026-05-07-foo.md`) or
    its repo-relative path (e.g. `docs/superpowers/specs/2026-05-07-foo.md`)
    appears as a literal substring in the plan-index file. Markdown inline-link
    forms like `[label](../specs/2026-05-07-foo.md)` therefore match by
    basename.

    Entries in the allowlist file (default
    `.github/governance/orphan-spec-allowlist.txt`) are explicitly excluded
    from orphan findings.

    Returns a list of dicts in the shape:
        {"spec_file": "...", "spec_age_days": N,
         "reason": "spec not referenced by active-plan-index.md (by basename or path)"}
    """
    spec_dir = repo_root / "docs" / "superpowers" / "specs"
    if not spec_dir.is_dir():
        # Fail-closed: orphan audit cannot run without a spec dir.
        print(
            f"spec_freshness_audit: spec dir not found: {spec_dir}",
            file=sys.stderr,
        )
        sys.exit(2)
    if not plan_index_path.is_file():
        # Fail-closed: orphan audit cannot run without the plan-index.
        print(
            f"spec_freshness_audit: plan-index not found: {plan_index_path}",
            file=sys.stderr,
        )
        sys.exit(2)

    plan_index_text = plan_index_path.read_text(encoding="utf-8")
    allowlist: set[str] = set()
    if allowlist_path is not None:
        allowlist = load_orphan_allowlist(allowlist_path)

    orphans: list[dict] = []
    for spec_file in sorted(spec_dir.glob("*.md")):
        rel_path = spec_file.relative_to(repo_root).as_posix()
        basename = spec_file.name
        if basename in allowlist or rel_path in allowlist:
            continue
        # Reference check: either basename or rel-path appears as a literal
        # substring in the plan-index. Substring match (not whole-line) is
        # intentional so markdown inline links and table rows both count.
        if basename in plan_index_text or rel_path in plan_index_text:
            continue
        orphans.append({
            "spec_file": rel_path,
            "spec_age_days": spec_age_days(spec_file, repo_root),
            "reason": "spec not referenced by active-plan-index.md (by basename or path)",
        })
    return orphans


def audit_stale_claims(repo_root: pathlib.Path, age_days: int) -> list[dict]:
    """Audit spec docs for MR-id claims absent from the current catalog.

    Extracted from the legacy `main()` so `--check stale` produces byte-identical
    output to the pre-P2 default invocation.
    """
    spec_dir = repo_root / "docs" / "superpowers" / "specs"
    if not spec_dir.is_dir():
        return []

    known_ids = collect_known_mr_ids(repo_root)
    stale: list[dict] = []

    for spec_file in sorted(spec_dir.glob("*.md")):
        claims = find_claims(spec_file)
        if not claims:
            continue
        age = spec_age_days(spec_file, repo_root)
        if age < age_days:
            continue
        for line_no, mr_id, line_text in claims:
            if mr_id in known_ids:
                continue
            stale.append({
                "spec_file": str(spec_file.relative_to(repo_root)),
                "spec_age_days": age,
                "line_number": line_no,
                "claimed_mr_id": mr_id,
                "line_text": line_text[:200],  # truncate for issue body sanity
            })
    return stale


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--age-days", type=int, default=14,
                        help="Minimum spec age (in days) before a stale claim becomes report-worthy")
    parser.add_argument("--repo-root", default=".",
                        help="Repo root directory; default is current working directory")
    parser.add_argument("--check", choices=("stale", "orphan", "all"), default="stale",
                        help="Which audit to run: stale-claim, orphan-spec, or both. "
                             "Default 'stale' preserves pre-P2 behavior byte-for-byte.")
    parser.add_argument("--plan-index",
                        default="docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md",
                        help="Path to active-plan-index used by --check orphan / all "
                             "(repo-relative or absolute)")
    parser.add_argument("--orphan-allowlist",
                        default=".github/governance/orphan-spec-allowlist.txt",
                        help="Path to orphan-spec allowlist file (repo-relative or absolute)")
    args = parser.parse_args()

    repo_root = pathlib.Path(args.repo_root).resolve()
    spec_dir = repo_root / "docs" / "superpowers" / "specs"

    # Stale-only path keeps the legacy fail-open behavior (missing spec_dir →
    # empty list, exit 0) so --check stale stays byte-equivalent to pre-P2.
    if args.check == "stale":
        if not spec_dir.is_dir():
            print(json.dumps([]))
            return 0
        stale = audit_stale_claims(repo_root, args.age_days)
        print(json.dumps(stale, indent=2))
        return 0

    # orphan / all path: fail-closed on missing inputs (handled inside
    # audit_orphan_specs).
    plan_index_arg = pathlib.Path(args.plan_index)
    if not plan_index_arg.is_absolute():
        plan_index_arg = repo_root / plan_index_arg
    allowlist_arg = pathlib.Path(args.orphan_allowlist)
    if not allowlist_arg.is_absolute():
        allowlist_arg = repo_root / allowlist_arg

    if args.check == "orphan":
        orphans = audit_orphan_specs(repo_root, plan_index_arg, allowlist_arg)
        print(json.dumps(orphans, indent=2))
        return 0

    # all: emit a dict with both lists so consumers can branch.
    if not spec_dir.is_dir():
        print(
            f"spec_freshness_audit: spec dir not found: {spec_dir}",
            file=sys.stderr,
        )
        sys.exit(2)
    stale = audit_stale_claims(repo_root, args.age_days)
    orphans = audit_orphan_specs(repo_root, plan_index_arg, allowlist_arg)
    print(json.dumps({"stale": stale, "orphan": orphans}, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
