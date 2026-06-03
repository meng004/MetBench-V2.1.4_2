# Async Execution VM Verification

Date: 2026-06-03

Branch: `claude/async-execution-vm`

Base: `origin/main` at `1c7b6e4`

Runner: Windows VM, native PowerShell. `rtk` was not available in this VM.

## Result

Status: Partially verified, not completed.

The WPF async execution page is implemented and VM-visible. Real UI Automation evidence covers page load, async submit, queued job id, polling to success, manual refresh, success result, and cancel command visibility. AC-V5 remains blocked because all dependency-sensitive failure candidates completed successfully on this VM; no real Failed/TimedOut/ArtifactMissing terminal state was available without modifying runtime assets or faking execution state.

## Screenshots

| File | Evidence |
| --- | --- |
| `01-build-output.png` | WPF client build output tail, 0 errors. |
| `02-async-page-loaded.png` | `System MT Async Execution` page loaded from WPF navigation. |
| `03-submit-immediate.png` | Submit creates a real job id and shows queued state immediately after `SubmitAsync`. |
| `04-polling-progress.png` | Automatic polling reaches terminal success for `advection-amplitude-linearity`. |
| `05-manual-refresh.png` | Manual refresh button updates the selected job state. |
| `06-succeeded-result.png` | Successful async result summary uses real `MrRunResult` fields. |
| `07-failure-running.png` | Failure-candidate attempts were launched, but they did not fail on this VM. |
| `09-cancel-before.png` | Cancel flow before invoking Cancel. |
| `10-cancelled.png` | Cancel command returns, reads the store, and the UI shows the real Cancelled terminal state. |

No `08-failed-result.png` is present because no real failed async job was produced. See `failure-path-blocked.txt` and `observed-status-sequence.txt`.

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
| AC-V2 submit and queued job id | Pass | `03-submit-immediate.png`; `observed-status-sequence.txt` includes `state=Queued phase=queued progress=0` with job id `3f6dd1db-1265-4db8-b417-42fff745fa62`. |
| AC-V3 polling success | Pass | `04-polling-progress.png`, `06-succeeded-result.png`; status sequence reaches `Succeeded`. |
| AC-V4 manual refresh | Pass | `05-manual-refresh.png`; status sequence includes `[manual-refresh] state=Succeeded`. |
| AC-V5 failure state | Blocked | `failure-path-blocked.txt`; all attempted candidates reached `Succeeded`: `openmc-pincell-particle-count-convergence`, `openmc-pincell-nu-sigma-f`, `scipy-bvp-poisson-seed-mesh-insensitivity`, `scipy-ivp-lv-step-convergence`, `openmoc-pincell-ray-track-convergence`. |
| AC-V6 no UI blocking waits | Pass | `Select-String ... '\.Result|\.Wait\('` returned no matches. |
| AC-V7 no Core/DAL changes | Pass | `git diff origin/main -- MetBench_BLL.Core MetBench_DAL` returned no diff. |
| AC-V8 cancel command | Pass | `10-cancelled.png`; status sequence includes `[cancel-clicked] state=Cancelled phase=cancelled progress=40` for job id `6d28323c-6486-459d-b937-a8c124d5e45d` as read back from the job store. |

## Notes

- The UI changes are limited to WPF client composition, navigation, page/view model, localization, and a hosted worker bridge.
- `ISystemMtLauncher` remains scoped. The WPF hosted service creates a service scope per dequeued job and constructs the existing async pipeline inside that scope.
- The plan and active index are not marked completed because AC-V5 is still blocked.
