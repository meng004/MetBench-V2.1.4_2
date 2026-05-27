# T2 SystemMT Visualization + T3 Gap-Fill — Post-Merge Holistic Review

> **Date**: 2026-05-27
> **Chain**: 6-phase T2/T3 sequenced delivery (`docs/superpowers/plans/2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md`)
> **Status**: Closed (cleanup PRs #195 + #199 merged; governance rules codified by PR #203)
> **Review session**: same-session post-merge review (implementer == reviewer) — **acknowledged process gap per `CLAUDE.md §12.4 R2`** (R2 was not yet in force; codified retrospectively by this very chain's findings)
> **Worked example for**: `docs/superpowers/templates/chain-end-review-checklist.md` (Output section)

---

## §1 Chain phases reviewed

| Phase | PR | Merge SHA | Delivery |
|---|---|---|---|
| 1 | #184 | `7526407` | `ChartFigure` DTO + 3 projectors (BinaryRunPoint / PhaseConvergence / HistoricalTrend) |
| 2 | #185 | `0184f63` | `SkiaChartRenderer` (offscreen PNG, SkiaSharp 3.116.1 + LiveCharts.SkiaSharpView 2.0.0-rc5.4 + Linux native asset) |
| 3a | #186 | `9e898f1` | `PdfSystemMtResultReportRenderer` (iTextSharp.LGPLv2.Core 3.7.1) |
| 3b | #188 | `b76f4d7` | `WordSystemMtResultReportRenderer` (DocumentFormat.OpenXml 3.3.0) |
| 3c | #190 | `0fd1b93` | `ExcelSystemMtResultReportRenderer` (ClosedXML 0.104.2) |
| 4 | #191 | `4c07c1f` | `MetaPatternMatrixAuditor` + `(equation × meta-pattern)` coverage spec doc |
| 5 | #192 | `c14ffd9` | `subchannel-friction-invariance` MR (substitution for spec-doc top-1 `burgers-timestep-convergence` after empirical validation showed Lax-Friedrichs dissipation grew under timestep refinement) |
| 6 (LEDGER) | #193 | `d2e1c5d` | status ledger + active plan index + audit spec doc refresh |

`origin/main` at chain close: `d2e1c5d`. Net delivery: +89 facts, +1 MR (32 → 33), no new SUT directory, no Method MT / WPF touch, no `SemanticCatalogBoundaryTests` regression.

---

## §2 Review findings (11 total)

### Category A — single-PR diff visible (AI review CAN catch with right prompt)

| # | Code | File | What | Severity | Cleanup PR |
|---|---|---|---|---|---|
| 1 | B1 / M1 | `ExcelSystemMtResultReportRenderer.cs` | `_ = context ?? new ReportContext()` discarded; Excel reports had no title / generated-at / totals. Public method XML doc claimed ReportContext support but the body silently dropped it | **Medium (user-visible)** | #195 |
| 2 | C1 | `PdfSystemMtResultReportRenderer.cs` | `image.Width > maxWidthPt` compared raw pixel value to a constant named in points; default DPI hid the bug | Low | #199 |
| 3 | C2 / M3 | `ExcelSystemMtResultReportRenderer.cs` | Charts sheet `const int rowsPerChart = 25;` would overlap with taller `ChartRenderOptions.Height` | Low | #195 |
| 4 | M2 | `WordSystemMtResultReportRenderer.cs` | EMU constants hardcoded `4_572_000 × 3_048_000`; non-3:2 chart options would distort | Low | #195 |
| 5 | T3 | `LauncherEndToEndSubchannelFrictionInvarianceTests.cs` | `Assert.Equal(SourceValue, FollowUpValue)` strict equality with no docstring of analytical intent — would be misread as "needs ApproxEqual relaxation" by future maintainer hitting FP wobble | Low (intent documentation) | #199 |

### Category B — cross-PR / cross-file / retrospective (AI review CANNOT catch at PR-time)

| # | Code | What | Why AI review missed it | Cleanup PR |
|---|---|---|---|---|
| 6 | L1 | `MrCatalogEntry.FromBlueprint` dropped the new `MetaPattern` init-property added by Phase 4. `HardcodedMrCatalogProvider` (which routes through FromBlueprint) returned entries with empty MetaPattern; the auditor would bucket all rows as Unclassified if fed via that provider. `CatalogParityTests` did not assert MetaPattern, so divergence was silent | Phase 4's diff did not touch `FromBlueprint`; PR-time reviewer would not read an unchanged file | #199 |
| 7 | M5 | `PhaseConvergenceProjector` accepted ≥ 2 phases as valid; `ErrorMonotonicPredicateValidator` (in a different file) required ≥ 3 (OrderedRoles ≥ 2 + ReferenceRole). Same input would render via projector but fail in the verifier | Validators in different files, different PRs, semantically must agree but no shared test asserted the agreement | #195 |
| 8 | T1 | No fact guarded `MetaPatternMatrixAuditor` correctness when called via `HardcodedMrCatalogProvider` — only Manifest path tested. The asymmetry L1 surfaces was therefore structurally invisible | "Test gap" is negative-space; reviewers don't see what's missing | #199 |
| 9 | T2 | No fact asserted Excel `ReportContext.Title` appears in output. B1 went unnoticed because the test surface didn't pin the contract | Same negative-space class as T1 | #195 |
| 10 | D1 | Phase 4's spec doc §3 marked candidate A4 (`subchannel-power-rebalance-invariance`, enthalpy balance) as CLOSED after PR #192, but PR #192 actually shipped `subchannel-friction-invariance` (friction decoupling) — same `(navier-stokes, Inv)` cell, different physics. The CLOSED label was wrong | Phase 5's runtime divergence from Phase 4's spec was acknowledged in PR #192 commit body but the spec doc itself was not re-touched in the same PR | #199 |
| 11 | D2 | Phase 4 spec §3 listed `subchannel-mesh-convergence` (A5) as a Tier-A candidate without checking whether `subchannel_1d.py` (a 0D analytical lumped model) actually exposes a mesh to refine. A1 `burgers-timestep-convergence` had the same blind spot — Lax-Friedrichs schemes increase dissipation under timestep refinement | Spec-doc time (Phase 4) didn't include SUT feasibility check; only Phase 5 empirical validation surfaced the failure mode | #199 |

---

## §3 Category split at a glance

| Category | Count | What single-PR LLM review catches | What it doesn't |
|---|---|---|---|
| A — single-PR visible | 5 | ✅ no-op coalescing, magic numbers, pixel vs point, strict-equality without docstring | — |
| B — cross-PR / cross-file | 4 (L1 / M5 / T1 / T2) | — | ❌ data flow across files / PRs; "test gap" negative-space |
| B — retrospective / temporal | 2 (D1 / D2) | — | ❌ a spec doc that is internally consistent at PR-time becomes inconsistent after Phase N+1 diverges |

**Implication**: even with both Codex and Claude AI reviewers, only ~5/11 ≈ 45% of findings would surface at PR-time. The remaining 6/11 are structurally only visible **after** the chain completes (B class).

---

## §4 Cleanup PR sequence

| PR | Layer | Findings closed | Merged |
|---|---|---|---|
| **#195** "M1+M2+M3+M5" | code | B1 (M1 Excel context), C2 (M3 stride), M2 (Word EMU), M5 (PhaseConvergence ≥3), T2 (Excel title fact) | `f10cfc2` |
| **#199** "L1+C1+T3+D1+D2" | code + tests + docs | L1 (FromBlueprint MetaPattern), C1 (PDF px↔pt), T3 (strict-eq intent), D1 (spec A4 wording), D2 (spec SUT-precondition habit), T1 (auditor parity fact) | `227959f` |
| **#203** "governance" | rules + workflows | Codify §12.4 R1-R4 + §12.5 guard-test compilation + chain-end checklist + AI review prompt extensions | `8e4bf19` |

---

## §5 Rules-of-thumb extracted (each → CLAUDE.md §12.4 / §12.5)

1. **`_ = ctx ??` is a textbook silent-discard bug** (B1) → R4 prompt explicitly flags this pattern.
2. **A field added on one projection side must be added on all sides** (L1) → R1 parity test mandatory.
3. **Two validators that must agree need a shared invariant test, not just per-validator tests** (M5) → R1 multi-provider matrix equivalence.
4. **Magic numbers in rendering paths should derive from ChartRenderOptions** (C1 / C2 / M2) → AI semantic review explicitly checks "could this be parameterized from a settable input".
5. **Strict equality assertions need an intent paragraph** (T3) → R4 contract↔fact pair; tolerance bands require documented physical / FD reason.
6. **Spec doc retrospective on Phase divergence belongs in the diverging PR, not just commit body** (D1) → R3 explicit spec doc re-touch requirement.
7. **Candidate-feasibility check belongs in the spec-doc phase** (D2) → spec doc §3 SUT-precondition habit note (already applied to the audit spec).
8. **"No fact" is invisible to AI reviewers** (T1 / T2) → R4 prompt grep for XML doc contract claims and check test surface.
9. **Chain-end fresh-session review is mandatory** (this very review session) → R2 chain-end review checklist.
10. **First action on any new post-merge finding is "can this become a Layer-4 guard test"** (overall meta-rule) → §12.5 compilation table.

---

## §6 Process gap acknowledged

The chain was marked **Controlled** in `docs/status/current.md` §3 at the close of Phase 6 (PR #193, `d2e1c5d`). Two cleanup PRs (#195, #199) and one governance PR (#203) landed AFTER that mark to address findings the implementer (same session) caught only because the user asked for a holistic review.

Per the new `CLAUDE.md §12.4 R2`, future chains:
- MUST NOT mark Controlled until a fresh-session post-merge review has produced a `<date>-<chain-name>-post-merge-review.md` (like this one) AND its cleanup PR(s) have merged
- Chain-end review session MUST be a different agent context from the implementation chain

This document is created **retroactively** as the worked example so future chain-end reviewers know what the output looks like. The retroactive nature is itself an artifact of R2 not existing at chain time.

---

## §7 References

- Implementation chain plan: [`docs/superpowers/plans/2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md`](../plans/2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md)
- Phase 4 coverage audit spec: [`docs/superpowers/specs/2026-05-27-meta-pattern-coverage-audit.md`](2026-05-27-meta-pattern-coverage-audit.md)
- Governance rules: `CLAUDE.md` §12.4 (cross-PR consistency) + §12.5 (guard-test compilation)
- Chain-end review checklist template: [`docs/superpowers/templates/chain-end-review-checklist.md`](../templates/chain-end-review-checklist.md)
- Soft-review prompt that now includes R1-R4 checks: [`.github/workflows/pr-soft-review.yml`](../../../.github/workflows/pr-soft-review.yml)
