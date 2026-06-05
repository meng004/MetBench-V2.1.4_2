# T0-T2 Async Import/Export PR4 Review-Fix VM Prompt

> **For VM agent:** Execute this prompt on the Windows VM only. Do not run this on cloud/Linux. Record only commands that actually ran and outputs that actually appeared.

## User Instruction

切换到分支 `t0-t2-async-import-export-pr4-wpf`，读取 `docs/superpowers/vm-prompts/2026-06-06-t0-t2-async-import-export-pr4-review-fix-vm-prompt.md`，执行任务。

## Scope

This is a follow-up validation for PR #301 after code-review fixes on the WPF async job page:

- `RunBatch` result summary must distinguish completed batch operation from all MR assertions passing.
- Operation-specific input controls must be visible only for the selected operation kind.
- VM evidence must include raw precondition outputs, not only a prose summary.

Do not implement new import APIs. Do not add result/evidence import. Do not mark the T0-T2 async import/export chain as Controlled.

## Preconditions

Run these commands first from the repository root and paste their raw output into the evidence summary:

```powershell
git fetch origin
git switch t0-t2-async-import-export-pr4-wpf
git pull --ff-only origin t0-t2-async-import-export-pr4-wpf
git status --short --branch
git log --oneline -5
dotnet --info
```

Required:

- Current branch is `t0-t2-async-import-export-pr4-wpf`.
- Worktree has no unrelated dirty files.
- `dotnet --info` succeeds.
- The branch contains the PR4 review-fix commit for `SystemMtAsyncJobViewModel.cs`, `SystemMtAsyncJobPage.xaml`, and `WpfAsyncJobCancellationWiringTests.cs`.

If any precondition fails, stop and report the exact blocker. Do not edit around missing base work.

## Required Checks

Run these commands exactly:

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfAsyncJobCancellationWiringTests" --logger "console;verbosity=minimal"
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfAsync|FullyQualifiedName~SystemMtJob|FullyQualifiedName~ExecutionArtifact|FullyQualifiedName~PutImportExport" --logger "console;verbosity=minimal"
dotnet build MetBench.sln --no-restore -v:minimal
powershell -NoProfile -ExecutionPolicy Bypass -File docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence\drive-async-import-export.ps1
git diff --check
```

Expected:

- The first focused test command passes and includes the two PR4 review-fix guards.
- The wider focused test command passes.
- `dotnet build MetBench.sln` exits 0 with 0 errors.
- The UIA driver exits 0 and records real WPF jobs for `RunMr`, `RunBatch`, `ImportAssets`, `ExportAssets`, `ExportExecutionArtifacts`, plus the invalid execution-id failure case.
- `git diff --check` exits 0. LF-to-CRLF working-copy warnings are acceptable only if the command exit code is 0.

## Visibility Spot Check

While the UIA driver is running or immediately after it opens the async page, capture screenshots that show:

- `RunMr`: MR selector visible; Batch MRs/package/staging/export/execution-id inputs hidden.
- `RunBatch`: Batch MRs input visible; single-MR selector and package/export inputs hidden.
- `ImportAssets`: package root and staging root visible; export root and execution id hidden.
- `ExportAssets`: package root and export root visible; staging root and execution id hidden.
- `ExportExecutionArtifacts`: export root and execution id visible; package root and staging root hidden.

If the UIA driver cannot capture all five visibility states, use manual screenshots and label them clearly.

## Evidence Output

Update or create:

```text
docs/superpowers/specs/2026-06-05-t0-t2-async-import-export-vm-evidence/vm-summary.md
```

The summary must include:

- branch name;
- validation head commit from `git rev-parse HEAD`;
- raw output blocks for `git status --short --branch`, `git log --oneline -5`, and `dotnet --info`;
- exact commands run;
- exit codes;
- test counts when available;
- WPF build result;
- UIA driver exit code;
- job ids;
- operation kinds exercised;
- terminal states;
- artifact paths;
- screenshot filenames;
- blockers or deviations.

Do not write "passed" for any step that was not executed. Use `BLOCKED` with the exact reason when needed.

## Commit and Push

If and only if the VM checks above pass or the evidence summary truthfully records a blocker, commit the evidence update:

```powershell
git status --short
git add docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence
git commit -m "test(systemmt): refresh async import export PR4 VM evidence"
git push origin t0-t2-async-import-export-pr4-wpf
```

If there are no evidence changes to commit, do not create an empty commit. Report that no VM evidence files changed.

## Pass/Fail Criteria

This VM follow-up passes only if:

- all required checks above pass;
- raw precondition outputs are committed in `vm-summary.md`;
- visibility screenshots or clearly labeled manual screenshots cover all five operation kinds;
- the evidence summary does not claim T0-T2 chain Controlled.

This follow-up is blocked if:

- the branch cannot be checked out;
- WPF build fails;
- tests fail;
- UIA cannot launch or operate the async page;
- the VM cannot capture the required evidence.
