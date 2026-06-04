# Windows VM Prompt: Async Cancellation Registry True-Interrupt Verification

User instruction to run this task:

```text
切换到分支 codex/async-cancel-registry-docs-postmerge，读取 docs/superpowers/vm-prompts/2026-06-04-async-cancel-registry-true-interrupt-vm-prompt.md，执行任务。
```

## Scope

Verify the Windows/WPF side of PR #286 after the cloud-side source wiring landed:

- `MetBench_Client/App.xaml.cs` registers singleton `IJobCancellationRegistry`.
- `MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs` passes that registry to `SystemMtJobWorker`.
- The async WPF page can cancel a running System MT job in a way that interrupts the underlying SUT process, not only flips the durable job record to `Cancelled`.

Do not change production code in this VM task. If temporary local changes are needed to make a SUT long-running, apply them only to build-output files and restore/delete them before reporting.

## Preconditions

Stop and report the exact blocker if any precondition fails.

1. Use Windows VM with .NET SDK and the existing MetBench WPF validation environment.
2. Fetch the branch and switch to it:

   ```powershell
   git fetch origin
   git switch codex/async-cancel-registry-docs-postmerge
   ```

   If the local branch does not exist:

   ```powershell
   git switch -c codex/async-cancel-registry-docs-postmerge --track origin/codex/async-cancel-registry-docs-postmerge
   ```

3. Confirm the branch contains PR #286 wiring:

   ```powershell
   Select-String -Path MetBench_Client\App.xaml.cs -Pattern 'AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>'
   Select-String -Path MetBench_Client\Hosting\SystemMtJobWorkerHostedService.cs -Pattern 'new SystemMtJobWorker\(_store, pipeline, _cancellation\)'
   ```

4. Confirm no unrelated local edits before starting:

   ```powershell
   git status -sb
   ```

## Core Steps

### 1. Build WPF client

Run:

```powershell
$evidence = 'docs/superpowers/specs/2026-06-04-async-cancel-registry-vm-verification'
New-Item -ItemType Directory -Force $evidence | Out-Null
dotnet build MetBench_Client/MetBench_Client.csproj -c Debug *> "$evidence/build-output.txt"
Get-Content "$evidence/build-output.txt" -Tail 40
```

Acceptance for this step:

- Record exact error/warning counts from `build-output.txt`.
- If build has any error, stop. Do not continue to UI verification.

### 2. Prepare a long-running pure-stdlib SUT probe

Use the build-output copy, not the source copy, so the branch remains code-clean.

```powershell
$clientOut = 'MetBench_Client/bin/Debug/net8.0-windows7.0'
$sutScript = Join-Path $clientOut 'SUT/advection_1d/advection_1d.py'
if (!(Test-Path $sutScript)) {
  'Missing build-output SUT script: ' + $sutScript | Tee-Object "$evidence/missing-sut-script.txt"
  exit 1
}

Copy-Item $sutScript "$sutScript.pr286-vm-backup" -Force
$original = Get-Content $sutScript -Raw
@"
import time
time.sleep(30)
$original
"@ | Set-Content $sutScript -Encoding UTF8
```

This is a local runtime probe only. It intentionally makes `advection-amplitude-linearity` long enough to cancel. Do not commit the modified build-output script.

### 3. Run WPF and submit a cancellable async job

Run:

```powershell
dotnet run --no-build --project MetBench_Client/MetBench_Client.csproj -c Debug
```

Keep this WPF process open. Use a second PowerShell window for the process-evidence commands below.

In the WPF UI:

1. Open the System MT async job page.
2. Select MR `advection-amplitude-linearity`.
3. Submit the job.
4. While the job is running, capture process evidence:

   ```powershell
   Get-CimInstance Win32_Process |
     Where-Object { $_.CommandLine -match 'advection_1d.py' } |
     Select-Object ProcessId, CommandLine |
     Format-List | Tee-Object "$evidence/process-before-cancel.txt"
   ```

5. Click Cancel.
6. Wait 3-5 seconds.
7. Capture after-cancel process evidence:

   ```powershell
   Get-CimInstance Win32_Process |
     Where-Object { $_.CommandLine -match 'advection_1d.py' } |
     Select-Object ProcessId, CommandLine |
     Format-List | Tee-Object "$evidence/process-after-cancel.txt"
   ```

8. Capture screenshots showing:
   - job running before cancel,
   - Cancelled state after cancel,
   - no successful result displayed for the cancelled job.

Save screenshots into:

```text
docs/superpowers/specs/2026-06-04-async-cancel-registry-vm-verification/
```

### 4. Restore temporary probe file

After UI verification:

```powershell
Copy-Item "$sutScript.pr286-vm-backup" $sutScript -Force
Remove-Item "$sutScript.pr286-vm-backup" -Force
git status -sb | Tee-Object "$evidence/git-status-after-restore.txt"
```

The source worktree should not show production code edits from the temporary probe.

## Acceptance Criteria

Report each item as PASS / FAIL / BLOCKED with file-backed evidence.

- **AC-1 Build:** `dotnet build MetBench_Client/MetBench_Client.csproj -c Debug` completes with exact warning/error counts recorded in `build-output.txt`.
- **AC-2 Wiring present:** both `Select-String` checks for registry registration and worker constructor pass.
- **AC-3 True interrupt:** `process-before-cancel.txt` contains an `advection_1d.py` process, and `process-after-cancel.txt` shows that process gone after Cancel. If the process remains, mark FAIL even if UI says `Cancelled`.
- **AC-4 Durable state:** WPF shows the job reaches `Cancelled`.
- **AC-5 No orphan result:** the cancelled job does not show a successful `MrRunResult`.
- **AC-6 Clean restore:** `git status -sb` after restoring the build-output script shows no unintended production source edits.

## Required VM Report

Write a concise VM report in the task response and, if committing evidence, in:

```text
docs/superpowers/specs/2026-06-04-async-cancel-registry-vm-verification/README.md
```

The report must include:

- branch name and commit SHA tested,
- build command and exact warning/error counts,
- each AC result with evidence filenames,
- explicit statement whether AC-3 proved process-level interruption,
- any blocker or substitution used.

Do not state "true cancel verified" unless AC-3 has process-before/process-after evidence showing the SUT process disappeared after Cancel.
