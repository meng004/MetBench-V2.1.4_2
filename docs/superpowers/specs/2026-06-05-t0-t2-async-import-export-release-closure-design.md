# T0-T2 Async Import/Export Release Closure Design

> **Status:** Proposed design approved in conversation on 2026-06-05.
> **Scope:** System-MT plus WPF user operation entry points. Method-MT legacy paths remain compatibility surfaces and are not part of this closure.

## 1. Goal

Close T0, T1, and T2 for a release-quality System-MT user path by making all user-visible long-running operations asynchronous and by adding import/export coverage for assets and run artifacts.

The target closure is a release closure, not only a code closure:

- cloud-side code and tests;
- Windows VM / WPF build and visible-operation evidence;
- documentation projections and user guide updates;
- PR merge, branch cleanup, and status ledger update.

## 2. Current Evidence Baseline

The current repository already has these controlled foundations. These are baseline facts for planning, not fresh verification for the future implementation PRs:

| Baseline fact | Evidence source |
|---|---|
| T0-T5 release readiness is Controlled, with VM release smoke evidence: 22/22 filtered commands PASS, full `MetBench_SystemMT.Tests` suite 1558 pass / 0 fail / 12 env-gated skips, WPF build 0 errors, screenshot matrix 21/21 PASS. | `docs/status/current.md` §2 and §3 row "T0-T5 minimum release readiness"; supporting files under `docs/superpowers/specs/2026-05-30-t0-t5-*`. |
| The current runtime catalog provider inventory is 21 SUT / 17 equations / 38 MRs. | `docs/status/current.md` §2 "Current SUT / equation / MR inventory"; `docs/PROJECT-STRUCTURE.md` §2. |
| System-MT async execution v1 exists through `SystemMtJobService -> SystemMtJobWorker -> SystemMtAsyncPipeline -> ISystemMtLauncher`. | `docs/status/current.md` §3 rows "System MT async execution WPF consumer" and "Minimum-MR-SubSet B-group two-stage import/runtime promotion"; code under `MetBench_BLL.Core/SystemMT/Jobs/`. |
| WPF has `SystemMtAsyncJobPage` and `SystemMtAsyncJobViewModel` for submit, polling, refresh, cancel, and result projection. | `docs/status/current.md` §3 row "System MT async execution WPF consumer"; code under `MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml` and `MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs`. |
| Runtime governance v1 is Controlled and preflight failures are recorded as runtime evidence and surfaced through async failed-state propagation. | `docs/status/current.md` §3 row "T1 runtime environment governance v1"; active index row for `2026-06-04-systemmt-runtime-environment-governance-v1-plan.md`. |
| Minimum-MR-SubSet A/B staged import packages exist under `MetBench_BLL.Core/SystemMT/ImportExport/Put/`; A/B live runtime promotion is already validated through launcher and async job tests. | `docs/status/current.md` §3 rows "Minimum-MR-SubSet A-group live runtime promotion" and "Minimum-MR-SubSet B-group two-stage import/runtime promotion"; tests under `MetBench_SystemMT.Tests/SystemMT/ImportExport/`. |
| Reporting and evidence projection exist: HTML/Markdown/Word/Excel/PDF reporting surfaces and `ExecutionEvidence` persistence are present. | `docs/status/current.md` §3 rows "ExecutionEvidence v2 schema and recorder", "Evidence-aware HTML report rendering", and "Evidence-aware execution markdown report"; requirements rows F-T2-01/F-T2-02. |

These facts mean the next work should compose existing seams rather than rewrite the launcher, typed catalog, evidence recorder, or WPF navigation shell.

## 3. Non-Goals

- Do not redesign Method-MT or migrate legacy Method-MT UI paths.
- Do not remove synchronous APIs. They remain internal compatibility paths for tests, services, and short local runs.
- Do not introduce Docker, remote server, HPC, dependency installation, or runtime image management in this closure.
- Do not import external execution evidence into the local database until a trust/provenance rule is designed. Results/evidence/report **export** is in scope; result/evidence **import** is explicitly out of scope for this release closure.
- Do not change Typed Semantic Catalog public semantics.
- Do not add new SUTs or new MR semantics.

## 4. Target User Semantics

After this closure, a WPF user should not need to wait on the UI thread for long-running System-MT work.

The user-visible long-running operations are:

1. MR execution and batch execution.
2. Asset package import/export for SUT, MR, sample cases, and mutation placeholders.
3. Execution-result, evidence, and report export.

Each operation should create a job, return a job id quickly, and update status through polling. Terminal states must be explicit: `Succeeded`, `Failed`, or `Cancelled`. Runtime failures must preserve the structured runtime failure kind when evidence exists.

## 5. Architecture

### 5.1 Job Layer

The existing MR-run job path is the reference pattern:

```text
WPF command
  -> ISystemMtJobService.SubmitAsync(...)
  -> IJobStore creates Queued record
  -> IJobQueue enqueues job id
  -> SystemMtJobWorker
  -> ISystemMtAsyncPipeline
  -> domain operation
  -> IJobStore persists terminal state and result/export artifact reference
```

The current `SystemMtJobRequest` is MR-run specific. This closure should add an operation-kind abstraction rather than overloading `MrId` for import/export/report jobs.

Recommended minimal model:

```csharp
public enum SystemMtJobKind
{
    RunMr,
    RunBatch,
    ImportAssets,
    ExportAssets,
    ExportExecutionArtifacts
}

public sealed record SystemMtOperationJobRequest(
    SystemMtJobKind Kind,
    string? MrId,
    IReadOnlyList<string>? MrIds,
    string? PackageRoot,
    string? StagingRoot,
    string? ExportRoot,
    Guid? ExecutionId,
    IReadOnlyDictionary<string, string>? ParameterOverrides);
```

The implementation may keep `SystemMtJobRequest` as a compatibility wrapper that maps to `SystemMtOperationJobRequest(Kind: RunMr, ...)`.

### 5.2 Domain Operations

The job worker should delegate to small operation handlers:

- `RunMrJobHandler` delegates to the existing `SystemMtAsyncPipeline`.
- `RunBatchJobHandler` delegates to launcher batch support or runs MR jobs sequentially through the same pipeline with progress updates.
- `ImportAssetsJobHandler` imports `sut-import-unit.json`, validates via `SutImportValidator`, and writes only to a staging/import area in v1 unless a later task explicitly promotes it.
- `ExportAssetsJobHandler` exports a validated `SutImportUnit` using `SutImportPackageExporter`.
- `ExportExecutionArtifactsJobHandler` exports `SystemMtResultRecord`, optional `ExecutionEvidence`, and selected report formats to an export directory.

Handlers must be fail-closed and must not parse failure strings to infer runtime kind. If structured runtime evidence exists, it should flow into job status.

### 5.3 Asset Import/Export

The asset package boundary remains the current single-SUT import unit:

- one SUT;
- multiple MRs;
- multiple input/output groups;
- multiple mutation assets;
- explicit provenance;
- compatibility profile;
- validation before import and before export.

This closure makes that operation asynchronous and WPF-visible. It does not expand the package schema unless tests prove a field is required for T0-T2 release closure.

### 5.4 Result/Evidence/Report Export

Export should be one-way in this closure.

The export bundle should include:

- `execution-result.json` for the selected execution or selected run set;
- `execution-evidence.json` when an `ExecutionEvidence` row exists;
- selected report files generated through existing report services/renderers;
- `manifest.json` with export time, MetBench version context if available, execution ids, and job id.

Importing these records back into LiteDB is not allowed in this release closure because it needs a separate trust model.

### 5.5 WPF UX Boundary

WPF should expose a single async operations surface or extend `SystemMtAsyncJobPage` into operation tabs:

- Run;
- Import/Export assets;
- Export results/reports.

The page must support submit, polling, refresh, cancel where meaningful, terminal state display, failure reason, artifact path display, and copyable job id. It must not block the dispatcher during operation execution.

### 5.6 Documentation Boundary

The same PR chain must update:

- `docs/status/current.md`;
- `docs/requirements.md`;
- `docs/PROJECT-STRUCTURE.md`;
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`;
- `docs/usage/MetBench-T0-T5-操作指南.md`;
- a VM prompt under `docs/superpowers/vm-prompts/`.

## 6. Acceptance Criteria

### T0 Closure

- WPF user can submit single MR and batch MR execution as async jobs.
- Polling status reaches terminal state and result is visible.
- Synchronous launcher API remains available and covered by existing tests.
- Async tests prove the real path uses `SystemMtJobService -> SystemMtJobWorker -> SystemMtAsyncPipeline -> ISystemMtLauncher`.
- Batch execution must include per-MR summary, partial-failure semantics, and cancellation semantics; otherwise batch must be removed from the release scope before implementation starts.

### T1 Closure

- Asset import/export for SUT/MR/sample/mutation package runs as async jobs.
- Validation failures become terminal failed jobs with explicit diagnostics.
- Successful asset import must create a concrete staged artifact in a deterministic staging root; validation-only import is not sufficient for T1 closure.
- Runtime preflight failures preserve structured failure kind when evidence exists.
- WPF asset import/export operation is visible and verified on Windows VM.

### T2 Closure

- Result/evidence/report export runs as async jobs.
- Export bundle contains manifest, result JSON, evidence JSON when present, and selected report files.
- WPF result/report export operation is visible and verified on Windows VM.
- User guide shows the async operation workflow and generated artifact location.

### Release Closure

- Cloud focused tests pass.
- Windows VM build and UI operation evidence pass.
- Implementation PRs are merged with required checks green and review findings handled.
- A post-merge status/projection closure PR records the actual merge commit, fetched `origin/main`, VM evidence path, and branch cleanup state before any Controlled claim is made.

## 7. Risks and Controls

| Risk | Control |
|---|---|
| Async job model becomes a parallel launcher. | Keep launcher as execution boundary; job handlers orchestrate only. |
| Result/evidence import pollutes local truth. | Do not implement result/evidence import in this closure. Export only. |
| Import/export package schema grows without evidence. | Add only fields required by failing tests; keep current `SutImportUnit` model as default. |
| WPF tests cannot run on cloud. | Cloud PRs include source guards and core tests; Windows VM prompt captures build/UI evidence. |
| Batch execution hides per-MR failures. | Batch status must include per-MR summary and terminal state must be failed if any required MR fails unless explicitly configured otherwise. |
| Runtime/environment failures get misclassified as MR failures. | Preserve `RuntimeEvidence.FailureKind` through job status; never parse strings when structured evidence exists. |

## 8. Open Questions Locked for Later

The following are intentionally not solved in this closure:

- trusted import of external execution evidence;
- remote/HPC/Docker runtime backends;
- dependency auto-install;
- full Method-MT async conversion;
- T6 semantic mutation and minimum MR subset analytics.
