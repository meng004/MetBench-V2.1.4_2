# WPF Minimal MVVM and Behaviors Governance Plan

Date: 2026-06-07
Status: Active scoped plan; current branch includes PR-0, PR-1, PR-2a, PR-2b, PR-2c, and PR-2d evidence
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

## Task 3b: PR-1 / PR-2a Completed Slice

Files:

- MetBench_Client/MetBench_Client.csproj
- MetBench_Client/App.xaml.cs
- MetBench_Client/Services/EventAggregator.cs
- MetBench_Client/ViewModels/*.cs legacy WPF pages
- MetBench_Client/Views/Pages/*.xaml legacy WPF pages
- MetBench_Client/Views/Windows/*.xaml affected shell/window files
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs

Steps:

1. Remove Prism.Wpf, Stylet.Start, PropertyChanged.Fody, and WPF-UI.Tray package references.
2. Delete Fody weaver files.
3. Replace Stylet event/action bindings with RelayCommand and Microsoft.Xaml.Behaviors.Wpf where event binding is required.
4. Add architecture guards preventing Prism, Stylet, Fody, and WPF-UI.Tray reintroduction.
5. Keep Wpf.Ui, WebView2, LiveChartsCore.*, and SkiaSharp.* for later PR slices.

Expected output:

- Client WPF build has 0 errors.
- Client governance tests pass.
- `rg "s:Action|s:View.ActionTarget|Stylet|Prism|PropertyChanged.Fody|WPF-UI.Tray" MetBench_Client` has no production hits except documented plan/design text outside runtime sources.

## Task 3c: PR-2b Wpf.Ui Dialog Surface Slice

Files:

- MetBench_Client/Services/UiDialog.cs
- MetBench_Client/Helpers/FileCompressionAndStorageUtility.cs
- MetBench_Client/ViewModels/ApplicationManagementViewModel.cs
- MetBench_Client/ViewModels/AutoDetectMRViewModel.cs
- MetBench_Client/ViewModels/DomainManagementViewModel.cs
- MetBench_Client/ViewModels/MRManagementViewModel.cs
- MetBench_Client/ViewModels/MRRecommendationViewModel.cs
- MetBench_Client/ViewModels/MTExecutionViewModel.cs
- MetBench_Client/ViewModels/MTReportGeneratorViewModel.cs
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs

Steps:

1. Add a client-owned `UiDialog` helper backed by native WPF `MessageBox`.
2. Replace direct `Wpf.Ui.Controls.MessageBox` construction with `UiDialog.ShowMessageAsync` or `UiDialog.ConfirmAsync`.
3. Remove the remaining `.ShowDialogAsync()` Wpf.Ui dialog calls from client sources.
4. Add an architecture guard that prevents direct Wpf.Ui message-box usage from returning.
5. Remove and guard the remaining branch dependency on informational `showMessageAsync` return values.
6. Keep Wpf.Ui navigation, shell controls, themes, and icons in scope for later PR-2 slices.

Expected output:

- Client WPF build has 0 errors.
- Client governance tests pass and include the dialog guard.
- `rg "Wpf\\.Ui\\.Controls\\.MessageBox|ShowDialogAsync" MetBench_Client` has no matches.
- `rg "\\b(?:var|bool)\\s+\\w+\\s*=\\s*await\\s+showMessageAsync\\s*\\(" MetBench_Client` has no matches.
- No MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics change.

## Task 3d: PR-2c Wpf.Ui ThemeResource And ThemeService Surface Slice

Files:

- MetBench_Client/App.xaml.cs
- MetBench_Client/Views/Pages/SettingsPage.xaml
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs

Steps:

1. Remove the unused Wpf.Ui `IThemeService` / `ThemeService` registration from WPF app DI.
2. Replace `ui:ThemeResource` markup-extension usage with WPF-native `DynamicResource` on the Settings page.
3. Add architecture guards preventing `ui:ThemeResource` and Wpf.Ui theme service registration from returning.
4. Keep Wpf.Ui resource dictionaries, navigation shell, controls, icons, and `ApplicationThemeManager` for later PR-2 slices because they still have broad live use.

Expected output:

- Client WPF build has 0 errors.
- Client governance tests pass and include the theme-surface guards.
- `rg "ui:ThemeResource|IThemeService|ThemeService" MetBench_Client` has no matches.
- No MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics change.

## Task 3e: PR-2d Settings Theme API Isolation Slice

Files:

- MetBench_Client/App.xaml.cs
- MetBench_Client/Helpers/EnumToBooleanConverter.cs
- MetBench_Client/Helpers/ThemeToIndexConverter.cs
- MetBench_Client/Services/ClientThemeController.cs
- MetBench_Client/ViewModels/SettingsViewModel.cs
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs
- MetBench_Client.Tests/ClientI18n/SettingsLanguageTests.cs

Steps:

1. Add a client-owned `ClientTheme` enum and `IClientThemeController` abstraction.
2. Move direct Wpf.Ui `ApplicationThemeManager` usage behind a WPF-client service adapter.
3. Make `SettingsViewModel` depend on `IClientThemeController` and `ClientTheme` instead of Wpf.Ui appearance types.
4. Make `EnumToBooleanConverter` work with ordinary enum values rather than Wpf.Ui appearance types.
5. Delete the unused `ThemeToIndexConverter`.
6. Add architecture and unit-test coverage proving ViewModels/Helpers no longer depend on `Wpf.Ui.Appearance` and Settings theme commands still call the theme controller.

Expected output:

- Client WPF build has 0 errors.
- Client governance and i18n tests pass.
- `rg "Wpf.Ui.Appearance|using Wpf.Ui.Appearance" MetBench_Client\ViewModels MetBench_Client\Helpers` has no matches.
- `rg "ThemeToIndexConverter|IThemeService|ThemeService|ui:ThemeResource" MetBench_Client` has no matches.
- Wpf.Ui `ApplicationThemeManager` remains isolated in `MetBench_Client/Services/ClientThemeController.cs` for a later final theme-removal slice.
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

- Only WPF dependency-governance design, migration, guard, and VM evidence files are modified.
- Existing unrelated untracked local files are not committed.
- PR body states that Wpf.Ui, LiveCharts, SkiaSharp, and WebView2 remain present by design for later slices.
- PR body states that Stylet.Start, Prism.Wpf, PropertyChanged.Fody, and WPF-UI.Tray were removed in the current branch.
