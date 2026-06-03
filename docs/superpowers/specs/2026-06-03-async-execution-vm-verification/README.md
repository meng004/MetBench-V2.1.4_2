# Async Execution VM Verification

Date: 2026-06-03

Branch: `claude/async-execution-vm`

Base: `origin/main` at `1c7b6e4`

Runner: Windows VM, native PowerShell. `rtk` was not available in this VM.

## Result

Status: Verified on VM for AC-V1 through AC-V8; pending PR merge.

The WPF async execution page is implemented and VM-visible. Real UI Automation evidence covers page load, async submit, queued job id, polling to success, manual refresh, success result, a real job-level Failed terminal state, and cancel command visibility.

The failure-state evidence uses a recoverable VM runtime trigger: the UIA driver temporarily hides the build-output copy of `SUT/advection_1d/sample/standard.json`, submits the existing `advection-amplitude-linearity` MR through the WPF async page, waits for the real launcher/worker path to mark the job `Failed`, captures `08-failed-result.png`, and restores the sample file in `finally`. No production LiteDB row was hand-written and no fake job state was inserted.

## Screenshots

| File | Evidence |
| --- | --- |
| `01-build-output.png` | WPF client build output tail, 0 errors. |
| `02-async-page-loaded.png` | `System MT Async Execution` page loaded from WPF navigation. |
| `03-submit-immediate.png` | Submit creates a real job id and shows queued state immediately after `SubmitAsync`. |
| `04-polling-progress.png` | Automatic polling reaches terminal success for `advection-amplitude-linearity`. |
| `05-manual-refresh.png` | Manual refresh button updates the selected job state. |
| `06-succeeded-result.png` | Successful async result summary uses real `MrRunResult` fields. |
| `07-failure-running.png` | Dependency-sensitive failure-candidate attempts were launched; they still succeeded on this VM and are logged for audit context. |
| `08-failed-result.png` | Real `Failed` terminal job state after the build-output sample case was temporarily hidden and restored by the UIA driver. |
| `09-cancel-before.png` | Cancel flow before invoking Cancel. |
| `10-cancelled.png` | Cancel command returns, reads the store, and the UI shows the real Cancelled terminal state. |

`failure-path-blocked.txt` was removed after the real failure-state evidence was captured. See `observed-status-sequence.txt` for the exact job ids and state transitions.

## Commands

```powershell
git fetch origin
git checkout -b claude/async-execution-vm origin/main
git rev-parse HEAD
dotnet build MetBench_Client\MetBench_Client.csproj -v:quiet
powershell -NoProfile -ExecutionPolicy Bypass -File docs\superpowers\specs\2026-06-03-async-execution-vm-verification\drive.ps1
dotnet build MetBench_Client\MetBench_Client.csproj --no-restore -v:quiet
Select-String -Path MetBench_Client\ViewModels\SystemMtAsyncJobViewModel.cs,MetBench_Client\Hosting\*.cs -Pattern '\.Result|\.Wait\('
git diff origin/main -- MetBench_BLL.Core MetBench_DAL
git diff --check
```

## Verification Summary

| AC | Status | Evidence |
| --- | --- | --- |
| AC-V1 build | Pass | `dotnet build MetBench_Client\MetBench_Client.csproj --no-restore -v:quiet` returned exit 0; latest UIA build tail in `build-output.txt` shows 6 warnings / 0 errors. |
| AC-V2 submit and queued job id | Pass | `03-submit-immediate.png`; `observed-status-sequence.txt` includes submit job id `91cb2f56-2528-4ba1-a073-02785eafa7ea`. |
| AC-V3 polling success | Pass | `04-polling-progress.png`, `06-succeeded-result.png`; status sequence reaches `Succeeded`. |
| AC-V4 manual refresh | Pass | `05-manual-refresh.png`; status sequence includes `[manual-refresh] state=Succeeded`. |
| AC-V5 failure state | Pass | `08-failed-result.png`; `observed-status-sequence.txt` records job `e98cd8ad-9b95-4c0f-a0b6-fcf9c4e727fd` reaching `Failed / failed / 40%` with failure reason `MR 'advection-amplitude-linearity' sample case not found at ...\SUT\advection_1d\sample\standard.json`. |
| AC-V6 no UI blocking waits | Pass | `Select-String ... '\.Result|\.Wait\('` returned no matches. |
| AC-V7 no Core/DAL changes | Pass | `git diff origin/main -- MetBench_BLL.Core MetBench_DAL` returned no diff. |
| AC-V8 cancel command | Pass | `10-cancelled.png`; status sequence includes `[cancel-clicked] state=Cancelled phase=cancelled progress=40` for job id `071d247a-efcf-4b47-be76-8a8d93e372b1` as read back from the job store. |

## Notes

- The UI changes are limited to WPF client composition, navigation, page/view model, localization, and a hosted worker bridge.
- `ISystemMtLauncher` remains scoped. The WPF hosted service creates a service scope per dequeued job and constructs the existing async pipeline inside that scope.
- The build-output sample case used for the failure trigger was restored; `Test-Path MetBench_Client\bin\Debug\net8.0-windows7.0\SUT\advection_1d\sample\standard.json` returned `True`, and the `.vm-hidden` backup no longer exists.
- This evidence is branch-local until PR #280 lands; mainline status should only become Controlled after merge.
