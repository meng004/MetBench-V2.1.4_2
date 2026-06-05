# Windows VM Prompt: T0-T2 PR-1 Async Job Model Validation

You are Claude Code running on the Windows VM for MetBench.

## Scope

Validate PR #298 / branch `t0-t2-async-import-export-pr1-job-model`.

This PR changes WPF-hosted async worker wiring and `SystemMtAsyncJobViewModel`, so Windows evidence is required before merge. Do not implement unrelated fixes. Do not edit files unless a build/test failure is directly attributable to this PR and the smallest fix is obvious.

## Preconditions

1. You are in the MetBench repository on the Windows VM.
2. `dotnet` is available.
3. GitHub remote `origin` is available.
4. If the worktree is dirty, stop and report `git status --short`.

## Commands

Run these commands in PowerShell.

```powershell
git fetch origin
git switch t0-t2-async-import-export-pr1-job-model
git reset --hard origin/t0-t2-async-import-export-pr1-job-model
git status --short --branch
```

Expected:

- Branch is `t0-t2-async-import-export-pr1-job-model`.
- Worktree is clean.

## Build Validation

```powershell
dotnet build MetBench.sln --no-restore
```

Expected:

- Exit code 0.
- Record warning count if any.

## Focused Tests

```powershell
dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~SystemMtJob|FullyQualifiedName~RunBatchJobOperationHandler|FullyQualifiedName~RuntimePreflightAsyncJob|FullyQualifiedName~SystemMtAsyncPipeline|FullyQualifiedName~WpfAsyncJobCancellationWiringTests|FullyQualifiedName~LiteDbJobStoreTests|FullyQualifiedName~JobFacadeTypeLeakageTests|FullyQualifiedName~SystemMtLauncherBatchTests" --logger "console;verbosity=minimal"
```

Expected:

- Exit code 0.
- Test count should be at least 73 passed.
- No failed tests.

## WPF UI-Visible Smoke

Run the WPF app from Visual Studio or the command line.

Navigate to the System MT async execution page.

Validate:

1. Page opens without startup exception.
2. MR combo is populated.
3. Submit a known lightweight MR.
4. Poll log updates while the job runs.
5. Final state reaches a terminal state.
6. Result summary or failure reason is visible.
7. Cancel button remains enabled only while a job is running.

Evidence to collect:

- Screenshot of the async execution page after a completed or terminal job.
- Exact MR id used.
- Final state text.
- Poll log text, especially whether it remains readable after the ViewModel change.

Note: PR-1 does not add a UI command for RunBatch submission. Batch item visibility is cloud-safe projected into `PollLog` for future batch jobs; this VM smoke only needs to confirm the existing async page still builds, opens, submits, polls, and displays logs.

## Pass / Fail Criteria

Pass only if:

- Build exits 0.
- Focused tests exit 0.
- WPF async page smoke passes with screenshot evidence.

Fail if:

- Build fails.
- Any focused test fails.
- WPF app cannot start.
- Async page cannot submit/poll a lightweight MR.
- UI text overlaps or poll log becomes unreadable.

## Report Format

Report back with:

```text
Branch:
HEAD:
Build:
Focused tests:
WPF smoke:
MR id used:
Final state:
Screenshot path:
Changed files:
Conclusion:
Blockers:
```

Be precise. Do not claim PR #298 is Windows-validated unless all pass criteria above are met.
