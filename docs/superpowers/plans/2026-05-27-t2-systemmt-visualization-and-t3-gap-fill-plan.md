# T2 SystemMT Visualization + T3 Gap-Fill Sequenced Plan (Linux-only)

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan PR-by-PR. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the T2 visualization gap (`SystemMT` results currently exit as numbers + HTML/Markdown only, no charts / no 4-end report) and use the resulting visualization stack to immediately validate one T3 meta-pattern gap fill. All work is **cloud-side Linux-only**: zero WPF, zero new SUT venv, every fact and renderer must run in `ubuntu-24.04` GitHub Actions CI.

**Status:** active scoped plan — Phase 1 starts immediately after merge of this plan PR.

**Tech Stack:** .NET 8, xUnit, SkiaSharp 3.116.1 (cross-platform — already in `MetBench_BLL.csproj`), iTextSharp.LGPLv2.Core 3.7.1, DocumentFormat.OpenXml 3.3.0, ClosedXML 0.104.2.

---

## Why this exists

Stage 8 is largely Controlled. T0 / T1 / T3 representative-PDE-class are closed; Bol-Alg-01 (PR #181) and Bol-Alg-02 (PR #170) shipped first multi-phase / variance-ratio MR consumers. But the **user-facing output of a 32-MR run is still a numeric table + HTML/Markdown report**: no convergence curves, no PNG embeds, no PDF / Word / Excel artifacts. CLAUDE.md §2.2 T2 explicitly scopes "图表展示 + 4 端 (PDF / Word / Excel / HTML) 报告生成", and the SystemMT-side projection of this scope is presently incomplete (HTML + Markdown only).

CLAUDE.md §3 confirms the split: `MetBench_BLL/` is `net8.0` with `SkiaSharp` + `LiveChartsCore.SkiaSharpView` + `ClosedXML` + `DocumentFormat.OpenXml` + `iTextSharp.LGPLv2.Core` already in place; WPF chart plotters live in `MetBench_Client/Services/Plotting/`. The data-projection + offscreen-PNG + 4-end-report layers are all Linux-portable; only chart-binding into XAML pages requires the VM.

This plan therefore stays inside the cross-platform projects.

---

## Dependency graph

```
Phase 1 (PR-T2-1) ChartData DTO + projectors
            │
            ▼
Phase 2 (PR-T2-2) SkiaSharp offscreen PNG renderer
            │
            ├──────────────────┬──────────────────┐
            ▼                  ▼                  ▼
Phase 3a (PR-T2-3a)  Phase 3b (PR-T2-3b)  Phase 3c (PR-T2-3c)
PDF renderer         Word renderer         Excel renderer
(iTextSharp)         (OpenXml)             (ClosedXML)
            │                  │                  │
            └──────────────────┼──────────────────┘
                               ▼
            Phase 4 (PR-T3-7) (equation × meta-pattern) audit
                               │
                               ▼
            Phase 5 (PR-T3-8) first gap-fill MR
                               │
                               ▼
        Phase 6 (PR-LEDGER) post-stage status ledger refresh
```

Phase 3a/3b/3c are pairwise independent and can be staggered in any order, but each individually depends on Phase 1 + Phase 2.

---

## Phase 1 — PR-T2-1 SystemMT ChartData projection layer

### Preconditions

- `origin/main` HEAD ≥ `73dcd1c` (current; PR #182 already merged).
- `MetBench_BLL.Core` builds clean on Linux (verified).
- `PipelineOutcome.PhaseMetrics : IReadOnlyDictionary<string, IReadOnlyDictionary<string,double>>?` exists (PR-Bol-2A, in place at `PipelineOutcome.cs:52`).
- `SystemMtResultRecord` exposes `SourceValue / FollowUpValue / SourceMetrics / FollowUpMetrics` (verified at `SystemMtResultRecord.cs:35–57`).

### Core steps

1. Create `MetBench_BLL.Core/SystemMT/Reporting/Charts/` namespace.
2. Add immutable DTOs:
   - `ChartPoint(double X, double Y, string? Label = null)`
   - `ChartSeries(string Name, IReadOnlyList<ChartPoint> Points, string? Unit = null)`
   - `ChartFigure(string Title, string XAxisLabel, string YAxisLabel, IReadOnlyList<ChartSeries> SeriesList, ChartFigureKind Kind)`
   - `enum ChartFigureKind { BinaryScatter, PhaseLine, HistoricalTrend }`
3. Define `ISystemMtChartDataProjector` with three projector implementations (different input shapes — projectors do NOT share a single method signature):
   - `BinaryRunPointProjector.Project(SystemMtResultRecord record) → ChartFigure` — two-point scatter (source vs follow-up).
   - `PhaseConvergenceProjector.Project(IReadOnlyDictionary<string, IReadOnlyDictionary<string,double>> phaseMetrics, string mrId, string metric) → ChartFigure` — line chart over ordered phase roles. Input shape matches `PipelineOutcome.PhaseMetrics` exactly so the launcher can pass it through unchanged.
   - `HistoricalTrendProjector.ProjectAsync(string mrId, int lookbackRuns, ISystemMtResultRepository repo, CancellationToken ct) → Task<ChartFigure>` — N-point line over time.
4. Each projector lives in its own file; all public types are `sealed`.
5. Add `MetBench_SystemMT.Tests/SystemMT/Reporting/Charts/` test folder with three test files.
6. Numeric formatting: all axis labels use `CultureInfo.InvariantCulture` for byte-stability across host locales (existing PR #128 markdown precedent).

### Acceptance criteria

- [ ] `dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj` returns 0 errors on Linux.
- [ ] ≥ 25 xUnit facts green, distribution roughly: 8 (BinaryRunPointProjector) + 9 (PhaseConvergenceProjector) + 8 (HistoricalTrendProjector). Coverage must include: null / empty input handling, NaN / ±∞ values gracefully labelled, single-phase (Count=1 → throw or single-point depending on policy — pin the chosen policy with a fact), N-phase with N≥4, missing metric key, empty repo (HistoricalTrend), repo returns < lookbackRuns rows.
- [ ] Existing 1372 facts unchanged (zero regression).
- [ ] `HtmlSystemMtResultReportRenderer` output byte-identical (additive change only).
- [ ] `SemanticCatalogBoundaryTests` 3/3 green; new code does not reference `AssertionTypeCodes.` substring outside allowed dirs.
- [ ] **Zero new NuGet dependencies** in this phase — pure stdlib + existing BLL.Core references.

---

## Phase 2 — PR-T2-2 SkiaSharp offscreen PNG renderer

### Preconditions

- Phase 1 merged; `ChartFigure` DTO available on `origin/main`.
- `MetBench_BLL.csproj` references `SkiaSharp 3.116.1` + `LiveChartsCore.SkiaSharpView 2.0.0-rc5.4` (verified).
- `MetBench_BLL` builds clean on Linux (ubuntu-24.04 CI baseline).

### Core steps

1. Add to `MetBench_BLL.Core/SystemMT/Reporting/Charts/` (interface stays in Core to keep MetBench_BLL → MetBench_BLL.Core dependency direction):
   - `ISystemMtChartRenderer.RenderPng(ChartFigure, ChartRenderOptions) → byte[]`
   - `ChartRenderOptions(int Width = 1200, int Height = 800, int Dpi = 150, ChartTheme Theme = ChartTheme.Light)` — Dpi defaults to 150 (R4 mitigation — print 300dpi available but not default to keep embedded file sizes < 5MB).
   - `enum ChartTheme { Light, Dark }`
2. Add to `MetBench_BLL/Reporting/SystemMt/Charts/Rendering/` (concrete renderer — depends on SkiaSharp):
   - `SkiaChartRenderer` implementing the Core interface
3. Internal dispatch per figure kind (`switch figure.Kind`):
   - `BinaryScatter` — direct SkCanvas: draw axis + 2 circles + labels; no LiveCharts dependency for this trivial case (faster path).
   - `PhaseLine` + `HistoricalTrend` — `LiveChartsCore.SkiaSharpView.LineSeries` rendered to off-screen `SKSurface` → `SKImage.Encode(SKEncodedImageFormat.Png, 100)`.
4. Public method must be **deterministic**: same `ChartFigure` + same `ChartRenderOptions` → byte-identical `byte[]`. Pin this with a fact.
5. Add `MetBench_SystemMT.Tests/SystemMT/Reporting/Charts/Rendering/SkiaChartRendererTests.cs`.

### Acceptance criteria

- [ ] `dotnet build MetBench_BLL/MetBench_BLL.csproj` returns 0 errors on Linux.
- [ ] ≥ 12 facts green, covering:
  - Output `byte[].Length` ∈ [5_000, 500_000] for default options.
  - PNG magic (`89 50 4E 47 0D 0A 1A 0A`) at offset 0.
  - IHDR chunk decodes to `Width / Height` matching `ChartRenderOptions`.
  - Empty `SeriesList` → still returns a valid PNG (axes-only frame, no exception).
  - Two consecutive renders with the same input → byte-identical output (determinism).
- [ ] **No byte-snapshot baseline file** — SkiaSharp cross-distro rasterizer drift would make those flaky. Structural assertions only.
- [ ] Phase 1 (1372 + 25 = 1397) facts unchanged.
- [ ] No new SUT directories / no Python venv reference.

---

## Phase 3a — PR-T2-3a PDF SystemMT report renderer

### Preconditions

- Phase 1 + Phase 2 merged.
- `iTextSharp.LGPLv2.Core 3.7.1` in `MetBench_BLL.csproj` (verified).
- HTML / Markdown renderer evidence-aware contract stable (PR #126 + PR #128, already merged).

### Core steps

1. Add interface `IPdfSystemMtResultReportRenderer` to `MetBench_BLL.Core/SystemMT/Reporting/`:
   `byte[] Render(IReadOnlyList<SystemMtResultRecord>, IReadOnlyDictionary<Guid, ExecutionEvidence>?, ReportContext?)`.
2. Implement `PdfSystemMtResultReportRenderer` in `MetBench_BLL/Reporting/SystemMt/`. iTextSharp APIs (`Document` / `PdfWriter` / `Paragraph` / `Image.GetInstance(byte[])`).
3. Content section order (parity with HTML/Markdown):
   - Title page with `ReportContext.Title / GeneratedAt`.
   - Per-record block: `MrId`, `Passed`, `SourceValue` / `FollowUpValue`, `ValueName`, `FailureReason`, **TypedVerification block** if evidence present (Spec id / kind / predicate / status / diagnostic / property predicates), **chart image** built via Phase 1 + Phase 2.
4. Chart image selection per record:
   - `record.AssertionName == "ErrorMonotonic"` → not available in record; for the first-pass we use `BinaryScatter` for all records since `SystemMtResultRecord` only carries 2-point data. Phase 5 (gap-fill MR landing) will not change PDF; phase-line embedding into PDF is a documented follow-up (deferred).
   - All other records → `BinaryScatter` via `BinaryRunPointProjector`.
5. A4 page size, 14pt headings, 10pt body, invariant-culture numeric formatting.
6. Add `MetBench_SystemMT.Tests/SystemMT/Reporting/PdfSystemMtResultReportRendererTests.cs`.

### Acceptance criteria

- [ ] `dotnet build` 0 errors on Linux.
- [ ] ≥ 12 facts:
  - Single-record happy path → bytes > 10_000 and `< 5_000_000`.
  - Multi-record (5 records) → byte-length scales linearly within tolerance.
  - With evidence / without evidence → both succeed; evidence-present renders the typed verification block.
  - Reverse-parse generated PDF via iTextSharp `PdfReader` → page count ≥ 1, image count == record count, key text `MrId` substring found.
  - Empty input → returns a valid 1-page "no records" PDF (do not throw).
  - Deterministic: two renders with same input + same `ReportContext` are byte-identical (or only differ in PDF /CreationDate metadata; if iTextSharp inserts /CreationDate non-deterministically, pin a stripped-metadata equality test).
- [ ] No regression on previous phases.

---

## Phase 3b — PR-T2-3b Word SystemMT report renderer

### Preconditions

- Phase 1 + Phase 2 merged.
- `DocumentFormat.OpenXml 3.3.0` in `MetBench_BLL.csproj` (verified).

### Core steps

1. Add interface `IWordSystemMtResultReportRenderer` to `MetBench_BLL.Core/SystemMT/Reporting/`.
2. Implement `WordSystemMtResultReportRenderer` in `MetBench_BLL/Reporting/SystemMt/`. OpenXml APIs (`WordprocessingDocument.Create` → `MainDocumentPart` → `Body` → `Paragraph` / `Run` / `Drawing`).
3. Embed Phase 2 PNGs via `MainDocumentPart.AddImagePart(ImagePartType.Png)` + a `DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline` wrapper.
4. Section parity with PDF.
5. Add corresponding test file.

### Acceptance criteria

- [ ] `dotnet build` 0 errors on Linux.
- [ ] ≥ 12 facts:
  - Reverse-parse generated `.docx` (which is a zip) via `WordprocessingDocument.Open` → `Body.ChildElements.Count > 0`, `/word/media/` contains ≥ record-count PNGs.
  - `[Content_Types].xml` declares `image/png`.
  - Empty input → 1-paragraph "no records" doc, does not throw.
  - Deterministic when `ReportContext.GeneratedAt` is fixed.
- [ ] No regression.

---

## Phase 3c — PR-T2-3c Excel SystemMT report renderer

### Preconditions

- Phase 1 + Phase 2 merged.
- `ClosedXML 0.104.2` in `MetBench_BLL.csproj` (verified).

### Core steps

1. Add interface `IExcelSystemMtResultReportRenderer` to `MetBench_BLL.Core/SystemMT/Reporting/`.
2. Implement `ExcelSystemMtResultReportRenderer` in `MetBench_BLL/Reporting/SystemMt/`. ClosedXML APIs (`XLWorkbook` → `Worksheet` → `Cell` / `AddPicture`).
3. Workbook layout:
   - `Summary` sheet: header row + one row per record (MrId / AssertionName / ValueName / SourceValue / FollowUpValue / Passed / FailureReason / RunAt).
   - `TypedVerification` sheet (only if evidence present): one row per evidence entry with the typed-verification projection.
   - `Charts` sheet: one PNG per record anchored to a cell.
4. Numeric cells use proper number type (not string), so downstream Excel users can sort / pivot.
5. Add corresponding test file.

### Acceptance criteria

- [ ] `dotnet build` 0 errors on Linux.
- [ ] ≥ 12 facts:
  - Reverse-parse generated `.xlsx` via `XLWorkbook.Load(stream)` → worksheet names match expectation, `Summary` cell `A2` is the first MrId.
  - `Charts` sheet `Pictures.Count == record count`.
  - Cells of `SourceValue` / `FollowUpValue` columns are numeric (not text).
  - Empty input → workbook with a `Summary` sheet containing only the header row.
- [ ] No regression.

---

## Phase 4 — PR-T3-7 (equation × meta-pattern) coverage audit

### Preconditions

- All 32 current MRs registered via `IMrCatalogProvider` (verified).
- Meta-pattern set known and stable: `Mono` / `Inv` / `Conv` (per CLAUDE.md §2.2 T4 + manifest `meta_pattern` field).
- No code change required from Phase 1–3c; this is a parallel audit track (could in principle ship earlier, but sequenced after Phase 3c so the audit lands when all visualization is already shippable for the next MR).

### Core steps

1. Add `MetBench_BLL.Core/SystemMT/Coverage/MetaPatternMatrixAuditor.cs`:
   - `record CoverageCell(string EquationKey, string MetaPattern, IReadOnlyList<string> MrIds)`.
   - `record CoverageMatrix(IReadOnlyList<CoverageCell> Cells, IReadOnlyList<(string EquationKey, string MetaPattern)> Gaps)`.
   - `static CoverageMatrix Audit(IMrCatalogProvider provider)`.
2. For each MR, derive `(EquationKey, MetaPattern)` from the manifest (`SystemMtCatalogDocument` already carries `equation` + `meta_pattern` per MR).
3. Compute Cartesian product over `{distinct EquationKeys} × {Mono, Inv, Conv}` minus filled cells → `Gaps`.
4. Add test file `MetBench_SystemMT.Tests/SystemMT/Coverage/MetaPatternMatrixAuditorTests.cs` (≥ 8 facts).
5. Add spec document `docs/superpowers/specs/2026-05-27-meta-pattern-coverage-audit.md`:
   - Snapshot the current matrix (table form).
   - List Gaps ranked by feasibility (no new SUT > existing SUT new MR; SUT with existing venv > SUT needing new venv).
   - Recommend the top-1 candidate for Phase 5.

### Acceptance criteria

- [ ] `dotnet build` 0 errors on Linux.
- [ ] ≥ 8 facts:
  - Matrix total cell count == `(distinct EquationKey count) × 3`.
  - Sum over `Cells[i].MrIds.Count` == 32 (every MR classified exactly once).
  - No MR appears in two cells.
  - `Gaps` set-disjoint from filled cells.
- [ ] Spec document published with concrete gap candidates.
- [ ] No regression.

---

## Phase 5 — PR-T3-8 first gap-fill MR

### Preconditions

- Phase 4 merged; spec document has named top-1 candidate.
- Candidate falls on an existing SUT directory and requires no new Python venv (so Linux CI can run an end-to-end fact).
- Candidate's physical claim is verifiable by a deterministic numeric check (no LLM / no human-in-the-loop verification).

### Core steps (final shape pinned by Phase 4 candidate selection)

1. (If needed) add Python input adapter under existing `SUT/<sut>/`.
2. `LegacyCatalogFactory.cs` — append one `MrBlueprint`.
3. `SystemMtMetadataCatalog.cs` — append one `MrMetadata`.
4. `SUT/<sut>/catalog.json` — append manifest entry with all required fields.
5. Pinned-count bump 32 → 33 across the same six test files PR-Bol-2B touched + one production comment (mirror PR-N2 + PR-Bol-2B precedent).
6. Update parity-by-SUT test for the chosen SUT, mirroring how PR-N2 / PR-Bol-2B updated `OpenMc/OpenMocCatalogParityTests` to Mono/Conv classification per ID.
7. Update positional pin in `SystemMtLauncherTests.ListAvailableAsync_returns_known_scenarios_in_id_order`.
8. Add `LauncherEndToEnd<Name>Tests.cs` with ≥ 2 facts (use `[SkippableFact]` only if the chosen SUT requires a venv; otherwise plain `[Fact]`).

### Acceptance criteria

- [ ] Full suite green: `~1455 + 2 = ~1457 / 0 / 16` facts (assuming Phase 4 added 8 and Phase 5 adds 2 end-to-end + a few pinned-count updates).
- [ ] `git grep -nE 'Assert\.Equal\(32,'` returns zero rows.
- [ ] `MetaPatternMatrixAuditor.Audit(...)` re-run after the merge returns one fewer `Gap`.
- [ ] Phase 1–3c visualization stack handles the new MR end-to-end: render PNG via `SkiaChartRenderer`, embed in PDF / Word / Excel reports (one ad-hoc test fact per renderer demonstrating the new MR is visualizable).

---

## Phase 6 — PR-LEDGER post-stage status ledger refresh

### Preconditions

- Phases 1–5 all merged to `main`.

### Core steps

1. `docs/status/current.md`:
   - Status date header bump.
   - `Latest code-test baseline commit` advance to Phase 5 merge SHA.
   - Result narrative refresh (estimated `~1457 / 0 / 16` composition, with breakdown per phase).
   - Inventory advance 32 → 33 MRs.
   - Add Stage-8 row "T2 SystemMT visualization 4-end" → Controlled.
   - Execution Order step 7 reorder per new state.
2. `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`:
   - Mark this entire plan row Completed with all five merge SHAs.
   - Cross-link to Phase 4 spec document.
3. `docs/superpowers/specs/2026-05-27-meta-pattern-coverage-audit.md` (re-touch if matrix changed).

### Acceptance criteria

- [ ] Two docs files updated, no code change.
- [ ] Hard `test` gate green (docs-only); soft `review` skipped by `paths-ignore` (PR #175).
- [ ] All five preceding PR SHAs referenced.

---

## Global end-state acceptance

After Phases 1 + 2 + 3a + 3b + 3c + 4 + 5 + 6 all merged:

- [ ] `dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj && dotnet build MetBench_BLL/MetBench_BLL.csproj` returns 0 errors on Linux.
- [ ] Full test suite: ~1457 passed / 0 failed / 16 skipped on Linux (pass total may vary ±5 with overlapping fact estimates; what matters is 0 failed + no environmental skips that were not already skips on baseline `73dcd1c`).
- [ ] Cross-platform invariant: zero `System.Windows` / WPF / PresentationFramework / `MetBench_Client/*` reference enters `MetBench_BLL.Core` or `MetBench_BLL` in any of the 6+ PRs of this plan.
- [ ] Architecture guards: `SemanticCatalogBoundaryTests` 3/3, no new `AssertionTypeCodes.` substring outside allowed dirs.
- [ ] 32 MRs → 33 MRs, 16 SUTs unchanged.
- [ ] Inventory of the visualization stack — `(ChartFigure DTO, SkiaChartRenderer, PdfSystemMtResultReportRenderer, WordSystemMtResultReportRenderer, ExcelSystemMtResultReportRenderer, MetaPatternMatrixAuditor)` — all reachable from Linux CI, all exercised by ≥ 1 fact.

---

## Risks called out

- **R1 — SkiaSharp cross-platform rasterizer drift.** Mitigated by structural-only assertions in Phase 2 (no byte-snapshot baseline file).
- **R2 — iTextSharp LGPLv2 fork divergence from upstream 5.x API.** Mitigated by a 5-line smoke fact at the head of Phase 3a verifying the small subset of APIs we depend on (Document / PdfWriter / Image.GetInstance).
- **R3 — Phase 4 audit shows the top-1 gap requires a new SUT.** Mitigated by deferring Phase 5 into a separate driver decision under `docs/superpowers/specs/2026-05-26-t3-coverage-assessment-and-next-sut-decision.md` §5.x; Phase 6 then ships ledger refresh covering Phases 1–4 only and Phase 5 splits into its own scoped plan.
- **R4 — PNG file size in 4-end reports exceeds 10 MB on multi-record runs.** Mitigated by `ChartRenderOptions.Dpi` defaulting to 150 (not 300); 300dpi remains available for callers that need print quality.
- **R5 — Determinism on PDF `/CreationDate`.** iTextSharp may inject a non-deterministic timestamp; Phase 3a's determinism fact strips PDF metadata before byte-comparison if needed.

---

## Explicit non-goals (this plan)

- No WPF / XAML / `MetBench_Client/` change.
- No new SUT directory (Phase 5 reuses an existing SUT).
- No new Python venv requirement.
- No T4 / T5 / T6 work (per user scope restriction).
- No `IMRDiscoverer` implementation (T4 separately scoped).
- No mutation-testing matrix (T6 separately scoped).
- No T3 ML/PINN driver activation (would need its own §5.x decision).
- No revisit of `MetBench_BLL/` legacy Method MT generators — they stay where they are; new SystemMT-side renderers live alongside, do not replace.

---

## Workflow note for executors

This plan is intended to be executed PR-by-PR, sequentially:
1. Plan PR (this file) merges first.
2. Phase 1 PR opens, CI passes, merges.
3. Phase 2 PR opens, CI passes, merges.
4. Phase 3a/3b/3c (any order, can stagger), each opens, CI passes, merges.
5. Phase 4 PR opens, CI passes, merges.
6. Phase 5 PR opens, CI passes, merges.
7. Phase 6 PR opens (docs-only), CI passes, merges.

Total: 8 PRs (this plan PR + 7 implementation PRs). Each implementation PR carries the full PR Gate Checklist (7 sections) per project §12.
