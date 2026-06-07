# WPF Minimal MVVM Behaviors VM Evidence

Date: 2026-06-07
Windows classification: VM evidence collected

## Branch

- Branch: codex/wpf-minimal-mvvm-behaviors
- Base commit: c01de218404546d3379ebe03d8358374e768eabb
- Head commit at latest full-test evidence collection: a0461afa2680c2b9526cbf0662f1dddb34be7e2d
- Worktree state at latest full-test evidence collection: dirty only with refreshed evidence documentation/log before committing this evidence update.

## Modified Files

- MetBench_Client/MetBench_Client.csproj
- MetBench_Client/App.xaml.cs
- MetBench_Client/Services/EventAggregator.cs
- MetBench_Client/FodyWeavers.xml
- MetBench_Client/FodyWeavers.xsd
- MetBench_Client/ViewModels/ApplicationManagementViewModel.cs
- MetBench_Client/ViewModels/AutoDetectMRViewModel.cs
- MetBench_Client/ViewModels/DomainManagementViewModel.cs
- MetBench_Client/ViewModels/MainWindowViewModel.cs
- MetBench_Client/ViewModels/MRDisplayViewModel.cs
- MetBench_Client/ViewModels/MRManagementViewModel.cs
- MetBench_Client/ViewModels/MRRecommendationViewModel.cs
- MetBench_Client/ViewModels/MTExecutionViewModel.cs
- MetBench_Client/ViewModels/MTReportGeneratorViewModel.cs
- MetBench_Client/Views/Pages/ApplicationManagementPage.xaml
- MetBench_Client/Views/Pages/AutoDetectMRPage.xaml
- MetBench_Client/Views/Pages/DashboardPage.xaml
- MetBench_Client/Views/Pages/DomainManagementPage.xaml
- MetBench_Client/Views/Pages/MRDisplayPage.xaml
- MetBench_Client/Views/Pages/MRManagementPage.xaml
- MetBench_Client/Views/Pages/MRManagementPage.xaml.cs
- MetBench_Client/Views/Pages/MRRecommendationPage.xaml
- MetBench_Client/Views/Pages/MRRecommendationPage.xaml.cs
- MetBench_Client/Views/Pages/MTExecutionPage.xaml
- MetBench_Client/Views/Pages/MTReportGeneratorPage.xaml
- MetBench_Client/Views/Windows/ApplicationProgramsWindow.xaml
- MetBench_Client/Views/Windows/MainWindow.xaml
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs
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
- Microsoft.Xaml.Behaviors.Wpf: added and now used for migrated event-to-command bindings.
- Wpf.Ui / WPF-UI: still present by design; recorded for phased removal.
- WPF-UI.Tray: removed from the client project and guarded against reintroduction.
- Stylet.Start: removed from the client project after page command binding migration.
- Prism.Wpf: removed from the client project after dead-using cleanup.
- PropertyChanged.Fody: removed from the client project after explicit ObservableObject/RelayCommand migration.
- LiveChartsCore.* and SkiaSharp.*: still present; recorded for display replacement.
- Microsoft.Web.WebView2: still present; recorded for report preview replacement.
- HandyControl: no current use found.

## Build And Test Results

Full output is tracked in build-and-test.log.

- `dotnet restore MetBench.sln`: exit 0; existing OpenTK/SkiaSharp compatibility warnings remain.
- `dotnet build MetBench.sln --no-restore -v:minimal`: exit 0; 0 errors; existing warnings remain.
- `dotnet test MetBench_Client.Tests\MetBench_Client.Tests.csproj --no-build --logger "console;verbosity=minimal"`: exit 0; 22 passed.
- `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-build --filter FullyQualifiedName~ErrorMonotonicLauncherBranchingTests --logger "console;verbosity=minimal"`: exit 0; 4 passed.
- `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-build --filter FullyQualifiedName~MinimumMrSubsetBGroupExternalSourceSmokeTests --logger "console;verbosity=minimal"`: exit 0; 3 passed; 0 skipped. This run uses a temporary Python `sitecustomize.py` shim so external Minimum-MR-SubSet code that still calls removed NumPy `np.trapz` runs on NumPy 2.x by mapping it to `np.trapezoid`.
- `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-build --logger "console;verbosity=minimal"`: exit 0; 1819 passed, 12 skipped, 0 failed.
- The remaining 12 full-suite skips are existing OpenMOC/OpenMC environment-gated tests; the NumPy compatibility path no longer skips.
- `git diff --check`: exit 0.

## Screenshots

- 01-main-window-startup.png: WPF main window launched from built output.
- 02-mt-report-generator-behavior-page.png: MT Report Generator page with the migrated Export button visible.
- 03-export-command-empty-file-dialog.png: result after clicking the migrated Export command, showing the existing empty-file validation dialog.

## Incomplete Items And Blockers

- This branch does not remove Wpf.Ui, LiveCharts, SkiaSharp, or WebView2. They remain intentionally present for staged follow-up PRs.
- Stylet.Start, Prism.Wpf, PropertyChanged.Fody, and WPF-UI.Tray are removed and guarded in this branch.
- Full `MetBench_SystemMT.Tests` is green in this VM run with the repository's existing environment-gated OpenMOC/OpenMC skips. The external Minimum-MR-SubSet B-group tests now run and pass rather than skipping on NumPy 2.x.
- CLAUDE.md still advertises the older WPF stack. The design document records this as governance target overrides older convention; a later sync PR is required after phased evidence is collected.
