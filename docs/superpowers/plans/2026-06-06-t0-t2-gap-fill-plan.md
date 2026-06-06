# T0-T2 Gap-Fill Implementation Plan

> **Status:** In progress (updated 2026-06-06). Scope confirmed in conversation:
> P0 (three true gaps) + P1 (cloud enhancements) + P3 (VM/quality debt).
> **P2 deliberate boundaries (result/evidence import, asset live-promotion) are
> explicitly OUT of scope** and remain as-is until a separate trust-model plan.
>
> **Progress:** P0 **A1** (#308 four-end async export), **A2** (#309 recorder
> async / no sync-over-async), **A3** (#310 ExportReport handler) and **C4**
> (#311 BLL.Core TreatWarningsAsErrors) are merged to `origin/main`. A fresh
> §12.4 R2 chain-end holistic review of #308–#311 found **no Critical/Important**
> issues; its 2 minor stale-doc findings (ExportReport enum doc, RecordedExecution
> cref) are fixed in the closure PR. **Remaining:** P1 **C1** (batch/range export —
> needs a new job kind + R1 parity on request/record/status + WPF wiring) and
> **C2** (richer SampleTraces — a `SystemMtPipeline` per-variable-capture feature,
> tracked in the status ledger as 未闭环) are deferred as their own focused PRs.
> P3 **C3** (UI-only MR CRUD) plus the A1/A3 WPF renderer/handler wiring are
> captured in `docs/superpowers/vm-prompts/2026-06-06-t0-t2-gap-fill-a1-a3-wpf-wiring-vm-prompt.md`
> and await a VM run. This row is **not Controlled** until C1/C2 land (or are
> explicitly descoped) and the C3/A1/A3 VM evidence is captured.
>
> **REQUIRED SUB-SKILL:** use superpowers:subagent-driven-development or
> superpowers:executing-plans, task-by-task, TDD-first.

**Goal:** Close the verified residual gaps in T0 (core System-MT flow), T1
(direct support), and T2 (visualization/reporting) so the "all user-visible
resources are async and exportable" objective is actually complete — not just
for HTML/Markdown, and not with a blocking persistence path still in the core.

## Evidence Baseline (verified on `origin/main` `211c5c1`, 2026-06-06)

These are measured facts, not memory:

- **A1 (T2)** — `MetBench_BLL.Core/SystemMT/ImportExport/ExecutionArtifacts/ExecutionArtifactExporter.cs:77-95`
  emits only `report.html` + `report.md`. `Word/Excel/PdfSystemMtResultReportRenderer`
  (in `MetBench_BLL/Reporting/SystemMt/`) exist but are **not** wired into the
  async export bundle. Interfaces `IWord/IExcel/IPdfSystemMtResultReportRenderer`
  live in `MetBench_BLL.Core/SystemMT/Reporting/`, so the Core-layer exporter can
  depend on the interface and have concrete renderers injected at composition.
- **A2 (T0)** — `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs:179`
  (`_legacyResults.SaveAsync(...).GetAwaiter().GetResult()`) and `:309`
  (`_evidence!.SaveAsync(...).GetAwaiter().GetResult()`) are sync-over-async
  blocking writes on a core persistence path. Only caller is
  `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`.
- **A3 (T2)** — `SystemMtJobKind.ExportReport` is reserved with no handler;
  `SystemMtJobService.ValidateOperation` validates its fields then throws
  `NotSupportedException`.
- **C1 (T2)** — `ExecutionArtifactExportRequest` takes a single `ExecutionId`;
  no batch/range multi-execution export.
- **C2 (T0)** — `ExecutionEvidence.SampleTraces` carries only target-field
  source/transformed/output triples; multi-variable/multi-path traces pending.
- **C3 (T1/T2, VM)** — gap `G-T1T2-UiOnlyMrSut`: WPF users cannot register/edit/
  validate/save an MR without editing source `catalog.json`.
- **C4 (quality)** — `MetBench_BLL.Core` builds with **0 warnings** today, but no
  project sets `<TreatWarningsAsErrors>`; `MetBench_BLL` carries ~358 nullable
  warnings.

**Corrected stale doc claims (do NOT treat as gaps):** noise-aware typed
predicates (`NoiseAwareBinaryComparisonPredicate` + kernel + dispatcher + the
`p9-k-eff-noise-aware` MR) and the `flw≈k·src` scaling equality
(`ScaledEqualityKernel`) are already implemented and wired. The status-ledger
notes claiming them unimplemented are stale and should be dropped when this
plan's docs task runs.

## Out of Scope (P2 — confirmed excluded)

- Result/evidence **import** into LiteDB (needs trust/provenance model).
- Asset import **live promotion** to catalog/LiteDB (currently staging-only by
  design). Async asset import remains staging-only in this plan.

## PR Chain

| PR | Scope | Layer | Env |
|---|---|---|---|
| PR-0 | Register plan, active-index row, status `Planned` row. | docs | cloud |
| PR-1 | **A1** four-end async export (Word/Excel/PDF added, fail-closed). | T2 | cloud |
| PR-2 | **A2** async execution recorder (`RecordAsync`, no sync-over-async). | T0 | cloud |
| PR-3 | **A3** `ExportReportJobOperationHandler` for the reserved kind. | T2 | cloud |
| PR-4 | **C1** batch/range multi-execution export. | T2 | cloud |
| PR-5 | **C2** richer `SampleTraces` granularity. | T0 | cloud |
| PR-6 | **C4** ratchet `<TreatWarningsAsErrors>` on `MetBench_BLL.Core`; staged nullable-debt note for `MetBench_BLL`. | quality | cloud |
| VM-1 | **C3** UI-only MR CRUD — VM implementation prompt only (cloud writes the prompt; user runs VM). | T1/T2 | VM |
| PR-N | Post-chain holistic review (§12.4 R2) + docs/status closure. | docs | cloud |

Run a `/code-review` checkpoint after each implementation PR. Critical/Important
findings block the next PR.

## Task 0 — Register plan (PR-0)

- [ ] Add active-plan-index row (Active).
- [ ] Add status-ledger Stage-8 row `Planned` (must not be Controlled).
- [ ] Commit `docs(plan): register T0-T2 gap-fill plan`.

## Task 1 — A1: four-end async export (PR-1)

**Files:** `ExecutionArtifactExportRequest.cs`, `ExecutionArtifactExporter.cs`,
`ExecutionArtifactExportJobTests.cs`, `ExecutionArtifactExporterTests.cs`.

- [ ] **TDD:** add failing tests: request with `IncludeWord/Excel/Pdf=true`
  produces `report.docx` / `report.xlsx` / `report.pdf` listed in `manifest.json`;
  requesting a format without its injected renderer fails closed with a clear
  diagnostic (mirror the existing Markdown fail-closed contract).
- [ ] Add `IncludeWord`/`IncludeExcel`/`IncludePdf` (default `false`) to
  `ExecutionArtifactExportRequest`.
- [ ] Inject optional `IWord/IExcel/IPdfSystemMtResultReportRenderer` into
  `ExecutionArtifactExporter`; write each requested format; fail closed when the
  flag is set but the renderer is null.
- [ ] Preserve section parity with `ISystemMtResultReportRenderer` (the renderer
  interfaces already pin this; keep parity tests green).
- [ ] Run focused: `ExecutionArtifactExporterTests|ExecutionArtifactExportJobTests`,
  then the reporting parity tests.
- [ ] `/code-review` checkpoint. Commit.

## Task 2 — A2: async execution recorder (PR-2)

**Files:** `SystemMtExecutionRecorder.cs`, `SystemMtLauncher.cs`, recorder tests.

- [ ] **TDD:** add a guard test asserting the recorder path performs no
  sync-over-async (`.GetAwaiter().GetResult()` / `.Result` / `.Wait()`); evidence
  + legacy-mirror rows are still written; behaviour byte-identical to before.
- [ ] Introduce `RecordAsync(...)` returning `Task<RecordedExecution>`; replace
  the two `.GetAwaiter().GetResult()` calls with `await`.
- [ ] Propagate `await` to the only caller, `SystemMtLauncher`; keep a thin sync
  shim only if an external compat caller requires it (verify first; do not add a
  shim speculatively).
- [ ] Run focused recorder/launcher/async-pipeline tests, then the full suite
  (this touches the core path — full regression required).
- [ ] `/code-review` checkpoint. Commit.

## Task 3 — A3: ExportReport handler (PR-3)

**Files:** new `ExportReportJobOperationHandler.cs`, `SystemMtJobService.cs`
(replace the `NotSupportedException` for `ExportReport`), dispatcher wiring,
tests.

- [ ] **TDD:** submitting an `ExportReport` job for a result set produces the
  selected report formats (reuse the A1 renderer seam) and a manifest; missing
  result fails closed; boundary/parity guard for the new handler.
- [ ] Decide the request shape (result-set selector + format flags); keep facade
  type-leakage rules (CLAUDE.md §6).
- [ ] Remove the reserved-kind `NotSupportedException`; register the handler in
  the dispatcher.
- [ ] Run focused job/dispatcher tests. `/code-review` checkpoint. Commit.

## Task 4 — C1: batch/range export (PR-4)

- [ ] **TDD:** export request accepting multiple `ExecutionId`s (or a filter)
  produces a per-execution sub-bundle + a top-level batch manifest; partial
  missing executions fail closed or are reported per-item (decide + test).
- [ ] Implement; reuse the single-execution exporter per item.
- [ ] Focused tests. `/code-review`. Commit.

## Task 5 — C2: richer SampleTraces (PR-5)

- [ ] **TDD:** recorder writes multi-variable / multi-path sample traces (not
  only the single target-field triple); assert the new granularity in evidence.
- [ ] Implement minimal extension to the trace capture; keep `ExecutionEvidence`
  schema backward-compatible (nullable/append-only).
- [ ] Focused evidence/recorder tests + full regression. `/code-review`. Commit.

## Task 6 — C4: warning ratchet (PR-6)

- [ ] Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to
  `MetBench_BLL.Core.csproj` (it builds 0 warnings today, so this only locks the
  gain). Verify `dotnet build MetBench_BLL.Core` stays green.
- [ ] Do **not** flip it on `MetBench_BLL` yet (358 warnings); instead register a
  tracked debt item (gap report) for staged nullable cleanup and, optionally,
  bump `MetBench_Client` `LangVersion` to enable `required` (VM-verified).
- [ ] Commit.

## Task 7 — C3: UI-only MR CRUD (VM-1, prompt only)

- [ ] Cloud writes a VM implementation prompt under
  `docs/superpowers/vm-prompts/` describing the WPF MR CRUD page (register / view
  / edit / validate / save MR without editing source `catalog.json`), with
  preconditions, exact build/run commands, expected screenshots, and pass/fail
  criteria. Do **not** attempt WPF code from cloud beyond the cloud-safe
  validation/service seam (which, if needed, gets its own cloud PR first).
- [ ] User schedules the VM run; cloud consumes the returned evidence.

## Task 8 — Post-chain review + closure (PR-N)

- [ ] After PR-1..PR-6 merge, run a fresh-session §12.4 R2 holistic review on the
  cumulative diff; resolve findings in a cleanup PR.
- [ ] Update `docs/status/current.md`, `docs/requirements.md`,
  `docs/PROJECT-STRUCTURE.md`, active index; drop the stale noise-aware /
  scaling-equality "unimplemented" notes. Mark Controlled only with full evidence
  (cloud tests + any VM evidence + merge commits + branch cleanup).

## Acceptance Criteria

- A1: async export bundle can contain HTML + Markdown + Word + Excel + PDF, each
  fail-closed when its renderer is absent; manifest lists every emitted file.
- A2: no `.GetAwaiter().GetResult()` / `.Result` / `.Wait()` on the recorder
  path; evidence + legacy mirror still written; full suite green.
- A3: `ExportReport` is a real async operation, no longer throwing
  `NotSupportedException`.
- C1: multi-execution export works with a batch manifest.
- C2: sample traces carry more than the single target-field triple.
- C4: `MetBench_BLL.Core` builds with `TreatWarningsAsErrors` on.
- C3: VM prompt exists; UI MR CRUD verified on VM (separate evidence).
- P2 items remain explicitly out of scope.

## Self-Review

- Scope confirmed P0+P1+P3; P2 excluded by user decision.
- Every gap cites a verified `file:line`, not a doc claim; two stale doc claims
  (noise-aware, scaling-equality) explicitly excluded as already-done.
- Cloud vs VM split is explicit; WPF MR CRUD stays a VM prompt.
- TDD-first per task; per-PR `/code-review`; chain-end §12.4 R2 review planned.
