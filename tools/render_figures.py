"""Render the Phase-3 visualization figures into docs/experiments/figures/.

Reads existing _data/* JSON and the rendered Markdown / CSV tables; no
solver re-runs. Produces three figures per the visualization plan in
`docs/superpowers/plans/2026-05-13-stage5-phase3-visualization.md`:

* fig1: per-MR detection rate (grouped bar) — MetaPattern grouping,
  Wilson 95% CI error bars, Phase 1/2/3 color bands.
* fig2: parameter-sweep trajectory — 6 panels, OpenMOC + OpenMC, with
  Case 4 / Case 6 OpenMOC basin annotations.
* fig3: matched-pair Cohen's κ heatmap — diverging colormap.

Dependency: matplotlib only. CLI:

    python3 tools/render_figures.py --fig 1|2|3|all
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import re
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches

REPO_ROOT = Path(__file__).resolve().parent.parent
FIG_DIR = REPO_ROOT / "docs" / "experiments" / "figures"
FIG_DIR.mkdir(parents=True, exist_ok=True)

DATA_DIR = REPO_ROOT / "docs" / "experiments" / "_data"
MATRIX_MD = REPO_ROOT / "docs" / "experiments" / "mutation-detection-matrix.md"
SWEEP_JSON = DATA_DIR / "mr-parameter-sweep.json"


def wilson_ci(k: int, n: int, z: float = 1.96) -> tuple[float, float]:
    if n == 0:
        return (0.0, 1.0)
    p = k / n
    denom = 1 + z * z / n
    center = (p + z * z / (2 * n)) / denom
    margin = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / denom
    return (max(0.0, center - margin), min(1.0, center + margin))


def parse_per_mr_table(md_text: str) -> list[dict]:
    """Pull the 'Per-MR detection rate' table out of mutation-detection-matrix.md."""
    pat = re.compile(r"^\|\s*([\w\-]+pincell[\w\-]+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|", re.M)
    rows = []
    for m in pat.finditer(md_text):
        scenario, n, det, miss, err = m.group(1), int(m.group(2)), int(m.group(3)), int(m.group(4)), int(m.group(5))
        rows.append({"scenario": scenario, "n": n, "detected": det, "missed": miss, "errors": err})
    return rows


# Per-scenario metadata: noether_id, meta_pattern, phase. Tied to SCENARIOS list.
SCENARIO_META = {
    "openmoc-pincell-nu-sigma-f":               ("Phase-1", "m_mono",  "openmoc"),
    "openmc-pincell-nu-sigma-f":                ("Phase-1", "m_mono",  "openmc"),
    "openmoc-pincell-sigma-a":                  ("Phase-1", "m_mono",  "openmoc"),
    "openmc-pincell-sigma-a":                   ("Phase-1", "m_mono",  "openmc"),
    "openmoc-pincell-group-permute":            ("Phase-2", "m_inv",   "openmoc"),
    "openmc-pincell-group-permute":             ("Phase-2", "m_inv",   "openmc"),
    "openmoc-pincell-fuel-sigma-t":             ("Phase-2", "m_mono",  "openmoc"),
    "openmc-pincell-fuel-sigma-t":              ("Phase-2", "m_mono",  "openmc"),
    "openmoc-pincell-moderator-sigma-a":        ("Phase-2", "m_mono",  "openmoc"),
    "openmc-pincell-moderator-sigma-a":         ("Phase-2", "m_mono",  "openmc"),
    "openmoc-pincell-fuel-sigma-s":             ("Phase-2", "m_mono",  "openmoc"),
    "openmc-pincell-fuel-sigma-s":              ("Phase-2", "m_mono",  "openmc"),
    "openmoc-pincell-fuel-radius":              ("Phase-2", "m_mono",  "openmoc"),
    "openmc-pincell-fuel-radius":               ("Phase-2", "m_mono",  "openmc"),
    "openmc-pincell-particles-refine":          ("Phase-2", "m_conv",  "openmc"),
    "openmoc-pincell-rotate-90":                ("Phase-2", "m_inv",   "openmoc"),
    "openmc-pincell-rotate-90":                 ("Phase-2", "m_inv",   "openmc"),
    "openmoc-pincell-mirror-x":                 ("Phase-2", "m_inv",   "openmoc"),
    "openmc-pincell-mirror-x":                  ("Phase-2", "m_inv",   "openmc"),
    "openmoc-pincell-mirror-y":                 ("Phase-2", "m_inv",   "openmoc"),
    "openmc-pincell-mirror-y":                  ("Phase-2", "m_inv",   "openmc"),
    "openmoc-pincell-mirror-x-tally":           ("Phase-3", "m_inv",   "openmoc"),
    "openmc-pincell-mirror-x-tally":            ("Phase-3", "m_inv",   "openmc"),
    "openmoc-pincell-mirror-y-tally":           ("Phase-3", "m_inv",   "openmoc"),
    "openmc-pincell-mirror-y-tally":            ("Phase-3", "m_inv",   "openmc"),
    "openmoc-pincell-fuel-temperature":         ("Phase-3", "m_mono",  "openmoc"),
    "openmc-pincell-fuel-temperature":          ("Phase-3", "m_mono",  "openmc"),
    "openmc-pincell-fuel-temperature-via-add-temperature": ("Phase-3", "m_mono", "openmc"),
}

PHASE_COLORS = {
    "Phase-1": "#dddddd",
    "Phase-2": "#bbddff",
    "Phase-3": "#ffddbb",
}

MP_ORDER = ["m_inv", "m_mono", "m_conv"]  # m_cmp is reported separately


def fig1_detection_rate() -> None:
    rows = parse_per_mr_table(MATRIX_MD.read_text())
    enriched = []
    for r in rows:
        meta = SCENARIO_META.get(r["scenario"])
        if meta is None:
            continue
        phase, mp, solver = meta
        lo, hi = wilson_ci(r["detected"], r["detected"] + r["missed"]) if (r["detected"] + r["missed"]) > 0 else (0.0, 0.0)
        rate = r["detected"] / (r["detected"] + r["missed"]) if (r["detected"] + r["missed"]) > 0 else 0.0
        enriched.append({
            "scenario": r["scenario"], "phase": phase, "mp": mp, "solver": solver,
            "n": r["detected"] + r["missed"], "rate": rate, "lo": lo, "hi": hi,
        })

    # Sort: MP_ORDER then phase then solver then scenario
    enriched.sort(key=lambda r: (MP_ORDER.index(r["mp"]) if r["mp"] in MP_ORDER else 99,
                                  r["phase"], r["solver"], r["scenario"]))

    fig, ax = plt.subplots(figsize=(15, 6))
    x_positions = []
    x_labels = []
    bar_x = 0
    mp_boundaries = []
    prev_mp = None
    for i, r in enumerate(enriched):
        if r["mp"] != prev_mp:
            if prev_mp is not None:
                mp_boundaries.append((prev_mp, bar_x - 0.5))
            prev_mp = r["mp"]
            mp_start = bar_x
        color = "#4477aa" if r["solver"] == "openmoc" else "#ee6655"
        # Hatch for OpenMC
        hatch = None if r["solver"] == "openmoc" else "///"
        err_lo = max(0.0, r["rate"] - r["lo"])
        err_hi = max(0.0, r["hi"] - r["rate"])
        ax.bar(bar_x, r["rate"] * 100, width=0.8, color=color, edgecolor="black",
               hatch=hatch, alpha=0.85,
               yerr=[[err_lo * 100], [err_hi * 100]], capsize=2,
               label="OpenMOC" if (r["solver"] == "openmoc" and bar_x == 0) else
                     ("OpenMC" if (r["solver"] == "openmc" and "openmc" not in [b for b in [bar_x]]) else None))
        x_positions.append(bar_x)
        # Shorten label
        short = r["scenario"].replace("openmoc-pincell-", "").replace("openmc-pincell-", "")
        x_labels.append(short)
        bar_x += 1
    mp_boundaries.append((prev_mp, bar_x - 0.5))

    # MP group labels
    last_x = -0.5
    for mp, end_x in mp_boundaries:
        ax.axvspan(last_x, end_x + 0.5, alpha=0.12,
                   color={"m_inv": "#88bb88", "m_mono": "#88aacc", "m_conv": "#ddaa66"}.get(mp, "#cccccc"))
        ax.text((last_x + end_x + 0.5) / 2, 102, mp, ha="center", fontsize=11, fontweight="bold")
        last_x = end_x + 0.5

    ax.set_xticks(x_positions)
    ax.set_xticklabels(x_labels, rotation=60, ha="right", fontsize=8)
    ax.set_ylabel("Detection rate (%)")
    ax.set_ylim(0, 108)
    ax.axhline(0, color="black", linewidth=0.5)
    ax.set_title("MetBench per-MR detection rate (Wilson 95% CI) — by MetaPattern, MetBench Phase-3")

    # Legend
    legend_handles = [
        mpatches.Patch(color="#4477aa", label="OpenMOC"),
        mpatches.Patch(facecolor="#ee6655", hatch="///", label="OpenMC"),
        mpatches.Patch(color="#88bb88", alpha=0.4, label="m_inv (symmetry)"),
        mpatches.Patch(color="#88aacc", alpha=0.4, label="m_mono (monotonicity)"),
        mpatches.Patch(color="#ddaa66", alpha=0.4, label="m_conv (limit)"),
    ]
    ax.legend(handles=legend_handles, loc="upper right", fontsize=8)

    # Star on MR-T scenarios marking Case 6 finding
    for i, r in enumerate(enriched):
        if r["scenario"] == "openmoc-pincell-fuel-temperature":
            ax.annotate("★ Case 6 found here\n(MR-T sweep)", xy=(i, r["rate"] * 100), xytext=(i, 75),
                        ha="center", fontsize=8, color="#aa0000",
                        arrowprops=dict(arrowstyle="->", color="#aa0000", lw=1))

    plt.tight_layout()
    out_png = FIG_DIR / "fig1_per_mr_detection_rate.png"
    out_svg = FIG_DIR / "fig1_per_mr_detection_rate.svg"
    plt.savefig(out_png, dpi=300)
    plt.savefig(out_svg)
    plt.close(fig)
    print(f"wrote {out_png.relative_to(REPO_ROOT)}")


def fig2_sweep_trajectory() -> None:
    data = json.loads(SWEEP_JSON.read_text())
    # Pair openmoc/openmc same transform
    transforms = ["ScaleFuelSigmaT", "ScaleFuelSigmaS", "ScaleModeratorSigmaA",
                  "ScaleFuelRadius", "RefineParticles", "RaiseFuelTemperature"]
    titles = {
        "ScaleFuelSigmaT": "MR05 ScaleFuelSigmaT (m_mono)",
        "ScaleFuelSigmaS": "MR06 ScaleFuelSigmaS (m_mono)",
        "ScaleModeratorSigmaA": "MR07 ScaleModeratorSigmaA (m_mono)",
        "ScaleFuelRadius": "MR08 ScaleFuelRadius (m_mono)",
        "RefineParticles": "MR12 RefineParticles (m_conv, OpenMC only)",
        "RaiseFuelTemperature": "MR-T RaiseFuelTemperature (m_mono)",
    }
    fig, axes = plt.subplots(2, 3, figsize=(15, 8))
    axes_flat = axes.flatten()

    def find_scenario(transform: str, solver: str) -> dict | None:
        for sc_id, s in data["scenarios"].items():
            if s.get("solver") == solver and "cells" in s and \
               transform.lower().replace("scale", "").replace("raise", "") in sc_id.replace("-", "").lower():
                return {"id": sc_id, **s}
        return None

    for ax, tr in zip(axes_flat, transforms):
        ax.set_title(titles[tr], fontsize=10)
        ax.set_xlabel("factor")
        ax.set_ylabel("k_followup")

        for solver, color, marker in [("openmoc", "#4477aa", "o"), ("openmc", "#ee6655", "s")]:
            s = find_scenario(tr, solver)
            if s is None or not s.get("cells"):
                continue
            factors = []
            ks = []
            sigmas = []
            for c in s["cells"]:
                if "error" in c:
                    continue
                factors.append(c["factor"])
                ks.append(c["k_followup"])
                sigmas.append(c.get("k_followup_std", 0.0))
            if not factors:
                continue
            ax.plot(factors, ks, marker=marker, label=solver.upper(), color=color, lw=1.5)
            if any(sg > 0 for sg in sigmas):
                ax.fill_between(factors,
                                 [k - 3 * sg for k, sg in zip(ks, sigmas)],
                                 [k + 3 * sg for k, sg in zip(ks, sigmas)],
                                 alpha=0.15, color=color)
            # Source baseline horizontal line
            ax.axhline(s["k_source"], color=color, linestyle=":", alpha=0.5, lw=0.8)

        ax.legend(fontsize=8, loc="best")
        ax.grid(True, alpha=0.3)

        # Annotate Case 4 (MR07 factor 1.5) and Case 6 (MR-T factor 1.25)
        if tr == "ScaleModeratorSigmaA":
            s = find_scenario(tr, "openmoc")
            if s and s.get("cells"):
                # We didn't sweep through 1.5 for openmoc-pincell-moderator-sigma-a
                # (sweep stays in [1.05, 1.40] safe zone). Annotate based on baseline
                # known value 0.476.
                ax.annotate("Case 4 basin\n(factor=1.5,\nfrom baseline)",
                             xy=(1.4, s["cells"][-1]["k_followup"]), xytext=(1.45, 0.6),
                             fontsize=8, color="#aa0000",
                             arrowprops=dict(arrowstyle="->", color="#aa0000", lw=1))
        if tr == "RaiseFuelTemperature":
            s = find_scenario(tr, "openmoc")
            if s and s.get("cells"):
                for c in s["cells"]:
                    if c.get("factor") == 1.25 and c.get("k_followup", 1) < 0.7:
                        ax.annotate("Case 6 basin",
                                     xy=(1.25, c["k_followup"]),
                                     xytext=(1.50, 0.7),
                                     fontsize=9, color="#aa0000", fontweight="bold",
                                     arrowprops=dict(arrowstyle="->", color="#aa0000", lw=1.5))
                        ax.scatter([1.25], [c["k_followup"]], s=120,
                                    edgecolor="#aa0000", facecolor="none", lw=2, zorder=5)

    fig.suptitle("MetBench parameter sweep — 5 sample points × 6 MRs (Case 4 & Case 6 OpenMOC basins surfaced)",
                  fontsize=11, fontweight="bold")
    plt.tight_layout()
    out_png = FIG_DIR / "fig2_parameter_sweep_trajectory.png"
    out_svg = FIG_DIR / "fig2_parameter_sweep_trajectory.svg"
    plt.savefig(out_png, dpi=300)
    plt.savefig(out_svg)
    plt.close(fig)
    print(f"wrote {out_png.relative_to(REPO_ROOT)}")


def fig3_kappa_heatmap() -> None:
    # Parse κ blocks from mutation-detection-matrix.md
    md = MATRIX_MD.read_text()
    blocks = re.findall(r"###\s+([^\n]+?)\s*\n\s*\nPairs evaluated:\s+(\d+)\s*/\s*(\d+)\.\s*Cohen's κ\s*=\s*\*\*([\-\d\.]+)\*\*",
                         md)
    data = []
    for label, evaluated, total, k in blocks:
        try:
            k_val = float(k)
        except ValueError:
            continue
        data.append({"label": label.strip(), "evaluated": int(evaluated), "total": int(total), "k": k_val})

    if not data:
        print("(no κ blocks parsed; skipping fig3)")
        return

    fig, ax = plt.subplots(figsize=(8, max(3, 0.4 * len(data) + 1)))
    labels = [d["label"] for d in data]
    ks = [d["k"] for d in data]
    counts = [f"{d['evaluated']}/{d['total']}" for d in data]

    # Single column heatmap, since each block has one κ
    import numpy as np
    arr = np.array(ks).reshape(-1, 1)
    im = ax.imshow(arr, cmap="RdBu", vmin=-1, vmax=1, aspect="auto")

    ax.set_yticks(range(len(labels)))
    ax.set_yticklabels(labels, fontsize=8)
    ax.set_xticks([0])
    ax.set_xticklabels(["κ"])
    ax.set_title("Cross-solver Cohen's κ on matched-pair mutations")

    # Annotate κ + pair count
    for i, (k, cnt) in enumerate(zip(ks, counts)):
        ax.text(0, i, f"{k:.3f}\n({cnt})", ha="center", va="center",
                color="white" if abs(k) > 0.5 else "black", fontsize=8)

    plt.colorbar(im, ax=ax, label="κ", shrink=0.6)
    plt.tight_layout()
    out_png = FIG_DIR / "fig3_kappa_heatmap.png"
    out_svg = FIG_DIR / "fig3_kappa_heatmap.svg"
    plt.savefig(out_png, dpi=300)
    plt.savefig(out_svg)
    plt.close(fig)
    print(f"wrote {out_png.relative_to(REPO_ROOT)}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fig", choices=["1", "2", "3", "all"], default="all")
    args = parser.parse_args()
    if args.fig in ("1", "all"):
        fig1_detection_rate()
    if args.fig in ("2", "all"):
        fig2_sweep_trajectory()
    if args.fig in ("3", "all"):
        fig3_kappa_heatmap()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
