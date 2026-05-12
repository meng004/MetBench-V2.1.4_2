# Stage 5 Phase 3 — Visualization plan

> Plan for graphical visualization of MetBench's MR-matrix experiment
> results. The project currently ships **only tabular** outputs
> (Markdown tables + CSV exports + auto-generated reports). This plan
> proposes a focused set of charts that surface the most decision-
> relevant patterns and respect the cross-platform / CI constraints
> documented in `CLAUDE.md` (Linux cloud session must build everything
> the WPF Windows VM doesn't already cover).

## Status quo — what we already have

| Output | Type | File |
|--------|------|------|
| Per-MR detection rate + Wilson 95% CI | Markdown table | `mutation-detection-matrix.md` |
| Per-mutation cell detail (k_source / k_followup / outcome) | Markdown table | same |
| Cohen's κ on 17 matched pairs | Markdown table | same |
| Threshold sensitivity sweep | Markdown table | same |
| Cross-program disagreements (baseline + per-pair) | Markdown table | `cross-program-report.md` |
| LLM-filter calibration scorecard | Markdown table | `calibration-report.md` |
| MR parameter sweep ≥5 points per MR | Markdown table | `mr-parameter-sweep.md` |
| Raw data backing every table | CSV / JSON | `_data/*.json`, `*.csv` |
| Real-bug live-trigger scorecard | Markdown table | `real-bugs-live-report.md` |
| Bug inventory cross-link | Markdown table | `bug-inventory.md` |
| MR effectiveness by MetaPattern | Markdown table | `mr-effectiveness-by-metapattern.md` |

What we **don't** have: any chart / plot / heatmap / HTML dashboard.
WPF UI (Windows-only) has `LiveChartsCore.SkiaSharpView.WPF`
references but those don't help the cloud Linux session.

## Why plots matter for this study

Three patterns are hard to read off Markdown tables but jump out of
simple plots:

1. **Per-MR detection rate over time / phase**: Phase-1 had 4
   scenarios; Phase-2 added 6; Phase-3 added 4 tally + 2 temperature.
   The audit narrative "MR detection improves with each phase" wants
   a single grouped bar chart.
2. **Parameter-sweep trajectories**: at 5 sample points per MR, a
   k_eff-vs-factor curve makes the Case 4 / Case 6 OpenMOC convergence
   basins visually obvious (one factor sliver collapses, neighbours
   stay smooth). The current table form needs the reader to mentally
   plot it.
3. **Cross-solver κ matrix**: 11 matched-pair classes × {classical /
   extended / synthetic-only} is naturally a heatmap.

## Scope — three charts, ship-or-skip

Each chart below is sized "≤30 lines of matplotlib + ≤5 min runtime".
Heavy interactive dashboards are explicitly out of scope.

### Chart 1 — Per-MR detection rate (grouped bar with Wilson 95% CI)

* **Source data**: `_data/candidates/*/matrix.json` aggregated by
  `tools/mutation_study.py::stats` (the CSV `mutation-detection-matrix.csv`
  already has the per-scenario counts).
* **Y axis**: detection rate (0-100%).
* **X axis**: 27 scenarios, grouped by NOETHER MetaPattern
  (`m_inv` / `m_mono` / `m_conv` / `m_cmp`).
* **Bars**: solid for OpenMOC, hatched for OpenMC, paired side-by-
  side per scenario.
* **Error bars**: Wilson 95% CI (already computed in the report;
  re-derive in Python from `(detected, n)` per scenario).
* **Headline annotations**:
  - "Phase 1" / "Phase 2" / "Phase 3" colored vertical bands behind
    bars to make the per-phase contribution visually obvious.
  - Star (★) on MR-T to mark "first classical-MT live finding of an
    unknown bug (Case 6)".
* **File**: `docs/experiments/figures/fig1_per_mr_detection_rate.png`
  (+ `.svg`).

### Chart 2 — Parameter-sweep trajectory (k_eff vs factor)

* **Source data**: `_data/mr-parameter-sweep.json`.
* **Layout**: 6 panels (one per sweepable MR: MR05, MR06, MR07, MR08,
  MR12, MR-T) in a 2×3 grid.
* **Each panel**:
  - X axis: factor (log-scale where appropriate).
  - Y axis: k_followup (left); k_followup_std on a second axis for MR12.
  - Two lines: OpenMOC (solid) and OpenMC (dashed with ±σ shaded
    region from the 5000-particle MC noise).
  - Reference horizontal line at unpatched k_source.
* **Headline annotations**:
  - **Red circle** at `(MR-T, factor=1.25)` labelled "Case 6 OpenMOC
    basin" — the standout finding.
  - Smaller red circle at `(MR07, factor=1.5)` labelled "Case 4
    OpenMOC basin" — pre-existing.
  - For MR12: predicted-σ curve (1/√factor) overlaid for OpenMC.
* **File**: `docs/experiments/figures/fig2_parameter_sweep_trajectory.png`.

### Chart 3 — Cross-solver κ heatmap

* **Source data**: `mutation-detection-matrix.md` parsed for the κ
  blocks, or recomputed from `_data/candidates/*/matrix.json`
  matched-pair index in `tools/mutation_study.py::MATCHED_PAIRS`.
* **Layout**: 11 matched-pair categories × 5 MR scenarios where the
  pair is meaningfully evaluated → ~30 cells.
* **Color**: κ value in [-1, 1], colormap diverging (red = low /
  worse than chance, white = 0, blue = 1). Annotate each cell with
  the integer κ × 100 (e.g. "100" for κ=1.000).
* **Side panel**: pair count next to each category (e.g. "4/4
  evaluated" for ScaleNuSigmaF).
* **File**: `docs/experiments/figures/fig3_kappa_heatmap.png`.

## Implementation plan

### New script: `tools/render_figures.py`

```python
"""Render the three Phase-3 figures into docs/experiments/figures/.
Reads existing _data/* JSON / CSV; no solver re-runs needed.
"""
```

Dependencies: `matplotlib` only (already a soft dep of the OpenMC
conda env; we'll fall back to `pip install --user matplotlib` if
absent). No seaborn, no plotly — keep environment minimal.

Three top-level functions (`fig1_detection_rate`, `fig2_sweep_trajectory`,
`fig3_kappa_heatmap`) each:
1. Read source data file
2. Produce a single matplotlib `Figure`
3. Save to PNG (300 dpi) + SVG (for paper inclusion)

CLI: `python3 tools/render_figures.py [--fig 1|2|3|all]`.

### Embedding in existing reports

After the script lands, **insert image references** in:
* `PHASE2.md`: insert all three figures in a new "Visual summary" section near the top.
* `mr-parameter-sweep.md`: insert Fig 2 right under "Headline findings".
* `mr-effectiveness-by-metapattern.md`: insert Fig 1 + Fig 3.

Use plain Markdown `![alt](figures/figN_*.png)` so GitHub / Pandoc /
VSCode all render them. Keep PNG (rasterized) for portability and
SVG (vector) for paper-quality inclusion.

### Validation

Before merging the figures:
1. Run `python3 tools/render_figures.py --fig all`; verify all
   three PNGs land in `figures/`.
2. Open each in a viewer; confirm the headline annotations (Case 6
   red circle on Fig 2, Phase color bands on Fig 1, κ values readable
   on Fig 3).
3. Spot-check 3 data points per figure against the source Markdown
   tables (no transcription errors).

### Estimated effort

| Step | Effort |
|------|-------:|
| `tools/render_figures.py` skeleton + Fig 1 | 1-2 hours |
| Fig 2 (sweep trajectory) | 1-2 hours |
| Fig 3 (κ heatmap) | 1 hour |
| Embedding + Markdown edits | 30 min |
| Validation | 30 min |
| **Total** | **~half day** |

## Out of scope (documented as "deferred")

* **Interactive HTML dashboard** (plotly / bokeh / d3): nice-to-have
  for an internal exploration tool; not needed for paper-quality
  static figures. Defer until someone needs to do live exploration.
* **Per-mutation k_eff time-series**: the matrix isn't time-stamped;
  we'd need to instrument the runner to dump per-iteration k_eff.
  Useful for diagnosing Case-4 / Case-6 basins; deferred to
  follow-up "OpenMOC pathology characterisation" PR.
* **WPF UI integration**: the existing `LiveChartsCore` references
  in `MetBench_Client/` are Windows-only and live on the WPF track,
  not the cloud Linux track. If the user wants the figures in the
  WPF UI, the Windows VM developer can add a `WebView2` that points
  at the PNG files — no Linux-side work needed.

## Hand-off

After landing this plan as PR-3:
* The three figures become the visual core of any paper-style writeup
  of MetBench's MR-matrix study.
* `bug-inventory.md` (the synthetic ↔ real cross-link table) + the
  three figures are sufficient to make the "Classical MT finds
  unknown bugs (1 case)" + "Extended MT finds unknown bugs (1 case)"
  + "Synthetic + real mix" narrative without needing the reader to
  scroll Markdown tables.

If the user wants to push further visualization (animated power-
iteration trace, geometry plots of the off-centre / asymmetric pin-
cell variants, MGXS data visualization), Phase-3 PR-4 can pick those
up — but they're net-new SUT instrumentation rather than reading
existing matrix data, so deferred until a concrete consumer asks.
