# WPF Minimal MVVM and Behaviors Governance Plan

Date: 2026-06-07
Status: Active scoped plan for PR-0 and staged follow-ups
Design: docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-governance-design.md

## Goal

Move the MetBench WPF client toward a final architecture that keeps CommunityToolkit.Mvvm for MVVM and Microsoft.Xaml.Behaviors.Wpf for XAML event-to-command binding, while removing Wpf.Ui, WPF-UI.Tray, Stylet, Prism.Wpf, PropertyChanged.Fody, LiveChartsCore.*, SkiaSharp.*, Microsoft.Web.WebView2, and any HandyControl usage through staged VM-verifiable PRs.

## Task 0: Baseline

Files:

- AGENTS.md
- docs/status/current.md
- docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
- CLAUDE.md
- MetBench_Client/MetBench_Client.csproj

Commands:

```powershell
git fetch origin
git status --short --branch
git rev-parse origin/main
git switch -c codex/wpf-minimal-mvvm-behaviors origin/main
type AGENTS.md
type docs\status\current.md
type docs\superpowers\plans\2026-05-25-metbench-active-plan-index.md
type CLAUDE.md
type MetBench_Client\MetBench_Client.csproj
```

Expected output:

- Branch starts from current origin/main.
- Worktree has no tracked local modifications before PR-0 edits.
- Current WPF stack and active-plan context are recorded.

## Task 1: Dependency Inventory

Files:

- MetBench_Client/MetBench_Client.csproj
- MetBench_Client/
- MetBench_Client.Tests/

Commands:

```powershell
rg -n "PackageReference|Wpf.Ui|WPF-UI|Stylet|Prism|PropertyChanged.Fody|LiveCharts|SkiaSharp|WebView2|HandyControl|Xaml.Behaviors|CommunityToolkit" MetBench_Client MetBench_Client.Tests
rg -n "xmlns:(ui|s|hc|i)=|SymbolIcon|INavigationService|INavigableView|NavigationView|ThemeService|TaskBarService|EventToCommand|ActionTarget|WebView2|CartesianChart|PieChart" MetBench_Client
```

Expected output:

- CommunityToolkit.Mvvm is already in use.
- Microsoft.Xaml.Behaviors.Wpf is absent before PR-0 spike.
- Wpf.Ui, Stylet, LiveCharts, SkiaSharp, WebView2, Prism, and Fody have concrete use or package evidence.
- HandyControl has no current use.

## Task 2: PR-0 Design Documents

Files:

- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-governance-design.md
- docs/superpowers/plans/2026-06-07-wpf-minimal-mvvm-behaviors-governance-plan.md

Command:

- Search both new documents for unresolved marker words before PR submission.

Expected output:

- No matches.
- Design includes inventory, final-state definition, VM/cloud split, phased PR plan, acceptance commands, screenshots, and rollback gates.

## Task 3: Minimal Behaviors Spike

Files:

- MetBench_Client/MetBench_Client.csproj
- MetBench_Client/ViewModels/MTReportGeneratorViewModel.cs
- MetBench_Client/Views/Pages/MTReportGeneratorPage.xaml

Steps:

1. Add Microsoft.Xaml.Behaviors.Wpf package reference.
2. Make MTReportGeneratorViewModel partial.
3. Add a RelayCommand wrapper that calls the existing export method.
4. Replace only the export button's Stylet action binding with Interaction.Triggers and InvokeCommandAction.
5. Keep Stylet package and the remaining Stylet bindings because other pages still use them.

Expected output:

- XAML compiles.
- Clicking the export button still reaches the existing export behavior.
- No MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics change.

## Task 4: Build And Test Evidence

Evidence file:

- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/build-and-test.log

Commands:

```powershell
dotnet restore MetBench.sln
dotnet build MetBench.sln --no-restore
dotnet test MetBench_Client.Tests --no-build
dotnet test MetBench_SystemMT.Tests --no-build --filter "ClientI18n|SystemMtExplanation|SystemMtPairQuality"
git diff --check
```

Expected output:

- Restore succeeds.
- Solution build has 0 errors.
- Focused client and System MT tests pass.
- Diff whitespace check exits 0.

Failure action:

- If restore cannot reach NuGet for the new package, record the exact network failure and do not claim VM evidence.
- If WPF build fails because of the spike, revert only the three spike files and keep docs as PR-0 design evidence.

## Task 5: VM Screenshot Evidence

Evidence directory:

- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/

Screenshot files:

- 01-main-window-startup.png
- 02-mt-report-generator-behavior-page.png
- 03-export-command-empty-file-dialog.png

Steps:

1. Launch MetBench_Client from the built output.
2. Capture the main window after startup.
3. Navigate to MT Report Generator.
4. Capture the page containing the migrated export button.
5. Click the export button with no target report file selected.
6. Capture the visible message proving the migrated command reached the existing export handler.

Expected output:

- Screenshots exist and show real WPF UI, not mocked content.
- The third screenshot shows the user-visible state after command execution.

## Task 6: VM Receipt

File:

- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/README.md

Required content:

- Branch name.
- Base commit from `git rev-parse origin/main`.
- Head commit from `git rev-parse HEAD`.
- Modified files.
- Dependency inventory summary.
- Build/test commands and results.
- Screenshot list.
- Incomplete items and blockers.
- Windows classification.

Expected output:

- Receipt is factual and does not mark the dependency-removal program complete.

## Task 7: PR Gate

Commands:

```powershell
git status --short --branch
git diff --check
```

Expected output:

- Only PR-0 design, spike, and VM evidence files are modified.
- Existing unrelated untracked local files are not committed.
- PR body states that Wpf.Ui, Stylet, LiveCharts, SkiaSharp, WebView2, Prism, and Fody are still present after PR-0 by design.
