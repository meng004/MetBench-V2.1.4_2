# WPF Minimal MVVM Behaviors VM Evidence

Date: 2026-06-09
Windows classification: VM evidence collected

## Branch

- Branch: codex/wpf-pr2-native-shell
- Base commit: eb62fc0343a6b65d073f46295674c4460a090492
- Head commit at evidence collection: eb62fc0343a6b65d073f46295674c4460a090492
- Worktree state at evidence collection: dirty with uncommitted PR-2 WPF governance changes and unrelated local untracked files excluded from the PR scope.

## Modified File Groups

- MetBench_Client/MetBench_Client.csproj
- MetBench_Client/App.xaml
- MetBench_Client/App.xaml.cs
- MetBench_Client/Dictionary1.xaml
- MetBench_Client/GlobalUsings.cs
- MetBench_Client/Models/NavigationItem.cs
- MetBench_Client/Services/IClientNavigationWindow.cs
- MetBench_Client/Services/IClientWindow.cs
- MetBench_Client/Services/INavigationAware.cs
- MetBench_Client/Services/INavigationService.cs
- MetBench_Client/Services/IPageService.cs
- MetBench_Client/Services/NavigationService.cs
- MetBench_Client/Services/ApplicationHostService.cs
- MetBench_Client/Services/ClientThemeController.cs
- MetBench_Client/Services/PageService.cs
- MetBench_Client/ViewModels/*.cs WPF view-model binding cleanup
- MetBench_Client/Views/Pages/*.xaml and *.xaml.cs Wpf.Ui-free page conversion
- MetBench_Client/Views/Windows/MainWindow.xaml and MainWindow.xaml.cs
- MetBench_Client/Views/Windows/ApplicationProgramsWindow.xaml and ApplicationProgramsWindow.xaml.cs
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs
- MetBench_Client.Tests/ClientI18n/MainWindowLocalizationTests.cs
- MetBench_Client.Tests/ClientI18n/SettingsLanguageTests.cs
- docs/superpowers/plans/2026-06-07-wpf-minimal-mvvm-behaviors-governance-plan.md
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-governance-design.md
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/build-and-test.log
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/drive-wpf-pr2-native-shell.ps1
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/01-main-window-startup.png
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/02-mt-execution-native-page.png
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/03-system-mt-equation-catalog-native-page.png
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/04-settings-native-page.png
- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/README.md

Deleted Wpf.Ui helper files:

- MetBench_Client/Helpers/NameToPageTypeConverter.cs
- MetBench_Client/Helpers/PaneDisplayModeToIndexConverter.cs

## Dependency Inventory Summary

- CommunityToolkit.Mvvm: kept as the target MVVM stack.
- Microsoft.Xaml.Behaviors.Wpf: kept as the target XAML event-to-command stack.
- Wpf.Ui / WPF-UI: PR-2 removes the production package reference, XAML namespace, controls, navigation interfaces, shell usage, theme manager usage, and theme watcher usage from MetBench_Client.
- WPF-UI.Tray: removed from the client project and guarded against reintroduction.
- Stylet.Start: removed from the client project after page command binding migration.
- Prism.Wpf: removed from the client project after dead using cleanup.
- PropertyChanged.Fody: removed from the client project after explicit ObservableObject/RelayCommand migration.
- LiveChartsCore.* and SkiaSharp.*: still present; assigned to PR-3 display replacement.
- Microsoft.Web.WebView2: still present; assigned to PR-3 report preview replacement.
- HandyControl: no current use found.

## Build And Test Results

Full command output is tracked in build-and-test.log.

- `dotnet restore MetBench.sln -v:minimal`: exit 0; existing OpenTK/SkiaSharp compatibility warnings remain.
- `dotnet build MetBench.sln --no-restore -v:minimal`: exit 0; 0 errors, 6 warnings in the final minimal log.
- `dotnet test MetBench_Client.Tests\MetBench_Client.Tests.csproj --no-build -v:minimal`: exit 0; 36 passed, 0 skipped, 0 failed.
- `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-build --filter "ClientI18n|SystemMtExplanation|SystemMtPairQuality" -v:minimal`: exit 0; 18 passed, 0 skipped, 0 failed.
- `rg -n "Wpf\.Ui|WPF-UI|xmlns:ui|ui:|INavigableView|INavigationWindow|NavigationView|FluentWindow|SymbolIcon|ApplicationThemeManager|SystemThemeWatcher" MetBench_Client`: exit 1, no matches.
- `rg -n "�|锟|\?{3,}|瀵|鐨|鏈|鏁|铚|搴|椤|潰" MetBench_Client -g "*.cs" -g "*.xaml"`: exit 1, no matches.
- `git diff --check`: exit 0. Git reported CRLF normalization warnings only.
- `powershell -NoProfile -ExecutionPolicy Bypass -File docs\superpowers\specs\2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence\drive-wpf-pr2-native-shell.ps1`: exit 0.

## Screenshots

- 01-main-window-startup.png: WPF main window launched from the current PR-2 build output.
- 02-mt-execution-native-page.png: native shell navigation to MT Execution.
- 03-system-mt-equation-catalog-native-page.png: native shell navigation to System MT Equation Catalog.
- 04-settings-native-page.png: native shell navigation to Settings after Wpf.Ui-free settings migration.

## Incomplete Items And Blockers

- PR-2 has no remaining Wpf.Ui/WPF-UI production dependency blocker in MetBench_Client based on the current guard.
- LiveChartsCore.*, SkiaSharp.*, and Microsoft.Web.WebView2 remain intentionally present for PR-3 display and preview replacement.
- CLAUDE.md still advertises the older WPF stack. The design document records this as governance target overrides older convention; a later synchronization PR remains required after PR-3 evidence exists.
- Unrelated local untracked files are present in the worktree and must not be committed as part of this PR: `.claude/settings.local.json`, `_worktrees/`, and `tools/uia-verify-i18n.ps1`.
