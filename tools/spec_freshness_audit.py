#!/usr/bin/env python3
"""Phase 3 of the CI Cat B hardening plan: scan spec docs for "top-1 candidate" /
"next gap-fill" / "recommended MR" claims and check whether the claimed MR id
is actually present in the current catalog.

Output: a JSON list of stale claims (claimed MR id + spec file + line + spec age in days).
Exit code 0 always (informational; workflow decides whether to open issues).

Usage:
    python3 tools/spec_freshness_audit.py [--age-days N] [--repo-root .]

The workflow .github/workflows/spec-freshness-monitor.yml consumes the JSON
output to create / update GitHub issues with the label 'governance:stale-spec'.
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


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--age-days", type=int, default=14,
                        help="Minimum spec age (in days) before a stale claim becomes report-worthy")
    parser.add_argument("--repo-root", default=".",
                        help="Repo root directory; default is current working directory")
    args = parser.parse_args()

    repo_root = pathlib.Path(args.repo_root).resolve()
    spec_dir = repo_root / "docs" / "superpowers" / "specs"
    if not spec_dir.is_dir():
        print(json.dumps([]))
        return 0

    known_ids = collect_known_mr_ids(repo_root)
    stale: list[dict] = []

    for spec_file in sorted(spec_dir.glob("*.md")):
        claims = find_claims(spec_file)
        if not claims:
            continue
        age = spec_age_days(spec_file, repo_root)
        if age < args.age_days:
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

    print(json.dumps(stale, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
