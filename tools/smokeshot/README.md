# smokeshot — MetBench UIA driver

UIA-based screenshot smoke automation for `MetBench_Client.exe`. Implements
the 10-step smoke flow described in
[`docs/superpowers/plans/2026-05-15-v2.1-followup-pipeline.md`](../../docs/superpowers/plans/2026-05-15-v2.1-followup-pipeline.md)
§PR-VM-5 (F8).

## Build

```powershell
dotnet build tools\smokeshot\smokeshot.csproj
```

Produces `bin\Debug\net8.0-windows\smokeshot.exe`. Windows-only — depends on
`System.Windows.Automation` (UIA) and the WPF visual tree of MetBench_Client.

## CLI

```text
smokeshot nav-all [--out DIR]            5-page navigation loop (PR #29 behavior)
smokeshot app-add <sutName> [--out DIR]  Step 1: Application Management add SUT
smokeshot mr-add  <mrCode>  [--out DIR]  Step 2: MR Management add MR
smokeshot mt-exec [--out DIR]            Step 3: MT execution (skips if no OpenMOC)
smokeshot metapatterns [--out DIR]       MetaPatterns CRUD: list / page2 / toggle
smokeshot debug                          Dump named UIA tree of running app
```

Exit codes: `0` ok, `1` fail, `2` skipped (env not ready), `3` app not running.

`--out DIR` defaults to `docs/screenshots/v2-ship-2026-05-14/`.

## Orchestration

```powershell
.\tools\smokeshot\run_full_smoke.ps1
```

Launches the WPF client, runs every flow, writes screenshots + `smoke-summary.md`,
then stops the app. Use `-KeepRunning` to leave it open for manual inspection.

## Design

| File | Role |
|------|------|
| `Program.cs`     | CLI dispatch, app handle resolution, focus + dismiss-dialogs |
| `UiaHelpers.cs`  | Reusable UIA primitives: Find/Click/SetValue/Wait/Screenshot |
| `Flows.cs`       | One static method per smoke flow (NavAll / AppAdd / MrAdd / MtExec / MetaPatterns / Debug) |
| `run_full_smoke.ps1` | Orchestrator: launch app → invoke each flow → Markdown summary |

Adding a new flow:
1. Add a static method `public static int MyFlow(IntPtr hwnd, AutomationElement app, string outDir)` to `Flows.cs`. Return `0` on success.
2. Add a `case` to `Program.Main` switch.
3. (Optional) Add a `Run-Flow` line to `run_full_smoke.ps1`.

## Why some flows are partial / skipped

| Flow | Status | Reason |
|------|--------|--------|
| `nav-all` | Full | Page-level navigation only; no form-fill needed |
| `app-add` / `mr-add` | Partial (nav + capture) | Form-field automation needs `AutomationProperties.Name` on the WPF `<ui:TextBox>` elements; a tiny follow-up to make Add fully scripted |
| `mt-exec` | Gated | Needs `$env:METBENCH_OPENMOC_PYTHON` pointing at a Python with `import openmoc` working; otherwise exits with code 2 (skip) |
| `metapatterns` | 2 of 3 shots | List + Page-2 work; `smoke-meta-03-toggle.png` fails because WPF DataGrid row `SelectionItemPattern.Select` returns false on Wpf.Ui themed grids — needs follow-up to either click row via mouse coord OR add `AutomationProperties.IsRequiredForForm` to the row template. Depends on `MetaPatternsPage` (PR #39) being merged to `main`; works against any branch that has it |

The chosen design favors **clean skips over false-green** — Step 3 captures a partial
screenshot and exits 2, so the orchestrator's Markdown summary distinguishes "skipped because env not ready"
from "failed because UIA couldn't find an element".

## Known issues (per memory `feedback-uia-automation`)

- Use `AutomationElement.FromHandle(hwnd)`, not `FindFirst(ProcessIdProperty)` — the latter returns the wrong root on some WPF apps.
- Always `DismissDialogs(pid, hwnd)` before the first click — leftover modals swallow events.
- Wpf.Ui `NavigationViewItem` is `ControlType.DataItem`, not `ListItem` or `Group`.
- DPI scaling: prefer `InvokePattern.Invoke` over `mouse_event` for clicks; the latter requires physical-vs-logical coordinate conversion that breaks on multi-monitor setups.
