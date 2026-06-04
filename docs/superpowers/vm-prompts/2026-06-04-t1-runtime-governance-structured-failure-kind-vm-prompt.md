# T1 Runtime Governance Structured Failure Kind VM Prompt

> Date: 2026-06-04
> Target branch: `t1-runtime-governance-v1-structured-failure-kind`
> Base context: PR #294 `feat(systemmt): add runtime governance v1`
> Residual risk: async job runtime-preflight classification currently depends on `FailureReason.StartsWith("Runtime preflight failed:")`

## User Instruction

Switch to branch `t1-runtime-governance-v1-structured-failure-kind`, read this file, and execute the task.

## Goal

Remove string-prefix coupling from the async job runtime-preflight path. Runtime preflight failures must be propagated through a structured failure-kind field, not by parsing `FailureReason`.

## Hard Scope

Do:

- Add a nullable structured failure-kind field to async job DTOs and durable job records.
- Populate it from `ExecutionEvidence.RuntimeEvidence.FailureKind` when a launcher run produced runtime-preflight evidence.
- Preserve `FailureReason` as human-readable text.
- Keep runtime-preflight failures terminal `Failed` jobs with no saved `MrRunResult`.
- Keep MR assertion failures as infrastructure `Succeeded` jobs whose `MrRunResult.Passed` can be false.
- Use TDD: failing tests first, then minimal implementation.

Do not:

- Add fields to `MrRunResult`.
- Change `MrRunResultShapeLockTests` except to verify it remains unchanged if needed.
- Rework T0 MR semantics.
- Rework T5 anomaly workflow.
- Add Docker, remote, HPC, or WPF runtime-management UI.
- Auto-install dependencies.
- Merge PR #294.

## Why Not Add `RuntimeFailureKind` To `MrRunResult`

`MetBench_SystemMT.Tests/SystemMT/Launcher/MrRunResultShapeLockTests.cs` explicitly locks the launcher facade shape and says new fields require re-evaluating the launcher type-leakage rule. This residual-risk fix should avoid changing `MrRunResult`.

Use persisted evidence instead:

1. `SystemMtLauncher.RunAsync(...)` already records blocked preflight rows through `SystemMtExecutionRecorder.RecordBlockedPreflight(...)`.
2. `ExecutionEvidence.RuntimeEvidence.FailureKind` already carries the structured value.
3. `MrRunResult.RecordId` is the execution id string.
4. `SystemMtAsyncPipeline` can use an optional `IExecutionEvidenceRepository` to read the evidence row and classify the async outcome structurally.

## Expected Design

### Data Shape

Add nullable `FailureKind` string fields:

- `MetBench_BLL.Core/SystemMT/Jobs/JobExecutionOutcome`
- `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord`
- `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus`

Use string rather than enum in the durable job status layer so non-runtime backend failures can later use non-runtime categories without forcing a persistence migration. The current runtime values should be `RuntimeFailureKind.*.ToString()`.

### Async Pipeline

Modify:

- `MetBench_BLL.Core/SystemMT/Jobs/SystemMtAsyncPipeline.cs`

Constructor target:

```csharp
public SystemMtAsyncPipeline(
    ISystemMtLauncher launcher,
    IExecutionEvidenceRepository? evidenceRepository = null)
```

Behavior:

- Call `ISystemMtLauncher.RunAsync(...)` as today.
- If `result.Passed == false`, try to parse `result.RecordId` as `Guid`.
- If an evidence repository is available, load `ExecutionEvidence` by execution id.
- If `ExecutionEvidence.RuntimeEvidence is { Passed: false } runtime`, return:

```csharp
new JobExecutionOutcome(
    SystemMtJobState.Failed,
    sutName,
    Result: null,
    FailureReason: result.FailureReason,
    FailureKind: runtime.FailureKind)
```

- If runtime evidence is absent, keep the existing MR assertion behavior: return `Succeeded` with the `MrRunResult`.
- Remove the `FailureReason.StartsWith("Runtime preflight failed:")` branch.

### Worker And Store Projection

Modify:

- `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobWorker.cs`
- `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobRecord.cs`
- `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs`

Behavior:

- `SystemMtJobWorker` must persist `outcome.FailureKind` into `SystemMtJobRecord.FailureKind`.
- `SystemMtJobRecord.ToStatus()` must project `FailureKind` into `SystemMtJobStatus`.
- On success, `FailureKind` must be null.
- On cancellation, `FailureKind` may remain null unless the current code already has a structured cancellation kind.

### WPF Hosted Worker Wiring

Modify:

- `MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs`

Behavior:

- Resolve `IExecutionEvidenceRepository` from the scope if registered.
- Pass it to `new SystemMtAsyncPipeline(launcher, evidenceRepository)`.
- Keep this additive and cloud-safe. If a test project cannot compile WPF on the current VM, report that precisely.

The current WPF app registration already includes:

- `MetBench_Client/App.xaml.cs` registration for `IExecutionEvidenceRepository`

Do not add a new repository registration unless a compile error proves it is missing.

## Required TDD Tasks

### Task 1: Async Pipeline Uses Evidence, Not FailureReason Prefix

Write the failing test first in:

- `MetBench_SystemMT.Tests/SystemMT/Jobs/RuntimePreflightAsyncJobTests.cs`

Add or replace a test so that:

- stub launcher returns `MrRunResult.Passed = false`
- `FailureReason` does **not** start with `Runtime preflight failed:`
- `RecordId` is a real execution id
- fake evidence repo returns `ExecutionEvidence.RuntimeEvidence.Passed = false`
- fake evidence repo returns `FailureKind = RuntimeFailureKind.DependencyMissing.ToString()`
- expected outcome:
  - `FinalState == SystemMtJobState.Failed`
  - `Result == null`
  - `FailureKind == RuntimeFailureKind.DependencyMissing.ToString()`

This test must fail against the current PR #294 code because current code classifies runtime failure by `FailureReason.StartsWith(...)`.

Focused RED command:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimePreflightAsyncJobTests" --logger "console;verbosity=minimal"
```

Expected RED:

- at least one failure showing the async pipeline returned `Succeeded` or did not populate `FailureKind`.

Implement the minimal code after RED is observed.

Focused GREEN command:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimePreflightAsyncJobTests" --logger "console;verbosity=minimal"
```

Expected GREEN:

- all `RuntimePreflightAsyncJobTests` pass.

### Task 2: Worker Persists FailureKind Into Polling Status

Write the failing test first in:

- `MetBench_SystemMT.Tests/SystemMT/Jobs/RuntimePreflightAsyncJobTests.cs`

Expected test behavior:

- `SystemMtJobWorker` runs a job whose pipeline returns `FinalState = Failed`, `FailureKind = "DependencyMissing"`.
- `IJobStore.GetAsync(jobId)` returns a record with `FailureKind = "DependencyMissing"`.
- `record.ToStatus().FailureKind == "DependencyMissing"`.
- `IJobStore.GetResultAsync(jobId)` remains null.

Focused RED/GREEN command:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimePreflightAsyncJobTests" --logger "console;verbosity=minimal"
```

### Task 3: WPF Hosted Service Source Wiring

Add a source-level guard test if an existing WPF source-wiring test pattern exists. Search first:

```powershell
Select-String -Path MetBench_SystemMT.Tests\**\*.cs -Pattern "SystemMtJobWorkerHostedService|WpfAsyncJobCancellationWiringTests|new SystemMtAsyncPipeline" -CaseSensitive:$false
```

If there is an existing source guard, extend it to assert:

- `SystemMtJobWorkerHostedService.cs` resolves `IExecutionEvidenceRepository`
- it constructs `SystemMtAsyncPipeline(launcher, evidenceRepository)` or equivalent

If there is no suitable guard, create a narrowly scoped source guard under:

- `MetBench_SystemMT.Tests/SystemMT/Jobs/WpfAsyncJobRuntimeEvidenceWiringTests.cs`

Do not use this test as a substitute for WPF build. It is only a cloud-safe source wiring guard.

Focused command:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfAsyncJobRuntimeEvidenceWiringTests|FullyQualifiedName~WpfAsyncJobCancellationWiringTests" --logger "console;verbosity=minimal"
```

### Task 4: Shape Lock And Existing Semantics

Run these after implementation:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~MrRunResultShapeLockTests|FullyQualifiedName~RuntimePreflightAsyncJobTests|FullyQualifiedName~SystemMtAsyncPipelineTests|FullyQualifiedName~SystemMtJobWorkerTests" --logger "console;verbosity=minimal"
```

Expected:

- `MrRunResultShapeLockTests` pass unchanged.
- runtime preflight async tests pass.
- existing async pipeline and worker tests pass.

### Task 5: Final Verification

Run:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimeProfile|FullyQualifiedName~RuntimePreflight|FullyQualifiedName~RuntimeEvidence|FullyQualifiedName~SystemMtJob|FullyQualifiedName~SystemMtAsyncPipeline|FullyQualifiedName~MrRunResultShapeLockTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "SemanticCatalogBoundaryTests|Catalog_MR_id_set_equals_governance_whitelist" --logger "console;verbosity=minimal"
dotnet build MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore
dotnet build MetBench.sln --no-restore
git diff --check
```

If any command cannot run, report the exact command, exit code, and reason. Do not convert a skipped or blocked command into PASS.

## Commit

Commit only after tests pass or after a blocker is documented.

Suggested commit message:

```powershell
git add MetBench_BLL.Core/SystemMT/Jobs MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs MetBench_SystemMT.Tests/SystemMT/Jobs
git commit -m "fix(systemmt): propagate runtime preflight failure kind"
```

Push:

```powershell
git push -u origin t1-runtime-governance-v1-structured-failure-kind
```

Do not merge PR #294. Report back with:

- branch
- commit SHA
- exact files changed
- exact test commands and pass/fail counts
- whether `MrRunResultShapeLockTests` stayed unchanged and passed
- whether WPF build ran
- any blocker
