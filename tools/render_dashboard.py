"""Render a single-file interactive HTML dashboard for MetBench results.

Wraps the same three figures as `tools/render_figures.py` (per-MR
detection rate, parameter-sweep trajectories, cross-solver κ heatmap)
into a single self-contained `docs/experiments/dashboard.html` using
Plotly. Adds a sortable bug-inventory table on top so readers can
drill into each R-Case without leaving the page.

Data inputs (all already produced by other tools):

* `docs/experiments/mutation-detection-matrix.md` — per-MR rate table
  and per-pair κ blocks.
* `docs/experiments/_data/mr-parameter-sweep.json` — sweep trajectories.
* `docs/experiments/bug-inventory.md` — R-Case rows for the inventory.

Output: `docs/experiments/dashboard.html`, a single HTML file with
Plotly.js loaded from CDN (≈3 KB script tag) and all figure data
inline. Opens in any modern browser, no server needed.

Usage:
    /opt/openmoc-venv/bin/python3 tools/render_dashboard.py
"""

from __future__ import annotations

import html
import json
import re
import sys
from pathlib import Path

import plotly.graph_objects as go
from plotly.subplots import make_subplots

REPO_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = REPO_ROOT / "docs" / "experiments" / "_data"
DOC_DIR = REPO_ROOT / "docs" / "experiments"
MATRIX_MD = DOC_DIR / "mutation-detection-matrix.md"
SWEEP_JSON = DATA_DIR / "mr-parameter-sweep.json"
INVENTORY_MD = DOC_DIR / "bug-inventory.md"
OUT_HTML = DOC_DIR / "dashboard.html"

# Same scenario metadata as render_figures.py — kept in sync manually.
SCENARIO_META = {
    "openmoc-pincell-nu-sigma-f":                            ("Phase-1", "m_mono", "openmoc"),
    "openmc-pincell-nu-sigma-f":                             ("Phase-1", "m_mono", "openmc"),
    "openmoc-pincell-sigma-a":                               ("Phase-1", "m_mono", "openmoc"),
    "openmc-pincell-sigma-a":                                ("Phase-1", "m_mono", "openmc"),
    "openmoc-pincell-group-permute":                         ("Phase-2", "m_inv",  "openmoc"),
    "openmc-pincell-group-permute":                          ("Phase-2", "m_inv",  "openmc"),
    "openmoc-pincell-fuel-sigma-t":                          ("Phase-2", "m_mono", "openmoc"),
    "openmc-pincell-fuel-sigma-t":                           ("Phase-2", "m_mono", "openmc"),
    "openmoc-pincell-moderator-sigma-a":                     ("Phase-2", "m_mono", "openmoc"),
    "openmc-pincell-moderator-sigma-a":                      ("Phase-2", "m_mono", "openmc"),
    "openmoc-pincell-fuel-sigma-s":                          ("Phase-2", "m_mono", "openmoc"),
    "openmc-pincell-fuel-sigma-s":                           ("Phase-2", "m_mono", "openmc"),
    "openmoc-pincell-fuel-radius":                           ("Phase-2", "m_mono", "openmoc"),
    "openmc-pincell-fuel-radius":                            ("Phase-2", "m_mono", "openmc"),
    "openmc-pincell-particles-refine":                       ("Phase-2", "m_conv", "openmc"),
    "openmoc-pincell-rotate-90":                             ("Phase-2", "m_inv",  "openmoc"),
    "openmc-pincell-rotate-90":                              ("Phase-2", "m_inv",  "openmc"),
    "openmoc-pincell-mirror-x":                              ("Phase-2", "m_inv",  "openmoc"),
    "openmc-pincell-mirror-x":                               ("Phase-2", "m_inv",  "openmc"),
    "openmoc-pincell-mirror-y":                              ("Phase-2", "m_inv",  "openmoc"),
    "openmc-pincell-mirror-y":                               ("Phase-2", "m_inv",  "openmc"),
    "openmoc-pincell-mirror-x-tally":                        ("Phase-3", "m_inv",  "openmoc"),
    "openmc-pincell-mirror-x-tally":                         ("Phase-3", "m_inv",  "openmc"),
    "openmoc-pincell-mirror-y-tally":                        ("Phase-3", "m_inv",  "openmoc"),
    "openmc-pincell-mirror-y-tally":                         ("Phase-3", "m_inv",  "openmc"),
    "openmoc-pincell-fuel-temperature":                      ("Phase-3", "m_mono", "openmoc"),
    "openmc-pincell-fuel-temperature":                       ("Phase-3", "m_mono", "openmc"),
    "openmc-pincell-fuel-temperature-via-add-temperature":   ("Phase-3", "m_mono", "openmc"),
    "openmc-pincell-moderator-temperature-via-borated-water": ("Phase-3", "m_mono", "openmc"),
}

MP_ORDER = ["m_inv", "m_mono", "m_conv", "m_cmp"]
MP_LABEL = {"m_inv": "symmetry", "m_mono": "monotonicity", "m_conv": "limit", "m_cmp": "method-cmp"}
SOLVER_COLOR = {"openmoc": "#4477aa", "openmc": "#ee6655"}


def wilson_ci(k: int, n: int, z: float = 1.96) -> tuple[float, float]:
    if n == 0:
        return (0.0, 1.0)
    p = k / n
    denom = 1 + z * z / n
    center = (p + z * z / (2 * n)) / denom
    margin = z * ((p * (1 - p) / n + z * z / (4 * n * n)) ** 0.5) / denom
    return (max(0.0, center - margin), min(1.0, center + margin))


def parse_per_mr_table(md: str) -> list[dict]:
    rows = []
    in_section = False
    for line in md.splitlines():
        if line.startswith("## Per-MR detection rate"):
            in_section = True
            continue
        if in_section:
            if line.startswith("## ") and "Per-MR" not in line:
                break
            m = re.match(r"\|\s*([\w\-]+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|", line)
            if m:
                rows.append({
                    "scenario": m.group(1),
                    "n": int(m.group(2)),
                    "detected": int(m.group(3)),
                    "missed": int(m.group(4)),
                    "errors": int(m.group(5)),
                })
    return rows


def parse_kappa_blocks(md: str) -> list[dict]:
    pattern = re.compile(
        r"###\s+([^\n]+?)\s*\n\s*\nPairs evaluated:\s+(\d+)\s*/\s*(\d+)\.\s*Cohen's κ\s*=\s*\*\*([\-\d\.]+)\*\*"
    )
    return [{"label": m.group(1).strip(), "evaluated": int(m.group(2)),
             "total": int(m.group(3)), "k": float(m.group(4))}
            for m in pattern.finditer(md)]


def parse_bug_inventory(md: str) -> list[dict]:
    """Parse the real-bug table rows from bug-inventory.md."""
    rows = []
    in_real = False
    for line in md.splitlines():
        if line.startswith("## Real bugs"):
            in_real = True
            continue
        if in_real and line.startswith("## ") and "Real" not in line:
            break
        if in_real and line.startswith("| R-Case-"):
            cells = [c.strip() for c in line.split("|")[1:-1]]
            if len(cells) >= 9:
                rows.append({
                    "id": cells[0],
                    "source": cells[1],
                    "commit": cells[2].strip("`"),
                    "date": cells[3],
                    "files": cells[4],
                    "bug": cells[5],
                    "mp": cells[6],
                    "mr": cells[7],
                    "state": cells[8],
                })
    return rows


def build_fig1(rows: list[dict]) -> go.Figure:
    enriched = []
    for r in rows:
        meta = SCENARIO_META.get(r["scenario"])
        if meta is None:
            continue
        phase, mp, solver = meta
        n_dec = r["detected"] + r["missed"]
        lo, hi = wilson_ci(r["detected"], n_dec) if n_dec > 0 else (0.0, 0.0)
        rate = (r["detected"] / n_dec) if n_dec > 0 else 0.0
        enriched.append({
            **r, "phase": phase, "mp": mp, "solver": solver,
            "n_dec": n_dec, "rate": rate, "lo": lo, "hi": hi,
        })
    enriched.sort(key=lambda r: (MP_ORDER.index(r["mp"]) if r["mp"] in MP_ORDER else 99,
                                  r["phase"], r["solver"], r["scenario"]))

    fig = go.Figure()
    for solver in ("openmoc", "openmc"):
        xs, ys, los, his, customs = [], [], [], [], []
        for r in enriched:
            if r["solver"] != solver:
                continue
            short = r["scenario"].replace("openmoc-pincell-", "").replace("openmc-pincell-", "")
            xs.append(f"{short} [{r['mp']}]")
            ys.append(r["rate"] * 100)
            los.append((r["rate"] - r["lo"]) * 100)
            his.append((r["hi"] - r["rate"]) * 100)
            customs.append([r["scenario"], r["mp"], r["phase"], r["detected"],
                            r["missed"], r["errors"], r["n_dec"]])
        fig.add_trace(go.Bar(
            x=xs, y=ys,
            error_y=dict(type="data", array=his, arrayminus=los, visible=True, color="#333"),
            name=solver.upper(),
            marker=dict(color=SOLVER_COLOR[solver],
                        pattern=dict(shape="/" if solver == "openmc" else "")),
            customdata=customs,
            hovertemplate=(
                "<b>%{customdata[0]}</b><br>"
                "MetaPattern: %{customdata[1]} (%{customdata[2]})<br>"
                "detected: %{customdata[3]} / %{customdata[6]}<br>"
                "missed: %{customdata[4]}, errors: %{customdata[5]}<br>"
                "rate: %{y:.1f}%<extra></extra>"
            ),
        ))

    fig.update_layout(
        title="Per-MR detection rate by scenario (Wilson 95% CI)",
        xaxis_title="scenario [MetaPattern]",
        yaxis_title="detection rate (%)",
        yaxis_range=[0, 108],
        barmode="group",
        legend=dict(orientation="h", yanchor="bottom", y=1.02, xanchor="right", x=1),
        height=520,
        margin=dict(l=60, r=20, t=70, b=160),
    )
    fig.update_xaxes(tickangle=-50, tickfont=dict(size=10))
    return fig


def build_fig2(sweep: dict) -> go.Figure:
    transforms = ["ScaleFuelSigmaT", "ScaleFuelSigmaS", "ScaleModeratorSigmaA",
                  "ScaleFuelRadius", "RefineParticles", "RaiseFuelTemperature"]
    titles = {
        "ScaleFuelSigmaT":      "MR05 ScaleFuelSigmaT (m_mono)",
        "ScaleFuelSigmaS":      "MR06 ScaleFuelSigmaS (m_mono)",
        "ScaleModeratorSigmaA": "MR07 ScaleModeratorSigmaA (m_mono)",
        "ScaleFuelRadius":      "MR08 ScaleFuelRadius (m_mono)",
        "RefineParticles":      "MR12 RefineParticles (m_conv, OpenMC only)",
        "RaiseFuelTemperature": "MR-T RaiseFuelTemperature (m_mono)",
    }
    fig = make_subplots(rows=2, cols=3, subplot_titles=[titles[t] for t in transforms])

    def find_scenario(transform: str, solver: str) -> dict | None:
        for sc_id, s in sweep["scenarios"].items():
            if s.get("solver") != solver:
                continue
            key = transform.lower().replace("scale", "").replace("raise", "")
            if key in sc_id.replace("-", "").lower():
                return {"id": sc_id, **s}
        return None

    for i, tr in enumerate(transforms):
        row, col = i // 3 + 1, i % 3 + 1
        for solver in ("openmoc", "openmc"):
            s = find_scenario(tr, solver)
            if s is None or not s.get("cells"):
                continue
            factors, ks, sigmas, outs, notes = [], [], [], [], []
            for c in s["cells"]:
                if "error" in c:
                    continue
                factors.append(c["factor"])
                ks.append(c["k_followup"])
                sigmas.append(c.get("k_followup_std", 0.0))
                outs.append(c.get("outcome", "?"))
                notes.append(c.get("note", ""))
            if not factors:
                continue
            customs = list(zip(sigmas, outs, notes))
            fig.add_trace(
                go.Scatter(
                    x=factors, y=ks, mode="lines+markers",
                    name=f"{solver.upper()} ({tr})",
                    marker=dict(color=SOLVER_COLOR[solver],
                                symbol="circle" if solver == "openmoc" else "square",
                                size=8),
                    line=dict(color=SOLVER_COLOR[solver], width=1.5),
                    customdata=customs,
                    hovertemplate=(
                        f"<b>{solver.upper()} — {tr}</b><br>"
                        "factor=%{x:.3f}<br>"
                        "k_followup=%{y:.5f}<br>"
                        "σ=%{customdata[0]:.5f}<br>"
                        "outcome=%{customdata[1]}<br>"
                        "note=%{customdata[2]}<extra></extra>"
                    ),
                    showlegend=(i == 0),
                    legendgroup=solver,
                ),
                row=row, col=col,
            )
            # Source baseline horizontal reference
            fig.add_hline(y=s["k_source"], line_dash="dot",
                          line_color=SOLVER_COLOR[solver], line_width=0.8,
                          opacity=0.5, row=row, col=col)

        # Annotations for Case 4 / Case 6 basins
        if tr == "RaiseFuelTemperature":
            s_moc = find_scenario(tr, "openmoc")
            if s_moc:
                for c in s_moc.get("cells", []):
                    if c.get("factor") == 1.25 and c.get("k_followup", 1) < 0.7:
                        fig.add_annotation(
                            x=1.25, y=c["k_followup"],
                            text="★ Case 6 basin<br>(MetBench self-discovery)",
                            showarrow=True, arrowhead=2, arrowcolor="#aa0000",
                            font=dict(color="#aa0000", size=10),
                            ax=80, ay=-40,
                            row=row, col=col,
                        )
        if tr == "ScaleModeratorSigmaA":
            fig.add_annotation(
                x=1.4, y=1.0,
                text="↗ Case 4 basin sits at factor=1.5<br>(outside the safe sweep band [1.05, 1.40];<br>see cross-program-report.md)",
                showarrow=False,
                font=dict(color="#aa0000", size=9),
                row=row, col=col,
            )

    fig.update_layout(
        title="MR parameter sweep — 5 sample points × 6 MRs (Case 4 & Case 6 OpenMOC basins)",
        height=720,
        legend=dict(orientation="h", yanchor="bottom", y=-0.12, xanchor="center", x=0.5),
        margin=dict(l=50, r=20, t=70, b=80),
    )
    for r_ in (1, 2):
        for c_ in (1, 2, 3):
            fig.update_xaxes(title_text="factor", row=r_, col=c_)
            fig.update_yaxes(title_text="k_followup", row=r_, col=c_)
    return fig


def build_fig3(blocks: list[dict]) -> go.Figure:
    if not blocks:
        return go.Figure()
    labels = [b["label"] for b in blocks]
    ks = [b["k"] for b in blocks]
    counts = [f"{b['evaluated']}/{b['total']}" for b in blocks]
    text = [[f"κ={k:.3f}<br>({c})"] for k, c in zip(ks, counts)]

    fig = go.Figure(data=go.Heatmap(
        z=[[k] for k in ks],
        x=["κ"],
        y=labels,
        colorscale="RdBu", zmin=-1, zmax=1,
        text=text, texttemplate="%{text}",
        textfont=dict(size=10),
        hovertemplate="<b>%{y}</b><br>κ = %{z:.3f}<extra></extra>",
        colorbar=dict(title="Cohen's κ"),
    ))
    fig.update_layout(
        title="Cross-solver Cohen's κ on matched-pair mutations",
        height=max(360, 36 * len(labels) + 80),
        margin=dict(l=320, r=80, t=70, b=40),
    )
    return fig


def render_inventory_table(bug_rows: list[dict]) -> str:
    headers = ["ID", "Source", "Commit", "Date", "Files", "Bug", "MP", "Catching MR", "Live state"]
    keys = ["id", "source", "commit", "date", "files", "bug", "mp", "mr", "state"]
    parts = ['<table class="bug-table" id="bug-inventory-table">',
             "<thead><tr>"]
    for h in headers:
        parts.append(f'<th onclick="sortBugTable({headers.index(h)})">{html.escape(h)} ⇅</th>')
    parts.append("</tr></thead><tbody>")
    for r in bug_rows:
        parts.append("<tr>")
        for k in keys:
            v = r.get(k, "")
            # Keep inline-code formatting from markdown roughly intact
            v = re.sub(r"`([^`]+)`", r"<code>\1</code>", v)
            v = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", v)
            parts.append(f"<td>{v}</td>")
        parts.append("</tr>")
    parts.append("</tbody></table>")
    return "\n".join(parts)


def render_dashboard() -> None:
    md = MATRIX_MD.read_text()
    rows = parse_per_mr_table(md)
    kappa = parse_kappa_blocks(md)
    sweep = json.loads(SWEEP_JSON.read_text())
    bug_rows = parse_bug_inventory(INVENTORY_MD.read_text())

    fig1 = build_fig1(rows)
    fig2 = build_fig2(sweep)
    fig3 = build_fig3(kappa)

    # First figure pulls plotly.js from CDN; subsequent ones reuse it.
    div1 = fig1.to_html(include_plotlyjs="cdn", full_html=False,
                        div_id="fig1", default_height=520)
    div2 = fig2.to_html(include_plotlyjs=False, full_html=False,
                        div_id="fig2", default_height=720)
    div3 = fig3.to_html(include_plotlyjs=False, full_html=False,
                        div_id="fig3", default_height=max(360, 36 * len(kappa) + 80))

    inventory_html = render_inventory_table(bug_rows)

    summary = {
        "scenarios": len(rows),
        "kappa_pairs": len(kappa),
        "real_bugs": len(bug_rows),
        "live_in_matrix": sum(1 for r in bug_rows
                              if "live in matrix" in r.get("state", "").lower()
                              or "live confirmed" in r.get("state", "").lower()),
    }

    html_body = f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>MetBench v2.1.4 — interactive results dashboard</title>
<style>
  body {{ font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
          margin: 0; padding: 0; color: #222; background: #fafbfc; }}
  header {{ background: #1c2533; color: white; padding: 20px 32px; }}
  header h1 {{ margin: 0; font-size: 22px; }}
  header .sub {{ font-size: 13px; opacity: 0.85; margin-top: 4px; }}
  .stats {{ display: flex; gap: 16px; margin-top: 12px; }}
  .stat {{ background: rgba(255,255,255,0.12); padding: 8px 14px;
           border-radius: 4px; font-size: 12px; }}
  .stat b {{ display: block; font-size: 18px; font-weight: 600;
             color: #fafa66; }}
  main {{ max-width: 1280px; margin: 0 auto; padding: 24px 16px 48px; }}
  section {{ background: white; border: 1px solid #e3e5e8;
             border-radius: 6px; padding: 20px 24px; margin-bottom: 24px;
             box-shadow: 0 1px 3px rgba(0,0,0,0.04); }}
  section h2 {{ margin: 0 0 4px; font-size: 17px; color: #1c2533; }}
  section .lede {{ color: #555; font-size: 13px; margin: 0 0 14px;
                   line-height: 1.45; }}
  table.bug-table {{ border-collapse: collapse; width: 100%;
                     font-size: 12px; table-layout: auto; }}
  table.bug-table th {{ background: #ecf0f4; color: #1c2533;
                        padding: 8px 6px; text-align: left;
                        border-bottom: 2px solid #cdd3d9;
                        cursor: pointer; user-select: none;
                        white-space: nowrap; }}
  table.bug-table th:hover {{ background: #d8e0e8; }}
  table.bug-table td {{ padding: 7px 6px; border-bottom: 1px solid #eef0f3;
                        vertical-align: top; }}
  table.bug-table tr:hover td {{ background: #f7f9fb; }}
  table.bug-table code {{ font-family: ui-monospace, "SF Mono", monospace;
                          font-size: 11px; background: #eef2f6;
                          padding: 1px 4px; border-radius: 3px; }}
  footer {{ max-width: 1280px; margin: 0 auto;
            padding: 16px; color: #888; font-size: 12px; }}
  .pill {{ display: inline-block; padding: 1px 6px; border-radius: 8px;
           background: #ecf0f4; font-size: 11px; font-family: ui-monospace, monospace;
           color: #555; }}
  .pill.live {{ background: #d6f5dd; color: #145a32; }}
  .pill.cross {{ background: #fde6c4; color: #6e3814; }}
  .source-link {{ color: #2c6ec7; text-decoration: none; font-size: 12px; }}
  .source-link:hover {{ text-decoration: underline; }}
</style>
</head>
<body>
<header>
  <h1>MetBench V2.1.4 — interactive results dashboard</h1>
  <div class="sub">
    Phase-3 / Stage-5 snapshot — auto-generated by
    <code>tools/render_dashboard.py</code>.
    Static PNG/SVG equivalents live in
    <code>docs/experiments/figures/</code>.
  </div>
  <div class="stats">
    <div class="stat"><b>{summary['scenarios']}</b>scenarios in matrix</div>
    <div class="stat"><b>{summary['kappa_pairs']}</b>matched-pair κ blocks</div>
    <div class="stat"><b>{summary['real_bugs']}</b>real bugs (R-Cases)</div>
    <div class="stat"><b>{summary['live_in_matrix']}</b>live in matrix</div>
  </div>
</header>
<main>

<section>
  <h2>1. Real-bug inventory</h2>
  <p class="lede">
    All six R-Cases extracted from OpenMOC / OpenMC fix-commit history or
    discovered live by MetBench's MR matrix. Click a column header to sort.
    Live-in-matrix means the bug surfaces as a <code>status=error</code> cell
    (with a marker <code>RuntimeError</code>) when MetBench runs the
    corresponding scenario; see
    <a class="source-link" href="bug-inventory.md">bug-inventory.md</a>
    for the cross-link to synthetic mutants.
  </p>
  {inventory_html}
</section>

<section>
  <h2>2. Per-MR detection rate</h2>
  <p class="lede">
    Wilson-95 % CI on each scenario's mutation detection rate, grouped by
    MetaPattern (m_inv symmetry → m_mono monotonicity → m_conv limit). Hover a
    bar for the underlying detected/missed/error counts. OpenMC bars are
    hatched. The empty bars on the borated-water and add-temperature
    scenarios are expected — Mut00 is the only mutation that fires on them,
    and it reports <code>status=error</code> rather than detected/missed.
  </p>
  {div1}
</section>

<section>
  <h2>3. MR parameter sweep — 5 sample points × 6 MRs</h2>
  <p class="lede">
    Each subplot is one MR. Solid lines plot k_followup as a function of the
    transform factor; the dotted horizontal reference is the source k. The
    ★ on the MR-T panel is Case 6 (OpenMOC <code>CPUSolver</code> second
    convergence basin at fuel-temperature factor = 1.25). The Case 4 basin
    on MR07 sits at factor = 1.5, outside the safe sweep band; see
    <a class="source-link" href="cross-program-report.md">cross-program-report.md</a>.
  </p>
  {div2}
</section>

<section>
  <h2>4. Cross-solver Cohen's κ heatmap</h2>
  <p class="lede">
    κ = 1.00 means the two solvers detect (or fail to detect) the same matched
    mutation pairs perfectly; κ = 0 means random agreement. The two slips
    below 1.0 (MR02 mirror-x, sa-no-sigt-update) are documented design points,
    not regressions. See
    <a class="source-link" href="mutation-detection-matrix.md">mutation-detection-matrix.md</a>
    for per-pair raw data.
  </p>
  {div3}
</section>

</main>
<footer>
  Source files behind this dashboard:
  <code>tools/mutation_study.py</code>,
  <code>tools/mr_parameter_sweep.py</code>,
  <code>tools/cross_program_mr.py</code>,
  <code>tools/real_bugs_live_repro.py</code>,
  <code>tools/render_dashboard.py</code>.
  Open the markdown reports for full context.
</footer>

<script>
function sortBugTable(colIdx) {{
  const tbl = document.getElementById("bug-inventory-table");
  const tbody = tbl.tBodies[0];
  const rows = Array.from(tbody.querySelectorAll("tr"));
  const isAsc = tbl.getAttribute("data-sort-col") === String(colIdx)
              ? tbl.getAttribute("data-sort-dir") === "desc"
              : true;
  rows.sort((a, b) => {{
    const av = a.cells[colIdx].innerText.trim();
    const bv = b.cells[colIdx].innerText.trim();
    return isAsc ? av.localeCompare(bv, undefined, {{numeric: true}})
                 : bv.localeCompare(av, undefined, {{numeric: true}});
  }});
  rows.forEach(r => tbody.appendChild(r));
  tbl.setAttribute("data-sort-col", String(colIdx));
  tbl.setAttribute("data-sort-dir", isAsc ? "asc" : "desc");
}}
</script>
</body>
</html>
"""

    OUT_HTML.write_text(html_body, encoding="utf-8")
    size_kb = OUT_HTML.stat().st_size / 1024
    print(f"wrote {OUT_HTML.relative_to(REPO_ROOT)} ({size_kb:.1f} KB) — "
          f"{summary['scenarios']} scenarios, {summary['kappa_pairs']} κ blocks, "
          f"{summary['real_bugs']} real bugs")


def main() -> int:
    if not MATRIX_MD.exists():
        print(f"missing: {MATRIX_MD}", file=sys.stderr)
        return 1
    if not SWEEP_JSON.exists():
        print(f"missing: {SWEEP_JSON}", file=sys.stderr)
        return 1
    if not INVENTORY_MD.exists():
        print(f"missing: {INVENTORY_MD}", file=sys.stderr)
        return 1
    render_dashboard()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
