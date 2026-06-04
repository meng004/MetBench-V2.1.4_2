# Cloud Task — Minimum-MR-SubSet B-Group Two-Stage Import And Runtime

## Operator Instruction

切换到分支 `codex/minimum-mr-subset-b-group-two-stage-plan`，读取 `docs/superpowers/tasks/2026-06-04-minimum-mr-subset-b-group-two-stage-cloud-task.md`，执行任务。

If the execution environment is Windows VM and `rtk` is not available, do not use this cloud task as the command source. Read and execute:

```text
docs/superpowers/tasks/2026-06-04-minimum-mr-subset-b-group-two-stage-windows-vm-task.md
```

## Role

You are the cloud/Linux implementation agent for MetBench. Use superpowers before coding:

- Use `superpowers:executing-plans` or `superpowers:subagent-driven-development`.
- Use `superpowers:test-driven-development` for every code change.
- Use `superpowers:verification-before-completion` before any completion claim.

All shell commands in this repository must start with `rtk`.

## Objective

Implement the second `minimum-mr-subset` batch in two ordered stages:

1. **Stage 1 import only**: add P8 Schrodinger and P3 Lorenz to staged import/export packages.
2. **Stage 2 live runtime promotion**: promote explicit P8/P3 runtime MR slices and validate them through the current async execution pipeline.

The async validation path is mandatory:

```text
SystemMtJobService
  -> IJobQueue / IJobStore
  -> SystemMtJobWorker
  -> SystemMtAsyncPipeline
  -> ISystemMtLauncher
```

Do not treat direct `ISystemMtLauncher.RunAsync` tests alone as sufficient for Stage 2.

## Required Reading

Read these files before editing:

- `AGENTS.md`
- `CLAUDE.md`
- `docs/status/current.md`
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
- `docs/superpowers/specs/2026-06-04-minimum-mr-subset-b-group-two-stage-design.md`
- `docs/superpowers/plans/2026-06-04-minimum-mr-subset-b-group-two-stage-plan.md`
- `MetBench_BLL.Core/SystemMT/ImportExport/Put/README.md`
- `MetBench_SystemMT.Tests/SystemMT/ImportExport/AGroupPutImportExportTests.cs`
- Existing async tests under `MetBench_SystemMT.Tests/SystemMT/Jobs/`

External read-only evidence source:

- `/private/tmp/minimum-mr-subset`
- External commit expected by the plan: `b931b5f74d0f3f3cb704cd6fedbcb4f523cccd7f`
- P3 source path: `experiments/puts/p3_lorenz.py`
- P8 source path: `experiments/puts/p8_schrodinger.py`

If `/private/tmp/minimum-mr-subset` is missing on cloud/Linux, stop and record a blocker. Do not invent external evidence. On Windows VM, use the Windows VM task file instead of this cloud task.

## Hard Scope

Allowed in Stage 1:

- `MetBench_BLL.Core/SystemMT/ImportExport/Put/`
- `MetBench_SystemMT.Tests/SystemMT/ImportExport/`
- `docs/superpowers/plans/`
- `docs/superpowers/specs/`
- `docs/superpowers/tasks/`

Forbidden in Stage 1:

- `SUT/minimum_mr_subset_p3*`
- `SUT/minimum_mr_subset_p8*`
- `.github/governance/expected-catalog-counts.txt`
- runtime catalog manifests
- LiteDB files
- `MetBench_Client/`
- WPF/XAML

Allowed in Stage 2:

- live P3/P8 `SUT/` runtime assets
- launcher manifest/catalog/metadata changes
- runtime count whitelist updates
- launcher tests
- async job pipeline tests
- docs/status and active index updates supported by real evidence

Forbidden unless separately approved:

- WPF UI work
- Windows-only validation claims
- real OpenMC/P9 changes
- importing P1/P2/P6/P7/P10
- claiming external P3/P8 NumPy/SciPy smoke passed unless actually run in this turn

## Stage 1 Execution

Follow plan tasks 0-4 in:

```text
docs/superpowers/plans/2026-06-04-minimum-mr-subset-b-group-two-stage-plan.md
```

Use TDD:

1. Write `BGroupPutImportExportTests` first.
2. Run the focused test and confirm the expected RED failure.
3. Add `BGroupPutFixtures`.
4. Add round-trip and compatibility tests.
5. Update import/export README with honest evidence limits.

Required Stage 1 verification:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~AGroupPutImportExportTests|FullyQualifiedName~BGroupPutImportExportTests"
rtk git diff --check
```

Stage 1 acceptance:

- P3/P8 staged fixtures validate.
- P3/P8 package export/import round trips pass.
- P3/P8 detections are `Inconclusive` unless a real P3/P8 detection matrix is observed.
- No live runtime files or catalog counts change.
- Commit Stage 1:

```bash
rtk git add <stage-1-files>
rtk git commit -m "feat(systemmt): stage minimum MR subset B group imports"
```

## Stage 2 Execution

Follow plan tasks 5-9 in:

```text
docs/superpowers/plans/2026-06-04-minimum-mr-subset-b-group-two-stage-plan.md
```

Use TDD:

1. Write live launcher tests for P3/P8 MR IDs first.
2. Confirm expected RED failure due to missing live assets.
3. Add cloud-safe P3/P8 runtime assets and catalog rows.
4. Update runtime counts.
5. Write async job tests that submit real P3/P8 MR IDs through `SystemMtJobService` and execute `SystemMtJobWorker`.
6. Only modify async infrastructure if the tests prove a real generic gap.

Required Stage 2 verification:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~MinimumMrSubset"
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~SystemMtJob|FullyQualifiedName~MinimumMrSubsetBGroupAsync"
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter "SemanticCatalogBoundaryTests|Catalog_MR_id_set_equals_governance_whitelist"
rtk git diff --check
```

If a filter returns no tests, report that explicitly and run the nearest focused test names created by your implementation.

Stage 2 acceptance:

- P3/P8 live MR IDs are listed by launcher.
- P3/P8 direct launcher tests pass or skip only with explicit environment-gated reasons.
- P3/P8 async job tests pass through `SystemMtJobService -> SystemMtJobWorker -> SystemMtAsyncPipeline -> ISystemMtLauncher`.
- `SystemMtAsyncPipeline` does not gain P3/P8-specific branches.
- No Windows evidence is claimed.
- Commit Stage 2:

```bash
rtk git add <stage-2-files>
rtk git commit -m "feat(systemmt): promote minimum MR subset B group runtime"
```

## Evidence Report Required In Final Response

Report only facts you verified in the task:

- branch and final commit(s)
- changed files by stage
- exact verification commands and outcomes
- whether external P3/P8 smoke was run or blocked
- whether Stage 1 remained import-only
- whether Stage 2 validated async execution through the job pipeline
- any skipped or blocked checks with exact reason

Do not write "done", "green", "validated", or "merged" without fresh evidence from commands executed in the same task.
