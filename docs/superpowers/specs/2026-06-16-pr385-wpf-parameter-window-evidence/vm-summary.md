# PR #385 WPF Parameter Window Evidence

Date: 2026-06-16
Branch: `codex/quality-follow-up-remediation`
Head: `d24832a58e9db3934d9fd7be3f29af523d3d784b`
Base checked locally: `origin/main` = `c70a0bf8108092e696ef1e96299b3f60c2fecad8`

## Commands

```powershell
dotnet build tools\uia-acceptance\UiaAcceptance.csproj -c Release --nologo -v minimal
```

Result: pass, 0 warnings, 0 errors.

```powershell
dotnet build MetBench_Client\MetBench_Client.csproj -c Release --nologo -v minimal
```

Result: pass, 0 errors. Existing warnings remain.

```powershell
$root='D:\Codes\MetBench-V2.1.4_2'
$tool=Join-Path $root 'tools\uia-acceptance\bin\Release\net8.0-windows\UiaAcceptance.exe'
$exe=Join-Path $root 'MetBench_Client\bin\Release\net8.0-windows7.0\MetBench_Client.exe'
$ev=Join-Path $root 'docs\superpowers\specs\2026-06-16-pr385-wpf-parameter-window-evidence'
& $tool --exe $exe --evidence $ev --label pr385-parameter-window --steps 'nav:Nav_ApplicationManagement;sleep:2500;assertid:DataGrid_Application;shot:01-application-management;invokename:InputParams;sleep:1000;dumpdialog:02-input-dialog-tree;dialog:Cancel;invokename:OutputParams;sleep:1000;dumpdialog:03-output-dialog-tree;dialog:Cancel;shot:04-after-close' --timeout-seconds 180
```

Result: exit 0.

## UIA Assertions

- Navigated to `Nav_ApplicationManagement`.
- Asserted `DataGrid_Application` was present.
- Invoked `InputParams` via `InvokePattern`.
- Captured the input parameter window tree; the window contained `Name | Type | Description | Constraints | IsRequired | OK | Cancel`.
- Clicked `Cancel` and returned to the main window.
- Invoked `OutputParams` via `InvokePattern`.
- Captured the output parameter window tree; the window contained `Name | Type | Description | Constraints | IsRequired | OK | Cancel`.
- Clicked `Cancel` and returned to the main window.

## Evidence Files

- `pr385-parameter-window-01-application-management.png`
- `pr385-parameter-window-02-input-dialog-tree.txt`
- `pr385-parameter-window-dialog1.png`
- `pr385-parameter-window-03-output-dialog-tree.txt`
- `pr385-parameter-window-dialog2.png`
- `pr385-parameter-window-04-after-close.png`

## Verdict

PASS. The changed `ApplicationProgramsWindow` secondary window opens for both input and output parameter commands and closes through `Cancel` without requiring `INavigationWindow`.
