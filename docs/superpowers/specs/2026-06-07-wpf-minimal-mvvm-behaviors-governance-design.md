# WPF Minimal MVVM and Behaviors Governance Design

Date: 2026-06-07
Branch: codex/wpf-minimal-mvvm-behaviors
Base: origin/main c01de218404546d3379ebe03d8358374e768eabb

## Scope

This first phase defines the final-state governance architecture for the MetBench WPF client and validates one small VM slice. It does not rewrite the WPF shell, remove all packages, or change MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics.

The requested final state intentionally supersedes the older WPF stack convention in CLAUDE.md. That file still lists Wpf.Ui, LiveChartsCore.SkiaSharpView.WPF, and Microsoft.Web.WebView2 as accepted WPF stack elements. This governance target therefore records: governance target overrides older convention. A later synchronization PR must update CLAUDE.md and AGENTS.md after the phased migration has concrete evidence.

Superpowers workflow used for this phase:

- Brainstorming outcome: converge on a narrow PR-0 design plus one reversible XAML Behaviors spike, not a full UI rewrite.
- Writing-plans outcome: split the dependency reduction into separate VM-verifiable PRs with explicit rollback gates.

## Inventory

Inventory commands:

```powershell
rg -n "PackageReference|Wpf.Ui|WPF-UI|Stylet|Prism|PropertyChanged.Fody|LiveCharts|SkiaSharp|WebView2|HandyControl|Xaml.Behaviors|CommunityToolkit" MetBench_Client MetBench_Client.Tests
rg -n "xmlns:(ui|s|hc|i)=|SymbolIcon|INavigationService|INavigableView|NavigationView|ThemeService|TaskBarService|EventToCommand|ActionTarget|WebView2|CartesianChart|PieChart" MetBench_Client
```

| Package or surface | Current evidence | Current purpose | Final-state replacement | VM risk |
|---|---:|---|---|---|
| CommunityToolkit.Mvvm | 30 files | ObservableObject and existing command/property source generators | Keep as the only MVVM stack | Low |
| Microsoft.Xaml.Behaviors.Wpf | 0 files before this spike | Missing final event-binding stack | Keep as the only XAML event-to-command stack | Low |
| Wpf.Ui / WPF-UI / WPF-UI.Tray | Wpf.Ui: 64 files; WPF-UI packages in csproj | Shell, navigation controls, theme helpers, icons, message boxes, pages, tray | Native WPF Frame/Page, ResourceDictionary, built-in controls, own DialogService, optional own tray adapter only if required | High |
| Stylet.Start | 17 files | Legacy action binding and action target routing | CommunityToolkit RelayCommand plus Microsoft.Xaml.Behaviors InvokeCommandAction | Medium |
| Prism.Wpf | 3 files | Package plus dead Prism.Common usings in page code-behind | Remove after dead using check | Low |
| PropertyChanged.Fody | Package ref plus FodyWeavers.xml PropertyChanged | Compile-time property notification weaving | Explicit ObservableObject/SetProperty or generated observable properties | Medium |
| LiveChartsCore.* | 18 files | Dashboard and execution charts | First degrade to data tables and text summaries; later self-rendered WPF visuals only if needed | High |
| SkiaSharp.* | 16 files | LiveCharts rendering backend | Removed with LiveCharts path | High |
| Microsoft.Web.WebView2 | 3 files | MT report preview hosting | Export/open file path plus external viewer; no in-app HTML/PDF host in final state | Medium |
| HandyControl | 0 files | Not present in current WPF client | Guard as banned unless a future explicit exception is approved | Low |
| Microsoft.Extensions.Hosting, LiteDB, FluentValidation | csproj package refs | App hosting, local persistence, validation | Not classified as UI framework dependencies in this plan | Low |

## Final State Definition

- MVVM: CommunityToolkit.Mvvm only.
- XAML event binding: Microsoft.Xaml.Behaviors.Wpf only.
- Navigation: WPF native Frame/Page plus a small MetBench-owned NavigationService. Wpf.Ui navigation abstractions leave the runtime path.
- Theme and icons: WPF ResourceDictionary, built-in controls, text labels, and project-owned lightweight styles. Wpf.Ui theme/icon dependencies leave the runtime path.
- Dialogs: WPF MessageBox or a project-owned DialogService. Wpf.Ui MessageBox leaves the runtime path.
- Charts: this governance line does not keep LiveCharts as a final dependency. The first replacement is table/text summaries. A later PR may add project-owned WPF drawing if screenshots prove the table/text route is insufficient.
- HTML/PDF preview: this governance line does not keep WebView2 as a final dependency. The replacement is explicit file path, export, and external-open affordances.
- BLL/Core/DAL runtime semantics: unchanged by this governance line.

## VM And Cloud Split

VM responsibilities:

- WPF build, XAML compilation, package removal, page navigation, UIA, and screenshots.
- Any migration that changes XAML, WPF code-behind, WPF view models, app startup, message boxes, charts, WebView2, navigation, theme resources, or tray behavior.

Cloud responsibilities:

- Documentation, non-WPF guard tests, PR gate updates, and core tests that do not require WPF visuals.
- Analyzer or grep guard additions that block reintroduction of banned UI dependencies after VM evidence proves a removal phase.

## Phased PR Plan

### PR-0: Docs/design plus minimal Behaviors spike

Changes:

- Add this design document.
- Add the bite-sized implementation plan.
- Add Microsoft.Xaml.Behaviors.Wpf.
- Migrate one low-risk Stylet action binding: MT Report Generator export button to RelayCommand plus InvokeCommandAction.

Acceptance commands:

```powershell
dotnet restore MetBench.sln
dotnet build MetBench.sln --no-restore
dotnet test MetBench_Client.Tests --no-build
dotnet test MetBench_SystemMT.Tests --no-build --filter "ClientI18n|SystemMtExplanation|SystemMtPairQuality"
git diff --check
```

Screenshot requirements:

- Main window startup.
- MT Report Generator page with the migrated export button.
- Visible result after clicking the migrated export command.

Rollback condition:

- Revert the three spike files if XAML Behaviors cannot restore/build on the VM.

### PR-1: Stylet, Prism, and Fody binding convergence

Changes:

- Remove dead Prism usings and Prism.Wpf package.
- Migrate remaining Stylet action bindings one page at a time.
- Replace Fody-dependent view-model properties with explicit ObservableObject patterns.

Acceptance:

- VM WPF build has 0 errors.
- Each migrated page has a screenshot of at least one command path.
- `rg "s:Action|s:View.ActionTarget|Stylet|Prism|PropertyChanged.Fody" MetBench_Client` output is either empty or explicitly justified.

Rollback:

- Revert the last page-level migration if page navigation or command behavior regresses.

### PR-2: Wpf.Ui navigation, theme, controls, and dialogs

Changes:

- Introduce native Frame/Page shell and project-owned NavigationService.
- Replace Wpf.Ui dialogs with MessageBox or DialogService.
- Replace Wpf.Ui icons/theme helpers with ResourceDictionary and built-in controls.

Acceptance:

- VM screenshots for main shell, navigation, settings, at least one System MT page, and one dialog.
- Client i18n tests pass.
- No Wpf.Ui package reference remains unless a specific blocker is recorded in the PR body.

Rollback:

- Keep Wpf.Ui package until all shell and dialog screenshots pass.

### PR-3: LiveCharts and WebView2 display replacement

Changes:

- Replace chart surfaces with data tables/text summaries or project-owned WPF drawing.
- Replace report preview with explicit file path, export, and external-open behavior.

Acceptance:

- Screenshots for coverage/execution chart replacement and report export/open replacement.
- `rg "LiveCharts|SkiaSharp|WebView2" MetBench_Client` has no runtime use, or the PR body records a blocker.

Rollback:

- Revert the affected surface if users lose access to the underlying data or export workflow.

### PR-4: Dependency closure, guards, and convention sync

Changes:

- Remove final unused UI packages from csproj.
- Add a guard that blocks banned WPF UI framework dependencies.
- Update CLAUDE.md and AGENTS.md to match the proven final stack.

Acceptance:

- VM WPF build 0 errors.
- Guard fails on reintroduced banned package names.
- CLAUDE.md no longer advertises Wpf.Ui, LiveCharts, SkiaSharp, WebView2, Stylet, Prism, or Fody as accepted WPF runtime stack dependencies.

Rollback:

- If a guard blocks required business packages, narrow the guard to WPF UI dependency names only.

## PR-0 Spike Boundary

The original PR-0 spike changed only the MT Report Generator export button and proved that a WPF page can use the target event-binding stack without changing BLL/Core/DAL semantics.

This branch now also contains the follow-up PR-1 / PR-2a convergence slice:

- Stylet.Start, Prism.Wpf, PropertyChanged.Fody, and WPF-UI.Tray are removed from the client project.
- Legacy Stylet action bindings are migrated to CommunityToolkit.Mvvm RelayCommand plus Microsoft.Xaml.Behaviors.Wpf where event binding is required.
- Wpf.Ui, WebView2, LiveChartsCore.*, and SkiaSharp.* remain intentionally present for later, separately verified PRs.
