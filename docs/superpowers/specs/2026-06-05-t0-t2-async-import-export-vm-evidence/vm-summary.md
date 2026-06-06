# T0-T2 Async Import/Export PR4 WPF VM Summary

branch=t0-t2-async-import-export-pr4-wpf
validation_head=007c1c156a446510e35c77c22c8b3dd76688de46
evidence_commit=08d7fedee99a6e2301370b3c91bb0b97aeade73d
current_pr_head=resolve from GitHub before merge; do not copy this evidence note as a live PR-head source
origin_main=22748408e3fa01b67aecedf57b0b4cdc23e6d328

## Precondition Raw Outputs

`git fetch origin`: exit 0; no output.

`git switch t0-t2-async-import-export-pr4-wpf`: first sandboxed attempt failed with `.git/index.lock` permission denial; elevated retry exit 0:

```text
Your branch is up to date with 'origin/t0-t2-async-import-export-pr4-wpf'.
Already on 't0-t2-async-import-export-pr4-wpf'
```

`git pull --ff-only origin t0-t2-async-import-export-pr4-wpf`: first sandboxed attempt failed with `.git/FETCH_HEAD` permission denial; elevated retry exit 0:

```text
Already up to date.
From https://github.com/meng004/MetBench-V2.1.4_2
 * branch            t0-t2-async-import-export-pr4-wpf -> FETCH_HEAD
```

`git status --short --branch`: exit 0:

```text
## t0-t2-async-import-export-pr4-wpf...origin/t0-t2-async-import-export-pr4-wpf
?? .claude/settings.local.json
?? _worktrees/
?? tools/uia-verify-i18n.ps1
warning: unable to access 'C:\Users\codex/.config/git/ignore': Permission denied
warning: unable to access 'C:\Users\codex/.config/git/ignore': Permission denied
```

The untracked paths above pre-existed this PR4 review-fix validation and were not staged.
This is a recorded precondition deviation, not a hidden clean-worktree claim:

- `.claude/settings.local.json` is local agent configuration and is not referenced by the solution, WPF project, UIA driver, or evidence artifacts.
- `_worktrees/` is a local workspace container and is not referenced by `MetBench.sln`, `MetBench_Client.csproj`, the focused test filters, or `drive-async-import-export.ps1`.
- `tools/uia-verify-i18n.ps1` is an unrelated untracked helper script and was not invoked by this PR4 validation.

The VM validation therefore did not run from a pristine worktree, but the dirty paths were untracked, not staged, and outside the executed PR4 build/test/UIA evidence path. If the merge policy requires a pristine VM status output rather than a documented non-impacting deviation, rerun this prompt from a clean VM checkout before merge.

`git log --oneline -5`: exit 0:

```text
007c1c1 fix(client): refine async import export PR4 review findings
6b6ec7a docs(systemmt): clarify async import export VM validation head
fa167d3 docs(systemmt): correct async import export VM evidence head
0cada90 feat(client): expose async import export operations
4c46ec0 Merge branch 't0-t2-async-import-export-pr3-execution-artifacts' into t0-t2-async-import-export-pr4-wpf
```

`dotnet --info`: exit 0:

```text
.NET SDK:
 Version:           9.0.306
 Commit:            cc9947ca66
 Workload version:  9.0.300-manifests.abe91478
 MSBuild version:   17.14.28+09c1be848

运行时环境:
 OS Name:     Windows
 OS Version:  10.0.26200
 OS Platform: Windows
 RID:         win-arm64
 Base Path:   C:\Program Files\dotnet\sdk\9.0.306\

已安装 .NET 工作负载:
没有要显示的已安装工作负载。
配置为在安装新清单时使用 loose manifests。

Host:
  Version:      10.0.8
  Architecture: arm64
  Commit:       94ea82652c

.NET SDKs installed:
  9.0.306 [C:\Program Files\dotnet\sdk]

.NET runtimes installed:
  Microsoft.AspNetCore.App 8.0.21 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 9.0.10 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 8.0.21 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 9.0.10 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 10.0.8 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.WindowsDesktop.App 8.0.21 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
  Microsoft.WindowsDesktop.App 9.0.10 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]

Other architectures found:
  x64   [C:\Program Files\dotnet\x64]
    registered at [HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64\InstallLocation]
  x86   [C:\Program Files (x86)\dotnet]
    registered at [HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x86\InstallLocation]

Environment variables:
  Not set

global.json file:
  Not found

Learn more:
  https://aka.ms/dotnet/info

Download .NET:
  https://aka.ms/dotnet/download
```

## Commands

- `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfAsyncJobCancellationWiringTests" --logger "console;verbosity=minimal"`: exit 0; 9 passed, 0 failed, 0 skipped.
- `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfAsync|FullyQualifiedName~SystemMtJob|FullyQualifiedName~ExecutionArtifact|FullyQualifiedName~PutImportExport" --logger "console;verbosity=minimal"`: exit 0; 93 passed, 0 failed, 0 skipped.
- `dotnet build MetBench.sln --no-restore -v:minimal`: exit 0; 0 errors; console emitted existing warnings.
- `powershell -NoProfile -ExecutionPolicy Bypass -File docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence\drive-async-import-export.ps1`: exit 0; generated screenshots 01-13 and operation artifacts.
- `dotnet build MetBench_Client\MetBench_Client.csproj --no-restore -v:minimal`: exit 0; 3 warnings; 0 errors. This command is run inside the UIA driver and logged to `build-output.txt`.
- `git diff --check`: exit 0; only LF-to-CRLF working-copy warnings for evidence files.

## Review-Fix Validation

- RunBatch result summary distinguishes operation completion from MR assertion outcomes with `Batch MR assertions: total=...; passed=...; failed=...; cancelled=...; pending=...`.
- Operation-specific input visibility is UIA-asserted before screenshots 09-13 are captured. The driver fails if a required visible field is missing or if a field that should be hidden remains visible.
- No import APIs/result/evidence import path was added in this validation.
- T0-T2 release-closure chain is not marked Controlled here.

## WPF Jobs

| Operation | JobId | State | ArtifactPath |
|---|---|---|---|
| RunMr | 754b8a37-28b4-4624-a8f0-260b0bc714f7 | Succeeded | - |
| RunBatch | 5c2ba502-1a9c-4818-b8ff-0d25e6d72621 | Succeeded | - |
| ImportAssets | 07c5f067-6e90-46ee-9705-2d9c84c66b8e | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence\operation-artifacts\p4-staging\P4\202606060020022033547Z\staging-manifest.json |
| ExportAssets | ed788e52-61d9-46ee-b849-51fa472b447f | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence\operation-artifacts\p4-export\sut-import-unit.json |
| ExportExecutionArtifacts | 3bc8d56a-8eff-4cbd-b4a5-8ccc8d793f37 | Succeeded | C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence\operation-artifacts\execution-export\manifest.json |
| ExportExecutionArtifacts-missing-execution | d03c4554-c2d9-4fb3-9c67-ac95023b74e5 | Failed | - |

execution_id=50d51be1-826a-4efa-8dcf-cd2c2c83fc8b
execution_export_root=C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence\operation-artifacts\execution-export
execution_export_manifest=C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence\operation-artifacts\execution-export\manifest.json
execution_export_result=C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence\operation-artifacts\execution-export\execution-result.json
execution_export_html=C:\MetBench-V2.1.4_2\docs\superpowers\specs\2026-06-05-t0-t2-async-import-export-vm-evidence\operation-artifacts\execution-export\report.html

## Screenshots

- `01-page-operation-selector-visible.png`
- `02-runmr-queued-or-running.png`
- `03-runmr-succeeded.png`
- `04-runbatch-terminal.png`
- `05-import-assets-terminal-artifact.png`
- `06-export-assets-terminal-artifact.png`
- `07-export-execution-artifacts-terminal-artifact.png`
- `08-failure-display.png`
- `09-visibility-runmr.png`
- `10-visibility-runbatch.png`
- `11-visibility-import-assets.png`
- `12-visibility-export-assets.png`
- `13-visibility-export-execution-artifacts.png`

## Blockers

None for PR4 WPF operation exposure. Do not mark the T0-T2 release-closure chain Controlled from this PR alone.
