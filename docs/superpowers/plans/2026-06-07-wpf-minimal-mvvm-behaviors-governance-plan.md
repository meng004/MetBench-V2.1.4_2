# WPF Minimal MVVM and Behaviors Governance Plan

Date: 2026-06-07
Status: Active scoped plan; current branch includes PR-0, PR-1, and PR-2 evidence through final Wpf.Ui/WPF-UI production dependency removal. PR-3 remains active for LiveChartsCore.*, SkiaSharp.*, and Microsoft.Web.WebView2 display replacement.
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
5. At this slice boundary, keep Wpf.Ui, WebView2, LiveChartsCore.*, and SkiaSharp.* for later slices; Wpf.Ui is subsequently removed by Task 3l.

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
6. At this slice boundary, keep Wpf.Ui navigation, shell controls, themes, and icons in scope for later PR-2 slices; those surfaces are subsequently removed by Task 3l.

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
4. At this slice boundary, keep Wpf.Ui resource dictionaries, navigation shell, controls, icons, and `ApplicationThemeManager` for later PR-2 slices because they still have broad live use; those surfaces are subsequently removed by Task 3l.

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
- At this slice boundary, Wpf.Ui `ApplicationThemeManager` remains isolated in `MetBench_Client/Services/ClientThemeController.cs`; it is subsequently removed by Task 3l.
- No MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics change.

## Task 3f: PR-2e Settings Page Wpf.Ui XAML Removal Slice

Files:

- MetBench_Client/Views/Pages/SettingsPage.xaml
- MetBench_Client/Views/Pages/SettingsPage.xaml.cs
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs

Steps:

1. Remove the Wpf.Ui XAML namespace and `ui:Design.*` design-time attributes from SettingsPage.
2. Replace `ui:TextBlock`, `ui:Anchor`, and `ui:SymbolIcon` usage with WPF-native `TextBlock` / `Hyperlink`.
3. Remove stale commented Wpf.Ui Settings card markup.
4. Remove the `INavigableView<T>` interface from SettingsPage code-behind while preserving `DataContext` assignment.
5. Add a SettingsPage-specific architecture guard preventing Wpf.Ui namespace or `ui:` markup from returning.

Expected output:

- Client WPF build has 0 errors.
- Client governance and i18n tests pass.
- `rg "http://schemas.lepo.co/wpfui/2022/xaml|Wpf\\.Ui|ui:" MetBench_Client\Views\Pages\SettingsPage.xaml MetBench_Client\Views\Pages\SettingsPage.xaml.cs` has no matches.
- The source-code link still opens through WPF `Hyperlink.RequestNavigate`.
- No MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics change.

## Task 3g: PR-2f Progress Window Wpf.Ui Control Removal Slice

Files:

- MetBench_Client/Views/Windows/ProgressWindow.xaml
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs

Steps:

1. Replace the Wpf.Ui `ProgressRing` with a WPF-native indeterminate `ProgressBar`.
2. Remove the Wpf.Ui XAML namespace from ProgressWindow.
3. Add a ProgressWindow-specific architecture guard preventing Wpf.Ui namespace or `ui:` markup from returning.

Expected output:

- Client WPF build has 0 errors.
- Client governance tests pass.
- `rg "http://schemas.lepo.co/wpfui/2022/xaml|Wpf\\.Ui|ui:" MetBench_Client\Views\Windows\ProgressWindow.xaml` has no matches.
- No MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics change.

## Task 3h: PR-2g Paging Controls Wpf.Ui Control Removal Slice

Files:

- MetBench_Client/Controls/SimplePagination.xaml
- MetBench_Client/Views/Controls/PagingBar.xaml
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs

Steps:

1. Replace Wpf.Ui paging buttons with WPF-native `Button`.
2. Replace Wpf.Ui `SymbolIcon` markup with stable text button content for the small paging toolbar.
3. Remove Wpf.Ui XAML namespaces from both paging controls.
4. Add a paging-controls architecture guard preventing Wpf.Ui namespace or `ui:` markup from returning.

Expected output:

- Client WPF build has 0 errors.
- Client governance tests pass.
- `rg "http://schemas.lepo.co/wpfui/2022/xaml|Wpf\\.Ui|ui:" MetBench_Client\Controls\SimplePagination.xaml MetBench_Client\Views\Controls\PagingBar.xaml` has no matches.
- No MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics change.

## Task 3i: PR-2h Settings Page Lifecycle Repair

Files:

- MetBench_Client/Views/Pages/SettingsPage.xaml.cs
- MetBench_Client.Tests/ClientI18n/SettingsLanguageTests.cs

Steps:

1. Keep SettingsPage free of Wpf.Ui `INavigableView<T>`.
2. Restore explicit `DataContext = this` after the PR-2e code-behind cleanup.
3. Forward WPF-native `Loaded` / `Unloaded` events to `SettingsViewModel.OnNavigatedTo` / `OnNavigatedFrom`.
4. Add a WPF test proving SettingsPage `Loaded` initializes cultures, theme, and app version without Wpf.Ui navigation lifecycle.

Expected output:

- SettingsPage bindings keep their page-based `ViewModel.*` source.
- SettingsViewModel initialization runs when the page is loaded.
- Client tests pass with the lifecycle regression covered.
- No Wpf.Ui API is reintroduced into SettingsPage.

## Task 3j: PR-2i Application Programs Window Wpf.Ui Removal Slice

Files:

- MetBench_Client/Services/IClientWindow.cs
- MetBench_Client/ViewModels/ApplicationManagementViewModel.cs
- MetBench_Client/Views/Windows/ApplicationProgramsWindow.xaml
- MetBench_Client/Views/Windows/ApplicationProgramsWindow.xaml.cs
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs

Steps:

1. Add a client-owned `IClientWindow` abstraction for simple show/hide parameter windows.
2. Replace `ApplicationProgramsWindow` `ui:FluentWindow`, `ui:TitleBar`, `ui:DataGrid`, and `ui:Button` usage with WPF `Window`, `Border`, `TextBlock`, `DataGrid`, and `Button`.
3. Remove Wpf.Ui `INavigationWindow` / `INavigableView<T>` from the parameter window code-behind.
4. Make `ApplicationManagementViewModel` use `IClientWindow` for this parameter popup instead of casting it to Wpf.Ui `INavigationWindow`.
5. Add an architecture guard preventing Wpf.Ui namespace, `ui:` markup, `INavigationWindow`, and `INavigableView` from returning to this window.

Expected output:

- Client WPF build has 0 errors.
- Client governance tests pass and include the ApplicationProgramsWindow guard.
- `rg "http://schemas.lepo.co/wpfui/2022/xaml|Wpf\\.Ui|ui:|INavigationWindow|INavigableView" MetBench_Client\Views\Windows\ApplicationProgramsWindow.xaml MetBench_Client\Views\Windows\ApplicationProgramsWindow.xaml.cs` has no matches.
- No MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics change.

## Task 3k: PR-2j Dead Wpf.Ui Helper Removal Slice

Files:

- MetBench_Client/Helpers/NameToPageTypeConverter.cs
- MetBench_Client/Helpers/PaneDisplayModeToIndexConverter.cs
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs

Steps:

1. Confirm both helpers have no production call sites beyond their own definitions.
2. Delete the unused Wpf.Ui Gallery name-to-page converter.
3. Delete the unused Wpf.Ui `NavigationViewPaneDisplayMode` converter.
4. Add an architecture guard preventing Wpf.Ui controls/gallery helper dependencies from returning.

Expected output:

- Client WPF build has 0 errors.
- Client governance tests pass and include the helper guard.
- `rg "PaneDisplayModeToIndexConverter|NameToPageTypeConverter" MetBench_Client` has no production matches.
- No MetBench_BLL.Core, MetBench_DAL, or MetBench_BLL runtime semantics change.

## Task 3l: PR-2 Final Native Shell And Wpf.Ui Package Removal

Files:

- MetBench_Client/MetBench_Client.csproj
- MetBench_Client/App.xaml
- MetBench_Client/App.xaml.cs
- MetBench_Client/Dictionary1.xaml
- MetBench_Client/GlobalUsings.cs
- MetBench_Client/Models/NavigationItem.cs
- MetBench_Client/Services/IClientNavigationWindow.cs
- MetBench_Client/Services/INavigationAware.cs
- MetBench_Client/Services/INavigationService.cs
- MetBench_Client/Services/IPageService.cs
- MetBench_Client/Services/NavigationService.cs
- MetBench_Client/Services/ApplicationHostService.cs
- MetBench_Client/Services/ClientThemeController.cs
- MetBench_Client/Services/PageService.cs
- MetBench_Client/ViewModels/MainWindowViewModel.cs
- MetBench_Client/Views/Windows/MainWindow.xaml
- MetBench_Client/Views/Windows/MainWindow.xaml.cs
- MetBench_Client/Views/Pages/*.xaml
- MetBench_Client/Views/Pages/*.xaml.cs
- MetBench_Client.Tests/Architecture/WpfDependencyGovernanceTests.cs
- MetBench_Client.Tests/ClientI18n/MainWindowLocalizationTests.cs
- MetBench_Client.Tests/ClientI18n/SettingsLanguageTests.cs

Steps:

1. Remove the `WPF-UI` package reference from the client project.
2. Replace the Wpf.Ui shell with WPF `Window`, `ListBox`, and `Frame`.
3. Add a project-owned navigation service for WPF `Page` activation and `INavigationAware` lifecycle forwarding.
4. Replace Wpf.Ui theme manager usage with a WPF resource-based `NativeClientThemeController`.
5. Convert remaining Wpf.Ui page/control XAML to WPF-native `Page`, `Button`, `TextBox`, `DataGrid`, and `ItemsControl`.
6. Remove Wpf.Ui page/window interfaces from code-behind.
7. Add a production residual guard blocking Wpf.Ui, WPF-UI, Wpf.Ui XAML namespaces, Wpf.Ui navigation interfaces, shell controls, icons, and theme watcher APIs from returning.

Expected output:

- Client WPF build has 0 errors.
- Client governance and i18n tests pass.
- `rg "Wpf\\.Ui|WPF-UI|xmlns:ui|ui:|INavigableView|INavigationWindow|NavigationView|FluentWindow|SymbolIcon|ApplicationThemeManager|SystemThemeWatcher" MetBench_Client` has no matches.
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
rg -n "Wpf\.Ui|WPF-UI|xmlns:ui|ui:|INavigableView|INavigationWindow|NavigationView|FluentWindow|SymbolIcon|ApplicationThemeManager|SystemThemeWatcher" MetBench_Client
rg -n "�|锟|\?{3,}|瀵|鐨|鏈|鏁|铚|搴|椤|潰" MetBench_Client -g "*.cs" -g "*.xaml"
git diff --check
```

Expected output:

- Restore succeeds.
- Solution build has 0 errors.
- Focused client and System MT tests pass.
- Wpf.Ui production residual guard has no matches.
- Mojibake guard has no matches after the large XAML/code-behind conversion.
- Diff whitespace check exits 0.

Failure action:

- If restore cannot reach NuGet for the new package, record the exact network failure and do not claim VM evidence.
- If WPF build fails because of the spike, revert only the three spike files and keep docs as PR-0 design evidence.

## Task 5: VM Screenshot Evidence

Evidence directory:

- docs/superpowers/specs/2026-06-07-wpf-minimal-mvvm-behaviors-vm-evidence/

Screenshot files:

- 01-main-window-startup.png
- 02-mt-execution-native-page.png
- 03-system-mt-equation-catalog-native-page.png
- 04-settings-native-page.png

Steps:

1. Launch MetBench_Client from the built output.
2. Capture the native main window after startup.
3. Navigate to MT Execution through the WPF-native navigation list and capture the page.
4. Navigate to System MT Equation Catalog through the WPF-native navigation list and capture the page.
5. Navigate to Settings through the WPF-native footer navigation list and capture the page.

Expected output:

- Screenshots exist and show real WPF UI, not mocked content.
- Screenshots show the native shell and real page activation after Wpf.Ui removal.

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
- PR body states that Wpf.Ui/WPF-UI production package and runtime surfaces were removed in PR-2 and guarded against reintroduction.
- PR body states that Stylet.Start, Prism.Wpf, PropertyChanged.Fody, and WPF-UI.Tray were removed in PR-1/PR-2 convergence.
- PR body states that LiveChartsCore.*, SkiaSharp.*, and WebView2 remain for PR-3 display replacement.
