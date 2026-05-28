# P6 — Author-Side `/code-review` Advisory (v2 charter §6 P6)

> Scoped plan for v2 charter P6: operationalize module F (Author-Side Advisory).
> Docs-only PR. No fact / workflow / source-code changes.
> Charter: [`docs/superpowers/specs/2026-05-28-code-governance-v2-charter.md`](../specs/2026-05-28-code-governance-v2-charter.md) §3 F + §6 P6.

## §1 Goal & Acceptance

Make module F (author-side `/code-review` superpowers skill) actionable by adding
two short pointers that tell the author **when** to invoke it: (a) a CLAUDE.md
§12.2 advisory note naming the high-leverage path families, and (b) a non-blocking
sub-check in the PR Gate Checklist's `## Review` section. Module F's gate type
remains "non-gate, author discretion" (v2 charter §3 F). No grep, no CI, no
required check.

Acceptance: a fresh reader of CLAUDE.md §12 and `pr-gate-checklist.md` can,
without reading the v2 charter, answer "when should I run `/code-review` before
pushing this PR?" with a concrete path-family list.

## §2 Current-State Inventory

`grep -n "/code-review" CLAUDE.md docs/superpowers/templates/pr-gate-checklist.md`
returns 7 hits in `CLAUDE.md`, 0 in the checklist:

| Location | Role | Touch in this PR? |
|---|---|---|
| `CLAUDE.md:456` (§12.1 diagram, module E lane) | `/code-review ultra` for chain-end | No |
| `CLAUDE.md:461` (§12.1 diagram, module F lane) | `/code-review low/medium/high` | No |
| `CLAUDE.md:477` (§12.2 table, module F row) | tool reference | No (text), but new paragraph added immediately below table |
| `CLAUDE.md:499` (§12.4 R2 title) | "由模块 E 实现 (`/code-review ultra`)" | No (R1-R4 text frozen by §0.5) |
| `CLAUDE.md:505` (§12.4 R2 detail) | ultra invocation example | No |
| `CLAUDE.md:518` (§12.4 R4 title) | "由模块 C 实现 (`/code-review high` 半自动)" | No (frozen) |
| `CLAUDE.md:523` (§12.4 R4 detail) | "PR 触碰 `MetBench_BLL.Core/SystemMT/Reporting/` 或 `Catalog/Editing/` 时，作者侧建议先跑 `/code-review high`" | No (frozen) — but Plan §3.3 deduplicates by **generalizing** in §12.2, leaving R4 unchanged |
| `pr-gate-checklist.md` | 0 mentions | New sub-check added to `## Review` |

§12.2 module F row currently has no sub-bullet enumerating path families. §12.4
R4 detail line already covers `Reporting/` + `Catalog/Editing/`; this Plan
**broadens** that list in §12.2 (not in R4) and references R4 from there.

## §3 Design

### 3.1 CLAUDE.md change

Insertion point: immediately **after** the §12.2 table (after line 477) and
**before** the `### 12.3 强约束` header. A short paragraph, not a table-cell edit
(rationale: table cells render single-line best; multi-path enumeration belongs
below the table). R1-R4 text is **not** touched.

Verbatim insertion (4 lines markdown blockquote):

```
> **模块 F 触发建议（非强制）**：PR push 前若 diff 触碰下列路径族之一，作者侧建议先跑 `/code-review high` 自查 Cat A 语义 bug：
> `MetBench_BLL.Core/SystemMT/Reporting/`（公开契约 ↔ fact 配对，详见 §12.4 R4）、`Catalog/Editing/`、`Catalog/Typed/Runtime/`（predicate / discoverer 逻辑分支）、`MetBench_Analyzers/`（Roslyn diagnostic 误报风险）。
> 跨链 PR（≥ 3-PR chain 的最后一个）改用 `/code-review ultra`，由模块 E ritual 触发（详见 §12.4 R2）。
> 该建议是模块 F 的操作化指引，不改 §12.4 R4 中关于 `Reporting/` + `Catalog/Editing/` 的既有半自动核对要求。
```

### 3.2 pr-gate-checklist.md change

Insertion point: `## Review` section, **after** the three existing `- [ ]` items
(after line 49 `Review explicitly checked for status drift...`), still **before**
the `## Merge` heading. Single sub-check, same format as siblings.

Verbatim insertion (1 line markdown):

```
- [ ] **Author-side `/code-review` advisory (CLAUDE.md §12.2 module F)**: if this PR touches `MetBench_BLL.Core/SystemMT/Reporting/`, `Catalog/Editing/`, `Catalog/Typed/Runtime/`, or `MetBench_Analyzers/`, `/code-review high` was run locally and its output was either acted on or explicitly dismissed. Non-blocking; skip with a one-line reason if not applicable.
```

### 3.3 Deduplication with §12.4 R4 — choice (b)

Three options were considered:
- (a) Move R4 trailing advisory line up to §12.2 and delete it from R4. **Rejected**: CLAUDE.md §0.5 forbids editing R1-R4 text.
- (b) Keep R4 text verbatim; add a **more generic** advisory below §12.2 table that enumerates 4 path families (R4 covers only 2) and points back at R4 for the contract-fact rationale. **Chosen.**
- (c) Only edit `pr-gate-checklist.md`, leave CLAUDE.md untouched. **Rejected**: P6 charter row says "CLAUDE.md §12 加作者侧 advisory **+** `pr-gate-checklist.md` Review 节加 advisory bullet" — both are in scope.

Rationale for (b): the new §12.2 paragraph is **additive and broader** (`Reporting/`,
`Catalog/Editing/`, `Catalog/Typed/Runtime/`, `MetBench_Analyzers/`) and **cites
R4** for the narrower contract-fact context, so a reader who lands on R4 first
still sees the localized invocation; a reader who lands on §12.2 first sees the
broader list. No text duplication (the R4 line stays as-is; the §12.2 line
explicitly says "不改 §12.4 R4 中...的既有半自动核对要求").

## §4 Test Strategy

Docs-only PR. No fact added, no workflow modified, no source compilation.

Manual smoke (executed on the PR itself):
- `grep -n "/code-review" CLAUDE.md docs/superpowers/templates/pr-gate-checklist.md` should now return 8 hits (was 7) in `CLAUDE.md` and 1 (was 0) in the checklist.
- Render CLAUDE.md §12.2 in IDE / GitHub preview: the new paragraph appears as a blockquote immediately below the module table, before `### 12.3`. No table layout regression.
- Render `pr-gate-checklist.md` `## Review` in preview: the new sub-check appears as a 4th `- [ ]` item, formatting matches siblings.
- Re-read §12.4 R4 (CLAUDE.md:518-524) verbatim: confirm unchanged byte-for-byte vs `origin/main`.

## §5 Risks & Stop Conditions

**Risk** — Reader confusion from R4 trailing advisory + new §12.2 advisory.
Mitigated in §3.1 wording ("不改 §12.4 R4 中...的既有半自动核对要求") and by the
§12.2 paragraph generalizing the path-family list rather than restating R4's narrower one.

**Risk** — Path-family list drift. `Catalog/Typed/Runtime/` and
`MetBench_Analyzers/` are named on first impression; if either path is renamed or
the predicate / Roslyn-diagnostic surface moves, this paragraph goes stale.
Mitigation: §12.2 paragraph is short and the file already has §12 advisory texts
that are revised on chain ends; no separate stale-guard is justified for a
non-gate.

**Stop condition** — If `grep` finds that §12.2's module F row **already has** a
sub-bullet enumerating path families (i.e., a previous PR already shipped the
CLAUDE.md half), P6 degrades to checklist-only and we report-and-stop before
writing the §12.2 paragraph. Verified at writing-plan time: no such sub-bullet
exists; the §12.2 module F row is a single-line table cell only.

## §6 Execution Steps (≤ 6)

1. `grep -n "/code-review" CLAUDE.md docs/superpowers/templates/pr-gate-checklist.md` to reconfirm the 7+0 baseline and verify the §12.2 module F row still has no sub-bullet (P6 stop condition).
2. Decide between (a) / (b) / (c) for §3.3 — **already decided: (b)**.
3. Edit `CLAUDE.md`: insert the 4-line blockquote paragraph from §3.1 immediately after the §12.2 table (after line 477), before `### 12.3 强约束`.
4. Edit `docs/superpowers/templates/pr-gate-checklist.md`: insert the 1-line `- [ ]` sub-check from §3.2 in `## Review` section, after the existing 3 sub-checks, before `## Merge`.
5. Register this plan as `Active scoped — 单 PR 改造` in `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`, summary "v2 charter P6: 模块 F 作者侧 `/code-review` advisory 操作化（CLAUDE.md §12.2 加路径族建议 + PR Gate Checklist Review 节加 sub-check），docs-only".
6. `git add` the three changed paths, commit (no ledger change, no decision record needed — P6 is in-scope of v2 charter §6, and the charter itself is the decision record), `git push -u origin claude/p6-code-review-advisory`. **Do not** open the PR in this scoped plan PR; that is the next phase. **Do not** edit `docs/status/current.md`.

## §7 Acceptance Checklist

- [ ] `CLAUDE.md` §12.2 contains a new blockquote paragraph after the module table and before §12.3, listing ≥ 4 path families for author-side `/code-review high`, and explicitly stating it does not modify §12.4 R4.
- [ ] `CLAUDE.md` §12.4 R1-R4 text is byte-for-byte unchanged vs `origin/main` (verified by `git diff origin/main -- CLAUDE.md` showing zero hunks inside §12.4).
- [ ] `docs/superpowers/templates/pr-gate-checklist.md` `## Review` section contains a 4th `- [ ]` sub-check that names module F and the same 4 path families, marked non-blocking.
- [ ] No new file is created outside of this plan doc itself; no workflow YAML, no fact, no source file is touched.
- [ ] `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` has a new row pointing at this plan, status `Active scoped — 单 PR 改造`, expiry `Expires on PR merge`.
- [ ] Plan file is ≤ 200 lines.
