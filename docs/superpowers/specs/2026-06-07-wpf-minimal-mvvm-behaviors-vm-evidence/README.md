# WPF Minimal MVVM Behaviors VM Evidence

Date: 2026-06-07
Windows classification: VM evidence collected

## Branch

- Branch: codex/wpf-minimal-mvvm-behaviors
- Base commit: c01de218404546d3379ebe03d8358374e768eabb
- Head commit at evidence collection: c01de218404546d3379ebe03d8358374e768eabb
- Worktree state at evidence collection: dirty with PR-0 governance docs, one minimal WPF Behaviors spike, and evidence files.

## Modified Files

- MetBench_Client/MetBench_Client.csproj
- MetBench_Client/ViewModels/MTReportGeneratorViewModel.cs
- MetBench_Client/Views/Pages/MTReportGeneratorPage.xaml
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-governance-design.md
- docs/superpowers/plans/2026-06-07-wpf-minimal-mvvm-behaviors-governance-plan.md
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/build-and-test.log
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/drive-wpf-minimal-mvvm-behaviors.ps1
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/01-main-window-startup.png
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/02-mt-report-generator-behavior-page.png
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/03-export-command-empty-file-dialog.png
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/README.md

## Dependency Inventory Summary

- CommunityToolkit.Mvvm: already present and kept as the target MVVM stack.
- Microsoft.Xaml.Behaviors.Wpf: absent before PR-0, added in this spike.
- Wpf.Ui / WPF-UI / WPF-UI.Tray: still present by design; recorded for phased removal.
- Stylet.Start: still present by design; only MT Report Generator export action was migrated.
- Prism.Wpf: still present; recorded for early removal after dead-using cleanup.
- PropertyChanged.Fody: still present; recorded for view-model property migration.
- LiveChartsCore.* and SkiaSharp.*: still present; recorded for display replacement.
- Microsoft.Web.WebView2: still present; recorded for report preview replacement.
- HandyControl: no current use found.

## Build And Test Results

Full output is in build-and-test.log.

- Initial sandboxed `dotnet restore MetBench.sln`: failed with NU1301 because api.nuget.org socket access was blocked by sandbox permissions.
- Escalated `dotnet restore MetBench.sln`: exit 0.
- `dotnet build MetBench.sln --no-restore`: exit 0; 0 errors; existing warnings remain.
- `dotnet test MetBench_Client.Tests --no-build`: exit 0; 16 passed.
- `dotnet test MetBench_SystemMT.Tests --no-build --filter "ClientI18n|SystemMtExplanation|SystemMtPairQuality"`: exit 0; 18 passed.
- `git diff --check`: exit 0.

## Screenshots

- 01-main-window-startup.png: WPF main window launched from built output.
- 02-mt-report-generator-behavior-page.png: MT Report Generator page with the migrated Export button visible.
- 03-export-command-empty-file-dialog.png: result after clicking the migrated Export command, showing the existing empty-file validation dialog.

## Incomplete Items And Blockers

- PR-0 does not remove Wpf.Ui, Stylet, Prism, Fody, LiveCharts, SkiaSharp, or WebView2. They remain intentionally present for staged follow-up PRs.
- No blocker for the PR-0 design and minimal Behaviors spike.
- CLAUDE.md still advertises the older WPF stack. The design document records this as governance target overrides older convention; a later sync PR is required after phased evidence is collected.
