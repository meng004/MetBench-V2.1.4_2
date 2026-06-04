# Async Cancel True-Interrupt Repair Plan

> Date: 2026-06-04
> Status: Completed (PR #288 merged to `origin/main` as `3ad26e57b186c68976fdfb44379ef5b90ddcfab1`)
> Owner: Codex / Windows VM follow-up
> Trigger: VM evidence from `codex/async-cancel-registry-docs-postmerge` commit `bedfe31` shows the WPF async job reaches `Cancelled`, but the underlying `advection_1d.py` process remains alive after Cancel.

## Problem

PR #285 added `IJobCancellationRegistry` and PR #286 wired it into the WPF hosted worker. The registry now trips the worker token and the durable job record reaches `Cancelled`.

The Windows VM true-interrupt probe still failed AC-3:

- `process-before-cancel.txt` contained the running `cmd.exe` and `python` processes for `advection_1d.py`.
- `process-after-cancel.txt` still contained the same process IDs after the UI Cancel action.
- `ui-state-after-cancel.txt` showed `state=Cancelled`, `phase=cancelled`, and no result.

Conclusion: durable cancellation works; process-level interruption does not.

## Root Cause Hypothesis

`MetBench_BLL.Core/SystemMT/Pipeline/DefaultProcessExecutor.cs` creates a linked timeout/user cancellation token and waits with:

```csharp
await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
```

It kills the process tree only when the linked token trips because of timeout:

```csharp
catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
```

When the user cancellation token trips, the `OperationCanceledException` escapes without calling `process.Kill(entireProcessTree: true)`. Disposing the `Process` object does not terminate the shell child tree, so the WPF job becomes `Cancelled` while the SUT keeps running.

`MetBench_BLL.Core/SystemMT/CliProgramRunner.cs` has a similar risk surface, but the failing async System MT launcher path goes through `SystemMtPipeline` and `DefaultProcessExecutor`. This plan treats `DefaultProcessExecutor` as required scope and `CliProgramRunner` as a review item: modify it in the same PR only if tests or call tracing show the same production async path can reach it.

## Scope

In scope:

- `DefaultProcessExecutor` user-cancellation process-tree interruption.
- Focused cross-platform tests proving a cancelled process is killed, not merely reported cancelled.
- Status and evidence docs that keep true-interrupt Not yet Controlled until VM proof is collected.
- Windows VM rerun of `docs/superpowers/vm-prompts/2026-06-04-async-cancel-registry-true-interrupt-vm-prompt.md`.

Out of scope:

- WPF layout or view-model changes unless the VM rerun proves the UI can no longer submit/cancel the probe.
- DAL schema changes.
- `MetBench_BLL.Core/SystemMT/Catalog/Typed/*` public predicate/runtime semantics.
- Any fake production LiteDB job or execution evidence.
- Reclassifying the async WPF consumer as fully true-cancel Controlled before AC-3 process evidence passes.

Stop if the repair would require changing typed semantic catalog public predicate/runtime semantics.

## Design

### Required behavior

`DefaultProcessExecutor.RunAsync(...)` must distinguish timeout from caller cancellation:

- Timeout: kill the entire process tree, collect available stdout/stderr, and return `ProcessResult` with `TimedOut = true` and `ExitCode = -1`.
- Caller cancellation: kill the entire process tree, drain/close output tasks without hanging, then throw `OperationCanceledException` associated with the caller token.
- Normal process exit: preserve existing stdout/stderr/exit-code behavior.

`SystemMtPipeline` already catches `OperationCanceledException` and returns `PipelineStatus.Cancelled`; `SystemMtJobWorker` already finalizes the async job as `Cancelled`. The repair should not change that status flow.

### Implementation sketch

1. Replace the current timeout-only kill catch with explicit branches:
   - catch `OperationCanceledException` when `cancellationToken.IsCancellationRequested`;
   - kill tree;
   - await stream tasks after kill;
   - rethrow or throw `new OperationCanceledException(cancellationToken)`.
2. Keep the existing timeout branch, but share a small `TryKillProcessTree(Process process)` helper.
3. Avoid a read-task hang after kill:
   - killing the process tree should close redirected streams;
   - if a platform-specific race remains, add a small internal helper to wait for stdout/stderr with a bounded drain after kill and fall back to empty/partial text only for cancellation/timeout paths.
4. Review `CliProgramRunner` after the primary fix:
   - if the async launcher path cannot reach it, leave it unchanged and register a follow-up note;
   - if a direct production path uses it for cancellable external SUTs, apply the same kill-on-user-cancel behavior with focused tests.

## TDD Plan

Add focused tests under `MetBench_SystemMT.Tests/SystemMT/Pipeline/`, for example `DefaultProcessExecutorCancellationTests.cs`.

Test 1: `RunAsync_user_cancellation_kills_process_tree`

- Create a temp pure-stdlib Python script that:
  - writes its own PID to a temp file;
  - flushes the file;
  - sleeps for 30 seconds.
- Launch it through `DefaultProcessExecutor.RunAsync` using the normal shell command path.
- Wait until the PID file exists.
- Cancel the caller token.
- Assert `OperationCanceledException`.
- Poll for up to 5 seconds and assert `Process.GetProcessById(pid)` no longer resolves to a live process.

Test 2: `RunAsync_timeout_still_kills_process_tree_and_returns_timed_out`

- Use the same PID-writing script.
- Run with a 1 second timeout and a non-cancelled caller token.
- Assert `TimedOut = true`, `ExitCode = -1`.
- Poll and assert the PID is gone.

Test 3: `RunAsync_normal_exit_preserves_output_and_exit_code`

- Keep or add a simple smoke assertion so the cancellation repair does not regress normal stdout/stderr/exit-code behavior.

If `CliProgramRunner` is changed, add a parallel focused test that proves cancellation kills its direct process.

## Verification Gates

Run before opening the repair PR:

```powershell
dotnet restore MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "DefaultProcessExecutor"
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "SystemMtJobWorker|SystemMtAsyncPipeline"
dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --no-restore --filter "SemanticCatalogBoundaryTests"
dotnet build MetBench.sln
git diff --check
```

Expected:

- focused process-executor cancellation tests pass;
- async job worker / async pipeline cancellation behavior still passes;
- semantic catalog boundary tests pass;
- Windows solution build has 0 errors;
- no whitespace errors.

## VM Acceptance

After the Core repair lands on a VM branch, rerun:

```text
docs/superpowers/vm-prompts/2026-06-04-async-cancel-registry-true-interrupt-vm-prompt.md
```

Required result:

- AC-1 build PASS with exact warning/error counts.
- AC-2 wiring present PASS.
- AC-3 true interrupt PASS: `process-before-cancel.txt` contains the selected live SUT runner process, and `process-after-cancel.txt` does not contain that process after Cancel.
- AC-4 durable state PASS: WPF shows `Cancelled`.
- AC-5 no orphan result PASS.
- AC-6 clean restore PASS.

Only after AC-3 passes may docs state "true cancel verified".

The VM probe must not use an MR whose client-visible source/follow-up values are both zero. The selected probe MR is `advection-amplitude-linearity` because the launcher E2E test `LauncherEndToEndAdvectionTests` passes and asserts `FollowUpValue > SourceValue` for factor 2. A VM preflight run on this branch also showed nonzero client-visible values: source `0.75585863737664949`, follow-up `1.511717274753299`.

## UI True-SUT Cancellation Test Case

Test case id: `VM-ASYNC-CANCEL-TRUE-SUT-PROCESS`

Purpose: prove cancellation from the WPF async page interrupts the actual SUT process, not only the durable job row.

Preconditions:

- Windows VM with WPF UIA available.
- Branch contains the Core repair and the existing WPF cancellation registry wiring:
  - `MetBench_Client/App.xaml.cs` registers singleton `IJobCancellationRegistry`.
  - `MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs` passes the registry into `SystemMtJobWorker`.
- Worktree is clean before the probe.
- Do not edit production `SUT/` files or production LiteDB data.
- Selected MR has a nonzero launcher relation: `advection-amplitude-linearity` observes `peak_amplitude`, with follow-up greater than source. If the WPF client shows source/follow-up as `0` for this MR in a completed run, treat that as a separate client evidence blocker rather than passing this cancellation test.

Probe setup:

1. Build the WPF client:

   ```powershell
   dotnet build MetBench_Client/MetBench_Client.csproj -c Debug
   ```

2. Modify only the build-output copy of the pure-stdlib advection runner:

   ```powershell
   $clientOut = 'MetBench_Client/bin/Debug/net8.0-windows7.0'
   $sutScript = Join-Path $clientOut 'SUT/advection_1d/advection_1d.py'
   Copy-Item $sutScript "$sutScript.true-cancel-backup" -Force
   $original = Get-Content $sutScript -Raw
   @"
   from __future__ import annotations
   import time
   time.sleep(30)
   $($original -replace '^from __future__ import annotations\r?\n', '')
   "@ | Set-Content $sutScript -Encoding UTF8
   ```

   This keeps the real SUT runner path and real launcher command while making the run long enough to cancel. Restore this build-output file in `finally`.

Execution steps:

1. Launch the WPF app from the built output or with `dotnet run --no-build --project MetBench_Client/MetBench_Client.csproj -c Debug`.
2. Use UIA to open `SystemMtAsyncJobPage`.
3. Use `AsyncMrCombo` to select MR `advection-amplitude-linearity`.
4. Invoke `AsyncSubmitButton`.
5. Poll `AsyncState` until it is a running state (`Running`, `Queued`, or equivalent non-terminal state) and capture `AsyncJobId`.
6. Query the process table and save `process-before-cancel.txt`:

   ```powershell
   Get-CimInstance Win32_Process |
     Where-Object { $_.CommandLine -match 'advection_1d.py' } |
     Select-Object ProcessId, CommandLine |
     Format-List
   ```

7. Assert `process-before-cancel.txt` contains at least one `advection_1d.py` process. Record the exact ProcessId values.
8. Invoke `AsyncCancelButton`.
9. Wait up to 5 seconds, polling both UI and process table.
10. Save `process-after-cancel.txt` using the same process query.

Assertions:

- `process-before-cancel.txt` contains a real `advection_1d.py` process launched by the WPF-triggered System MT job.
- `process-after-cancel.txt` does not contain any of the before-cancel ProcessId values.
- `AsyncState` reaches `Cancelled`.
- `AsyncFailureReason` contains cancellation text, for example `cancellation requested`.
- `AsyncResultSummary` is empty or explicitly indicates no result; no successful `MrRunResult` is shown for the cancelled job.
- `git status -sb` after restoring the build-output runner shows no production source edits caused by the probe.

Fail conditions:

- If UI shows `Cancelled` but any before-cancel `advection_1d.py` PID is still alive, the test fails.
- If no `advection_1d.py` process is observed before Cancel, the test is blocked rather than passed.
- If the probe modifies source `SUT/advection_1d/advection_1d.py` instead of the build-output copy, the evidence is invalid.

Evidence files:

- `build-output.txt`
- `process-before-cancel.txt`
- `process-after-cancel.txt`
- `ui-state-after-cancel.txt`
- screenshot before Cancel
- screenshot after Cancel
- screenshot or UIA dump showing no successful result
- `git-status-after-restore.txt`

## PR Shape

Preferred PR:

- title: `fix(systemmt): kill SUT process tree on async cancellation`
- production changes: `DefaultProcessExecutor` only unless `CliProgramRunner` is proven in-path;
- tests: focused process-executor cancellation facts;
- docs: this plan, active index row, status ledger correction, VM evidence README after rerun if collected in the same branch.

If VM evidence cannot be collected in the same PR, keep the status wording as "Core repair merged; Windows true-interrupt evidence pending" and open a separate evidence PR after VM rerun.

## Acceptance Checklist

- [x] Failing VM evidence is cited as a blocker, not hidden as pending.
- [x] User cancellation kills the process tree in a focused automated test.
- [x] Timeout still kills the process tree and reports `TimedOut`.
- [x] Existing cancelled durable-state behavior remains unchanged.
- [x] No typed semantic catalog public semantics are changed.
- [x] Windows VM AC-3 process-before/process-after evidence passes before Controlled wording is restored.

Closure evidence on repair commit `4b0628617e50fe2b46d51d06229dba23268576e2`:

- focused `DefaultProcessExecutor` tests: 7 passed;
- async job worker / async pipeline focused tests: 16 passed;
- semantic catalog boundary tests: 3 passed;
- Windows solution build: 0 errors;
- VM UI true-interrupt probe: `docs/superpowers/specs/2026-06-04-async-cancel-registry-vm-verification/README.md`, `test-summary.txt` shows `result=PASS`, `before_pid=5860`, `after_pid_alive=False`, `ui_cancelled=True`.
