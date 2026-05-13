"""migrate_real_bugs_to_v2: docs/experiments/bug-inventory.md → KnownBugs JSON.

提取 6 个 R-Case 行（R-Case-1 至 R-Case-6）→ v2 KnownBug 实体 JSON 包。
正则匹配格式：
  | R-Case-N | <source> | `<commit>` | <date> | `<file>` | <bug> | <mp> | <mr> | <state> |

CLI:
    python tools/migrate_real_bugs_to_v2.py --output metbench/catalog/migration/real-bugs.json
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parent.parent
INVENTORY_MD = REPO_ROOT / "docs" / "experiments" / "bug-inventory.md"

# 6 个 R-Case 内容（基于 docs/experiments/bug-inventory.md 当前内容）
_KNOWN_BUGS = [
    {
        "code": "R-Case-1",
        "title": "OpenMOC CPUSolver `_k_eff *=` accumulation",
        "related_application": "openmoc",
        "upstream_fix_commit": "28008901",
        "status": "fixed-upstream",
        "description": (
            "`_k_eff *= ...` accumulation in CPUSolver power-iteration loop. "
            "Upstream fix was preventive — bug only bites non-converged "
            "transients which the pincell SUT does not exercise."
        ),
    },
    {
        "code": "R-Case-2",
        "title": "OpenMC PR #3712 `add_temperature` chained-append → None",
        "related_application": "openmc",
        "upstream_fix_commit": "dfc80c70",
        "status": "fixed-upstream",
        "description": (
            "`add_temperature` chained-append returns None. Live in matrix via "
            "openmc-pincell-fuel-temperature-via-add-temperature scenario; "
            "cell status=error with marker `RuntimeError: OpenMC PR #3712 ... triggered`."
        ),
    },
    {
        "code": "R-Case-3",
        "title": "OpenMC PR #3708 distribcell `group_name=''.zfill(N)` collapse",
        "related_application": "openmc",
        "upstream_fix_commit": "10f2b753",
        "status": "fixed-upstream",
        "description": (
            "distribcell tally `group_name='' zfill(N)` collapse. m_inv (MR02-tally / "
            "MR03-tally) MR family applicable. Walkthrough only — bug path needs "
            "MGXS-from-tallies + 2×2 pin SUT extension."
        ),
    },
    {
        "code": "R-Case-4",
        "title": "OpenMOC CPUSolver power-iter basin @ moderator-σ_a factor=1.5",
        "related_application": "openmoc",
        "upstream_fix_commit": None,
        "status": "open",
        "description": (
            "MetBench self-discovery 2026-05-12. m_cmp via MR14 cross-program. "
            "Live confirmed: OpenMOC k=0.476 vs OpenMC k=0.968 (51% Δ). "
            "Power-iter narrow basin in CPUSolver — extended MT first finding."
        ),
    },
    {
        "code": "R-Case-5",
        "title": "OpenMC PR #3662 `borated_water(density=X)` drops temperature",
        "related_application": "openmc",
        "upstream_fix_commit": "ef22558f",
        "status": "fixed-upstream",
        "description": (
            "`borated_water(density=X)` silently drops `temperature` argument. "
            "Live in matrix via openmc-pincell-moderator-temperature-via-borated-water "
            "scenario; cell status=error with marker `RuntimeError: OpenMC PR #3662 ... triggered`."
        ),
    },
    {
        "code": "R-Case-6",
        "title": "OpenMOC CPUSolver power-iter basin @ fuel-temperature factor=1.25",
        "related_application": "openmoc",
        "upstream_fix_commit": None,
        "status": "open",
        "description": (
            "MetBench self-discovery 2026-05-12 via tools/mr_parameter_sweep.py. "
            "m_mono via MR-T parameter sweep — first classical-MT live finding. "
            "OpenMOC k=0.508 at single factor sliver while neighbours give 1.11."
        ),
    },
]


def migrate() -> dict[str, Any]:
    return {
        "source": "docs/experiments/bug-inventory.md",
        "total": len(_KNOWN_BUGS),
        "known_bugs": _KNOWN_BUGS,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", required=False)
    args = parser.parse_args()

    pkg = migrate()
    out = json.dumps(pkg, indent=2, ensure_ascii=False)
    if args.output:
        Path(args.output).write_text(out + "\n", encoding="utf-8")
        print(f"wrote {args.output}: {len(pkg['known_bugs'])} known bugs")
    else:
        print(out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
