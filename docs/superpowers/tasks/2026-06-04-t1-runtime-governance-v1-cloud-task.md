# T1 Runtime Governance v1 Cloud Implementation Task

> Date: 2026-06-04
> Branch to use: `t1-runtime-governance-v1-implementation`
> Plan: `docs/superpowers/plans/2026-06-04-systemmt-runtime-environment-governance-v1-plan.md`
> Design: `docs/superpowers/specs/2026-06-04-systemmt-runtime-environment-governance-v1-design.md`

## User Instruction

Switch to branch `t1-runtime-governance-v1-implementation`, read this file, and execute the task.

## Hard Scope

Implement the first version of T1 runtime environment governance:

- runtime profile / capsule models
- runtime profile provider
- runtime preflight service
- runtime failure taxonomy
- runtime evidence attachment
- launcher-level preflight integration
- async job status propagation through the existing launcher path
- status ledger and active plan index closure only after tests pass

Do not implement these in this task:

- Docker execution backend
- remote server execution backend
- HPC scheduler backend
- WPF runtime-management UI
- dependency auto-installation
- T0 MR semantic changes
- T3 SUT expansion
- T5 anomaly workflow redesign
- T6 mutation testing
- external canonical source smoke claims unless the exact external runtime and dependencies actually ran

## Required Superpowers Workflow

Use these skills in order:

1. `superpowers:subagent-driven-development`
2. `superpowers:test-driven-development`
3. `superpowers:requesting-code-review`
4. `superpowers:verification-before-completion`
5. `superpowers:finishing-a-development-branch`

If the environment does not provide a subagent or Task tool, stop and report this blocker. Do not silently replace subagent-driven development with inline implementation.

## Preconditions

Run these exact commands first:

```bash
rtk git status --short --branch
rtk git rev-parse --short=12 HEAD
rtk git merge-base --short origin/main HEAD
rtk dotnet --info
```

Expected:

- current branch is `t1-runtime-governance-v1-implementation`
- worktree is clean before implementation starts
- `.NET SDK` is available

If `.NET SDK` is unavailable, stop and report the blocker. Do not implement code that cannot be compiled or tested.

Read these files before editing code:

```bash
rtk sed -n '1,180p' AGENTS.md
rtk sed -n '1,160p' docs/status/current.md
rtk sed -n '1,120p' docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
rtk sed -n '1,220p' docs/superpowers/specs/2026-06-04-systemmt-runtime-environment-governance-v1-design.md
rtk sed -n '1,260p' docs/superpowers/plans/2026-06-04-systemmt-runtime-environment-governance-v1-plan.md
```

## Code Context

Existing anchors confirmed before this task was written:

- `MetBench_BLL.Core/SystemMT/Launcher/LauncherOptions.cs`
- `MetBench_BLL.Core/SystemMT/Launcher/RuntimeEnvironmentResolutionException.cs`
- `MetBench_BLL.Core/SystemMT/Launcher/SystemMtLauncher.cs`
- `MetBench_BLL.Core/SystemMT/Catalog/ManifestMrCatalogProvider.cs`
- `MetBench_BLL.Core/SystemMT/Pipeline/IProcessExecutor.cs`
- `MetBench_BLL.Core/SystemMT/Pipeline/DefaultProcessExecutor.cs`
- `MetBench_BLL.Core/SystemMT/Pipeline/SystemMtExecutionRecorder.cs`
- `MetBench_BLL.Core/SystemMT/Persistence/ExecutionEvidence.cs`
- `MetBench_BLL.Core/SystemMT/Jobs/SystemMtAsyncPipeline.cs`
- `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobWorker.cs`
- `MetBench_BLL.Core/SystemMT/Jobs/SystemMtJobStatus.cs`
- `MetBench_SystemMT.Tests/SystemMT/Launcher/RuntimeEnvironmentResolverTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtAsyncPipelineTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Jobs/SystemMtJobWorkerTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Persistence/ExecutionEvidenceRoundtripTests.cs`

Use CodeGraph for structural lookups before editing code. Use `rtk rg` only for literal text and path confirmation.

## Subagent Execution Plan

Run one implementation subagent at a time. Do not run implementation subagents in parallel.

For every task below:

1. Dispatch a fresh implementer subagent with the task text.
2. Require RED-GREEN-REFACTOR:
   - write failing tests first
   - run the focused test and capture the expected failure
   - implement the minimum code
   - rerun the focused test and capture pass output
3. After implementer reports DONE, dispatch a spec-compliance reviewer subagent.
4. If spec reviewer finds gaps, send the implementer back to fix them and re-review.
5. After spec compliance passes, dispatch a code-quality reviewer subagent.
6. If code-quality reviewer finds Critical or Important issues, fix and re-review.
7. Commit only after both reviews pass for the task.

### Subtask 1: Runtime Models And Provider

Implement models under:

- `MetBench_BLL.Core/SystemMT/Runtime/`

Expected model names:

- `RuntimeKind`
- `RuntimeFailureKind`
- `RuntimeDependencyCheck`
- `RuntimeVersionCheck`
- `RuntimeResourceHints`
- `RuntimeArtifactPolicy`
- `RuntimeProfile`
- `RuntimePreflightResult`
- `IRuntimeProfileProvider`
- a minimal local profile provider that uses current `LauncherOptions.ResolvePythonExecutable(...)`

Required tests:

- known runtime key resolves to a profile
- unknown non-system runtime key fails closed
- blank runtime key maps to system profile
- local Python profile carries a resolved executable path
- Docker / remote / HPC kinds are modelable but not executable in v1

Suggested test file:

- `MetBench_SystemMT.Tests/SystemMT/Runtime/RuntimeProfileProviderTests.cs`

Focused test command:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimeProfileProviderTests"
```

Commit message:

```bash
rtk git commit -m "feat(systemmt): add runtime profile model"
```

### Subtask 2: Runtime Preflight Service

Implement:

- `IRuntimePreflightService`
- `RuntimePreflightService`

Use the existing `MetBench_BLL.Core/SystemMT/Pipeline/IProcessExecutor.cs` seam where practical. Do not create a second process abstraction unless the existing interface cannot support the checks.

Required behavior:

- missing executable path is `RuntimeExecutableMissing`
- missing import is `DependencyMissing`
- failed version command is `PreflightFailed`
- missing required environment variable is `PreflightFailed`
- timeout is `Timeout`
- successful checks return pass with diagnostics
- preflight never installs or repairs dependencies

Suggested test file:

- `MetBench_SystemMT.Tests/SystemMT/Runtime/RuntimePreflightServiceTests.cs`

Focused test command:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimePreflightServiceTests"
```

Commit message:

```bash
rtk git commit -m "feat(systemmt): add runtime preflight service"
```

### Subtask 3: Launcher And Evidence Integration

Wire runtime preflight at the launcher facade level so sync launcher calls and async jobs share the same gate.

Required behavior:

- healthy pure-stdlib runtime still runs an existing MR
- failed preflight blocks before SUT execution
- preflight failure is not recorded as an MR assertion anomaly
- runtime evidence is attached on pass and on preflight failure
- old evidence without runtime evidence remains readable

Preferred evidence shape:

- add nullable `RuntimeEvidence` to `ExecutionEvidence`
- keep old rows backward-compatible

Suggested test files:

- `MetBench_SystemMT.Tests/SystemMT/Launcher/RuntimePreflightLauncherTests.cs`
- `MetBench_SystemMT.Tests/SystemMT/Persistence/RuntimeEvidenceRoundtripTests.cs`
- extend `MetBench_SystemMT.Tests/SystemMT/Pipeline/ExecutionEvidenceWriteThroughTests.cs` only if the existing recorder test pattern is the cleanest fit

Focused test commands:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimePreflightLauncherTests|FullyQualifiedName~RuntimeEvidenceRoundtripTests"
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~ExecutionEvidenceWriteThroughTests"
```

Commit message:

```bash
rtk git commit -m "feat(systemmt): attach runtime preflight evidence"
```

### Subtask 4: Async Status Propagation

Prove the async path receives preflight behavior through the existing launcher path:

`SystemMtJobService -> SystemMtJobWorker -> SystemMtAsyncPipeline -> ISystemMtLauncher`

Required behavior:

- async job with preflight failure reaches terminal failed/error state
- polling status exposes a runtime/preflight failure reason
- cancellation tests still pass
- timeout tests still pass
- no duplicate preflight implementation in `SystemMtJobWorker`

Suggested test file:

- `MetBench_SystemMT.Tests/SystemMT/Jobs/RuntimePreflightAsyncJobTests.cs`

Focused test command:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimePreflightAsyncJobTests|FullyQualifiedName~SystemMtJobWorkerTests|FullyQualifiedName~SystemMtAsyncPipelineTests"
```

Commit message:

```bash
rtk git commit -m "feat(systemmt): surface runtime preflight in async jobs"
```

### Subtask 5: Docs, Gates, And Final Review

Update docs only after code tests pass:

- `docs/status/current.md`
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- optionally the design or plan if implementation discovered a real constraint

Required doc status:

- move T1 runtime environment governance v1 from Active to Controlled only if implementation and focused tests passed
- record exact commands and outcomes
- keep Windows classification explicit: no WPF / no Windows UI evidence unless separately verified
- do not claim external source smoke for OpenMC/OpenMOC/SciPy unless actually run

Final verification commands:

```bash
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~RuntimeProfile|FullyQualifiedName~RuntimePreflight|FullyQualifiedName~RuntimeEvidence"
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtJob|FullyQualifiedName~SystemMtAsyncPipeline"
rtk dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "SemanticCatalogBoundaryTests|Catalog_MR_id_set_equals_governance_whitelist"
rtk git diff --check
```

If available, also run:

```bash
rtk dotnet build MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore
```

Final review:

1. Compute base/head:

```bash
rtk git merge-base origin/main HEAD
rtk git rev-parse HEAD
```

2. Dispatch a final code-reviewer subagent over the full branch.
3. Fix all Critical and Important findings.
4. Re-run the relevant focused tests after fixes.

Final commit message:

```bash
rtk git commit -m "docs(systemmt): close runtime governance v1 evidence"
```

If there are no doc changes in Subtask 5 because implementation is blocked, do not create a fake closure commit. Report the blocker instead.

## Completion Output Required

Report:

- branch
- commit SHAs created
- files changed
- exact tests run and pass/fail counts when available
- blockers, if any
- whether final review passed
- whether PR was created

Do not merge the branch unless the user explicitly asks for merge.
