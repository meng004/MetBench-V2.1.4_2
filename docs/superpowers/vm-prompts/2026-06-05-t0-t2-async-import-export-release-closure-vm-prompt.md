# T0-T2 Async Import/Export Release Closure VM Prompt

## How to start

Use this exact instruction in the Windows VM:

```text
切换到分支 t0-t2-async-import-export-pr4-wpf，读取 docs/superpowers/vm-prompts/2026-06-05-t0-t2-async-import-export-release-closure-vm-prompt.md，执行任务。
```

## Scope

Execute only the Windows/WPF task for the T0-T2 async import/export release-closure chain.

This VM task is stacked after:

- PR-1 branch: `t0-t2-async-import-export-pr1-job-model`
- PR-2 branch: `t0-t2-async-import-export-pr2-asset-jobs`
- PR-3 branch: `t0-t2-async-import-export-pr3-execution-artifacts`
- VM branch for this task: `t0-t2-async-import-export-pr4-wpf`

Do not claim the T0-T2 async import/export release closure is Controlled. That status is allowed only after PR-1..PR-4 merge, required checks pass, VM evidence is committed, docs/user-guide projection is updated, and the post-merge closure PR lands.

## Must Read First

Read these files before editing:

1. `AGENTS.md`
2. `CLAUDE.md`
3. `docs/status/current.md`
4. `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`
5. `docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-release-closure-design.md`
6. `docs/superpowers/plans/2026-06-05-t0-t2-async-import-export-release-closure-plan.md`

If any file is missing, stop and report the exact missing path.

## Preconditions

Run and record exact output:

```powershell
git status --short --branch
git log --oneline -5
dotnet --info
```

Required:

- Current branch is `t0-t2-async-import-export-pr4-wpf`.
- Worktree has no unrelated dirty files.
- `dotnet --info` succeeds.
- The branch contains the PR-3 execution-artifact export work.

If a precondition fails, stop and report blocker. Do not implement around missing base work.

## Implementation Task

Extend the WPF async job page so a user can submit and poll these operations through the existing async job pipeline:

- `RunMr`
- `RunBatch`
- `ImportAssets`
- `ExportAssets`
- `ExportExecutionArtifacts`

Expected source areas:

- `MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs`
- `MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml`
- `MetBench_Client/App.xaml.cs`
- cloud-safe source guard tests under `MetBench_SystemMT.Tests/` if wiring/text changes need guards

Preserve existing async run behavior:

- submit returns a queued job id;
- polling updates state/progress;
- refresh still works;
- cancel still works;
- failure reason remains visible;
- existing result summary remains visible.

Add only the UI needed to select operation kind and provide operation inputs:

- package root;
- staging root if needed by import;
- export root;
- execution id for execution artifact export;
- artifact path display from polling status.

Do not add result/evidence import. Result/evidence import remains out of scope because no trust model is approved.

## TDD Requirement

Before implementation, add or update at least one failing cloud-safe source/wiring guard that proves the WPF surface is wired to the new operation kinds. Then implement the minimal source change and rerun the focused tests.

Suggested focused test filter:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfAsync|FullyQualifiedName~SystemMtJob|FullyQualifiedName~ExecutionArtifact|FullyQualifiedName~PutImportExport" --logger "console;verbosity=minimal"
```

If the exact test names differ, record the adjusted command and why it is equivalent.

## Manual VM Validation

Build:

```powershell
dotnet build MetBench.sln --no-restore
```

Run the WPF app and capture real evidence:

1. Open the WPF application.
2. Navigate to the System-MT async operations page.
3. Submit a pure-stdlib single MR async run.
4. Capture queued/running/succeeded polling evidence.
5. Submit a batch run using real MR ids from the catalog.
6. Create or locate a valid PUT import package for A-group/B-group fixture data. If no shipped package exists, create one from test fixture output under `%TEMP%\metbench-async-ie\source-package\` and record the exact source path.
7. Set staging root to `%TEMP%\metbench-async-ie\staging\`.
8. Set export root to `%TEMP%\metbench-async-ie\exports\`.
9. Submit asset import and asset export jobs.
10. Confirm asset import/export produces real artifact paths, including `staging-manifest.json` and `sut-import-unit.json` where applicable.
11. Submit execution artifact export for the successful pure-stdlib MR execution from step 3. Export to `%TEMP%\metbench-async-ie\execution-export\`.
12. Confirm execution artifact export produces `manifest.json`, `execution-result.json`, and the requested report files.
13. Confirm UI remains responsive while jobs are pending/running. Do not use sleeps on the dispatcher thread or blocking waits in UI code.

Screenshots must be real VM captures and saved under:

```text
docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-vm-evidence/
```

Minimum screenshots:

- page loaded with operation selector visible;
- single MR job queued/running;
- single MR job succeeded;
- batch job terminal status;
- asset import terminal status with artifact path;
- asset export terminal status with artifact path;
- execution artifact export terminal status with artifact path;
- failure display if any operation fails during validation.

## Evidence File

Create:

```text
docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-vm-evidence/vm-summary.md
```

The summary must include:

- branch name and head commit;
- exact commands run;
- exit codes;
- test counts when available;
- WPF build result;
- job ids;
- operation kinds exercised;
- terminal states;
- artifact paths;
- screenshot filenames;
- blockers or deviations.

Do not write "passed" for any step that was not executed. Use `BLOCKED` with exact reason when needed.

## Acceptance Criteria

This VM task passes only if all are true:

- WPF build has 0 errors.
- Focused tests pass.
- The async page can submit and poll a pure-stdlib `RunMr` job.
- The async page can submit and poll a `RunBatch` job.
- The async page can submit and poll `ImportAssets` and `ExportAssets`.
- The async page can submit and poll `ExportExecutionArtifacts`.
- Real artifact files exist at the recorded paths.
- UI displays artifact paths and failure reasons.
- No result/evidence import UI or API is added.
- VM evidence summary and screenshots are committed.

## Commit and PR

After passing validation:

```powershell
git status --short
git add MetBench_Client\ViewModels\SystemMtAsyncJobViewModel.cs MetBench_Client\Views\Pages\SystemMtAsyncJobPage.xaml MetBench_Client\App.xaml.cs MetBench_SystemMT.Tests docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence
git commit -m "feat(client): expose async import export operations"
git push origin t0-t2-async-import-export-pr4-wpf
```

Create or update a PR targeting `t0-t2-async-import-export-pr3-execution-artifacts`.

PR body must state:

- Windows/WPF scope;
- cloud-safe test command and result;
- WPF build command and result;
- screenshot/evidence directory;
- artifact paths created during validation;
- explicit statement that result/evidence import is out of scope.

If validation is blocked, do not commit a fake pass. Commit only useful task/evidence updates if they are truthful and ask for the smallest unblock action.
