"""Parse cross-program-report.md DISAGREE rows into a JSON manifest.

T5 PR-2 step 1: lift the existing static markdown report
`docs/experiments/cross-program-report.md` into a structured JSON file
that the .NET seed tool (`tools/SeedCrossProgramAnomalies`) can ingest
into `SystemMT.Litedb`'s Anomaly collection.

The report has a summary table whose last column is `verdict ∈
{agree, **DISAGREE**}`. For each DISAGREE row we capture the
transform name, both side k_eff values, the OpenMC sigma, the |Δk|,
the budget, and a descriptive sentence pulled from the per-transform
detail section beneath. The result is emitted as a JSON array.

Usage::

    python tools/import_cross_program_anomalies.py \\
        --input docs/experiments/cross-program-report.md \\
        --output docs/experiments/cross-program-anomalies-2026-05-28.json
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path


SUMMARY_TABLE_HEADER = "| Transform | k(OpenMOC) | k(OpenMC) | σ(OpenMC) | |Δk| | budget | verdict |"


def parse_disagree_rows(markdown_text: str) -> list[dict]:
    """Extract DISAGREE rows from the summary table.

    Returns a list of dicts in declaration order.
    """
    lines = markdown_text.splitlines()
    header_idx = None
    for i, line in enumerate(lines):
        if line.strip() == SUMMARY_TABLE_HEADER:
            header_idx = i
            break
    if header_idx is None:
        raise ValueError(
            f"Could not locate summary table header. Expected exact line:\n  {SUMMARY_TABLE_HEADER}"
        )
    rows = []
    # Skip header + separator row
    for j in range(header_idx + 2, len(lines)):
        line = lines[j].strip()
        if not line.startswith("|"):
            break
        cells = [c.strip() for c in line.strip("|").split("|")]
        if len(cells) != 7:
            continue
        verdict_raw = cells[6]
        verdict = re.sub(r"\*+", "", verdict_raw).strip()
        if verdict.lower() != "disagree":
            continue
        rows.append(
            {
                "transform": cells[0],
                "k_openmoc": float(cells[1]),
                "k_openmc": float(cells[2]),
                "sigma_openmc": float(cells[3]),
                "delta_k": float(cells[4]),
                "budget": float(cells[5]),
                "verdict": "DISAGREE",
            }
        )
    return rows


def attach_scenario_details(markdown_text: str, rows: list[dict]) -> list[dict]:
    """For each row, locate the per-transform detail section and lift
    the OpenMOC + OpenMC scenario ids and the percent-of-OpenMC."""
    for row in rows:
        transform = row["transform"]
        # Section header looks like `### ScaleModeratorSigmaA`
        section_pattern = rf"^### {re.escape(transform)}\s*$"
        section_match = re.search(section_pattern, markdown_text, re.MULTILINE)
        if not section_match:
            continue
        # Take the next ~10 lines under the header
        after = markdown_text[section_match.end() :].splitlines()
        details = "\n".join(after[:12])
        openmoc_scenario_match = re.search(r"OpenMOC scenario:\s*`([^`]+)`", details)
        openmc_scenario_match = re.search(r"OpenMC scenario:\s*`([^`]+)`", details)
        percent_match = re.search(r"\|Δk\|\s*=\s*[\d.]+\s*\(([\d.]+)%", details)
        if openmoc_scenario_match:
            row["openmoc_scenario"] = openmoc_scenario_match.group(1)
        if openmc_scenario_match:
            row["openmc_scenario"] = openmc_scenario_match.group(1)
        if percent_match:
            row["percent_of_openmc"] = float(percent_match.group(1))
    return rows


def classify_severity(row: dict) -> str:
    """Map relative |Δk| size to Anomaly severity.

    Mirrors AnomalyClassifier conventions:
    - critical: |Δk| > 5% of OpenMC (massive disagreement)
    - major:    1% < |Δk| <= 5%
    - minor:    budget < |Δk| <= 1%
    """
    percent = row.get("percent_of_openmc")
    if percent is None:
        # Fallback: compare delta_k to k_openmc
        percent = (row["delta_k"] / row["k_openmc"]) * 100.0 if row["k_openmc"] else 0.0
    if percent > 5.0:
        return "critical"
    if percent > 1.0:
        return "major"
    return "minor"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument(
        "--input",
        type=Path,
        default=Path("docs/experiments/cross-program-report.md"),
        help="Path to cross-program-report.md",
    )
    parser.add_argument(
        "--output",
        type=Path,
        required=True,
        help="Output JSON path",
    )
    args = parser.parse_args()

    if not args.input.exists():
        print(f"ERROR: input not found: {args.input}", file=sys.stderr)
        return 2

    text = args.input.read_text(encoding="utf-8")
    rows = parse_disagree_rows(text)
    rows = attach_scenario_details(text, rows)
    for row in rows:
        row["severity"] = classify_severity(row)
        row["category"] = "cross-program-disagreement"
        row["status_after_seed"] = "investigating"

    args.output.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "source": str(args.input),
        "extracted_at": "static-report",
        "anomalies": rows,
    }
    args.output.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {len(rows)} disagree row(s) to {args.output}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
