# T1 System MT Runtime Environment Governance v1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add T1 runtime environment governance so SUT runtime, dependency, and middleware failures are preflight-checked and recorded separately from MetBench orchestration failures and MR assertion failures.

**Architecture:** Build on the existing System MT launcher facade and async pipeline. Add runtime profile/capsule models, a preflight gate, and runtime failure evidence without adding Docker, remote, HPC, WPF UI, dependency auto-installation, or MR semantic changes in v1.

**Tech Stack:** .NET 8, C#, xUnit, System MT launcher, async job pipeline, existing execution evidence recorder, manifest runtime keys.

---

> Date: 2026-06-04
> Status: Active scoped T1 plan
> Design: `docs/superpowers/specs/2026-06-04-systemmt-runtime-environment-governance-v1-design.md`
> Execution task: `docs/superpowers/tasks/2026-06-04-t1-runtime-governance-v1-cloud-task.md`
> Execution mode: TDD, cloud-safe core implementation first

## Goal

Make SUT runtime environments explicit, preflight-checked, and evidence-backed before System MT execution runs. The first version must separate runtime/dependency/middleware failures from MetBench orchestration failures and MR assertion failures.

## T-Plan Placement

This plan is a T1 follow-up.

T1 owns the direct support layer around System MT execution: runner, adapter, runtime entry, I/O boundary, multi-environment resolution, and operational execution support. The existing T1 multi-env work made runtime keys configurable through `LauncherOptions.RuntimePythons` and `ResolvePythonExecutable(...)`; this plan activates the next T1 step by turning runtime/dependency/middleware health into preflight and evidence.

This plan does not reopen T0 MR semantics, T3 SUT coverage selection, T5 anomaly workflow behavior, or T6 mutation testing.

## Non-Goals

- No Docker backend implementation.
- No remote server backend implementation.
- No HPC scheduler implementation.
- No WPF runtime-management UI.
- No automatic dependency installation.
- No change to MR semantics, typed predicates, or existing minimum-mr-subset P3/P8 logic.
- No claim that external canonical source smoke passed unless the exact external source and dependencies actually ran.

## Preconditions

- Start from branch `t1-runtime-governance-v1-implementation`, which is created from the accepted runtime-governance plan branch.
- Read `docs/status/current.md`, this plan, the design spec, and `CLAUDE.md` before editing code.
- Confirm the worktree is clean or isolate work in a fresh branch or worktree.
- Preserve current async execution path: `SystemMtJobService -> SystemMtJobWorker -> SystemMtAsyncPipeline -> ISystemMtLauncher`.
- Preserve current manifest runtime key behavior: `LauncherOptions.RuntimePythons` and `ResolvePythonExecutable(...)`.

## Execution Handoff

Use the dedicated task prompt:

```bash
rtk git switch t1-runtime-governance-v1-implementation
rtk sed -n '1,260p' docs/superpowers/tasks/2026-06-04-t1-runtime-governance-v1-cloud-task.md
```

Then execute the task exactly as written. The task requires subagent-driven development, TDD, per-task spec review, per-task code-quality review, final review, and verification before completion.

## Task 1: Runtime Profile Model

### Preconditions

- The design spec is present and accepted.
- No implementation code has been changed yet.

### Core Steps

1. Add runtime model types under `MetBench_BLL.Core/SystemMT/Runtime/`.
2. Include at least:
   - `RuntimeProfile`
   - `RuntimeKind`
   - `RuntimeDependencyCheck`
   - `RuntimeVersionCheck`
   - `RuntimeResourceHints`
   - `RuntimeArtifactPolicy`
   - `RuntimeFailureKind`
   - `RuntimePreflightResult`
3. Keep models immutable or init-only where existing BLL.Core conventions allow.
4. Do not add LiteDB persistence or WPF UI in this task.

### Acceptance Criteria

- Model tests prove required fields, immutable/init-only behavior where applicable, and default fail-closed semantics.
- No existing System MT catalog or assertion tests are modified to pass artificially.

## Task 2: Runtime Profile Provider

### Preconditions

- Task 1 tests fail first, then pass.
- Existing `LauncherOptions.RuntimePythons` behavior is understood.

### Core Steps

1. Add `IRuntimeProfileProvider`.
2. Add a minimal provider that can build local Python profiles from existing launcher runtime keys.
3. Preserve known keys such as `system`, `openmoc`, `openmc`, and `scipy` where present.
4. Unknown non-system runtime keys must remain fail-closed.
5. Keep Docker, remote, and HPC as explicit profile kinds or placeholders only. Do not execute them in v1.

### Acceptance Criteria

- Tests cover known profile lookup, unknown profile failure, and no silent fallback for non-system keys.
- Existing `RuntimeEnvironmentResolutionException` behavior remains compatible.

## Task 3: Preflight Service

### Preconditions

- Task 2 provider tests pass.
- A process-runner seam is identified or introduced in the smallest reasonable form.

### Core Steps

1. Add `IRuntimePreflightService`.
2. Add `RuntimePreflightService` with a fakeable process runner.
3. Check executable resolution, version commands, import checks, required environment variables, and timeout handling.
4. Capture stdout/stderr summaries without treating them as user-facing proof unless command exit status is successful.
5. Do not install or repair missing dependencies.

### Acceptance Criteria

- Unit tests cover executable missing, dependency missing, version command success/failure, required environment variable missing, timeout, and pass case.
- Diagnostics include failure kind and detail suitable for persistence.

## Task 4: Launcher Integration

### Preconditions

- Task 3 tests pass.
- Existing launcher tests are green before integration, or any unrelated failure is documented.

### Core Steps

1. Wire preflight into the launcher facade before actual SUT execution.
2. Ensure synchronous launcher calls and async job calls share the same gate.
3. On preflight failure, return an execution result classified as environment/preflight failure, not an MR anomaly.
4. Preserve current pure-stdlib runtime slices by using lightweight checks for their profiles.

### Acceptance Criteria

- Launcher integration test proves a missing dependency blocks before SUT execution.
- Launcher integration test proves a healthy pure-stdlib profile still runs an existing MR.
- Existing System MT semantic boundary tests still pass.

## Task 5: Runtime Evidence

### Preconditions

- Task 4 integration behavior is test-backed.
- Current evidence model and recorder behavior are read before editing.

### Core Steps

1. Add runtime evidence fields to the existing evidence path, preferably as a nested `RuntimeEvidence` object.
2. Record runtime key, resolved executable path, version check summaries, dependency check summaries, and failure kind.
3. Ensure evidence serialization or persistence remains backward-compatible.
4. Do not require old records to have runtime evidence.

### Acceptance Criteria

- Evidence tests prove runtime evidence is written on pass and on preflight failure.
- Existing evidence tests still pass.
- Old evidence without runtime evidence remains readable if the current serializer supports that scenario.

## Task 6: Async Pipeline Status Propagation

### Preconditions

- Task 4 and Task 5 tests pass.
- The async worker and polling store are understood.

### Core Steps

1. Add async-path tests that submit a job whose runtime preflight fails.
2. Verify polling returns a terminal failure state with preflight/runtime classification.
3. Verify cancellation and timeout semantics are not regressed.
4. Do not duplicate preflight logic in the worker if launcher-level gating already covers it.

### Acceptance Criteria

- Async job tests prove preflight failure appears through status polling.
- Existing async job tests for success, failure, cancellation, and timeout still pass.

## Task 7: Documentation And Status Ledger

### Preconditions

- Code tasks and focused tests pass, or blockers are explicitly documented.

### Core Steps

1. Update `docs/status/current.md` with the exact implementation status and evidence.
2. Update the active plan index row for this plan from Active to Controlled only after implementation is merged.
3. Document any environment limitations without converting them into pass claims.

### Acceptance Criteria

- Status ledger distinguishes design accepted, implementation merged, and external runtime evidence.
- PR body lists exact tests and Windows classification.

## Focused Verification Commands

Use exact commands when the implementation exists:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimeProfile|FullyQualifiedName~RuntimePreflight|FullyQualifiedName~RuntimeEvidence"
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtJob|FullyQualifiedName~SystemMtAsyncPipeline"
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "SemanticCatalogBoundaryTests|Catalog_MR_id_set_equals_governance_whitelist"
rtk git diff --check
```

## Implementation Notes

- Prefer the existing launcher facade and evidence recorder over a new execution path.
- Prefer additive models and nullable runtime evidence fields over migrations in v1.
- Treat dependency absence as `DependencyMissing` or `PreflightFailed`, not as an MR assertion failure.
- Keep profile checks cheap enough for local pure-stdlib SUTs.
- Docker, remote, and HPC should be modeled as future profile kinds only until a separate backend plan is approved.
