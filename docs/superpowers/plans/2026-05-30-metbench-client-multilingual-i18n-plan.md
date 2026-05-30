# MetBench Client Multilingual I18n Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a mature, test-driven multilingual foundation to `MetBench_Client`, with Chinese and English support first, then migrate user-facing pages incrementally.

**Architecture:** Add a new UI-neutral `MetBench_UI.Localization` class library that targets plain `net8.0` and uses the mature .NET `.resx` / `System.Resources.ResourceManager` localization pipeline behind `IAppLocalizationService`. Keep localization state, fallback rules, and runtime culture change notification outside `MetBench_Client`; the current WPF client references the library through view-model bindings, and a future Avalonia client can reference the same library without taking a Windows/WPF dependency. Execute the implementation inside the Windows VM, with the VM Claude Code worker reading this plan file and the coordinator validating commits, tests, UIA screenshots, and evidence.

**Tech Stack:** .NET 8 class library localization core, WPF/WPF-UI current client adapter, Avalonia-ready resource/service API, `.resx` resources, `System.Resources.ResourceManager`, xUnit, CommunityToolkit.Mvvm, UIA screenshot smoke tests, Claude Code subagents using `gpt5.4`.

---

## Execution Model

The implementation is not executed directly from the macOS coordinator. The VM is the execution environment.

VM repository path:

```powershell
C:\MetBench-V2.1.4_2
```

VM Claude Code startup command:

```powershell
cd C:\MetBench-V2.1.4_2
git fetch origin
git checkout -B codex/client-i18n origin/main
cmd /c claude
```

Prompt to paste into VM Claude:

```text
Read and execute docs\superpowers\plans\2026-05-30-metbench-client-multilingual-i18n-plan.md exactly.

Use superpowers:subagent-driven-development. Dispatch a fresh subagent per task, and use model gpt5.4 for every implementer, spec-reviewer, code-quality-reviewer, and final reviewer subagent.

Use superpowers:test-driven-development for every production-code change. For each task: write failing tests first, run and capture the RED failure, implement the smallest code, run GREEN verification, then do spec compliance review and code quality review before moving to the next task.

Use the Windows VM for build, tests, and UIA screenshots. Do not modify production code outside the task scope. Commit after each task with a focused message. Push branch codex/client-i18n when complete.
```

Coordinator validation after VM pushes:

```bash
rtk git fetch origin codex/client-i18n
rtk git diff --check origin/main..origin/codex/client-i18n
rtk git diff --name-only origin/main..origin/codex/client-i18n
```

Coordinator must inspect the VM evidence directory before accepting:

```text
docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/
```

Required VM evidence files:

```text
vm-status.jsonl
vm-summary.md
01-red-green-infra-tests.png
02-settings-language-switch-zh.png
03-settings-language-switch-en.png
04-navigation-zh.png
05-navigation-en.png
06-system-mt-page-zh.png
07-system-mt-page-en.png
08-invalid-culture-fallback.png
09-missing-key-fallback.png
```

## Library Choice

Use the official .NET resource localization stack rather than a WPF-only localization extension.

Reasons:

- `.resx` + `ResourceManager` is mature, stable, and native to .NET desktop applications.
- The localization core lives in `MetBench_UI.Localization` and does not reference WPF, WPF-UI, XAML markup extensions, Avalonia, or Windows-only APIs.
- Avalonia's documented ResX approach also relies on resource culture changes; runtime switching requires either view reload or `INotifyPropertyChanged`. This plan implements the notification path once in app-owned code.
- WPF-specific work is limited to bindings in the current client. A future Avalonia UI can bind to the same `IAppLocalizationService` or the same indexer provider.

Package policy:

```xml
<!-- Do not add WPFLocalizationExtension or any other WPF-only localization package. -->
```

If the VM implementer needs DI-facing abstractions, add only Microsoft-owned, UI-neutral localization packages such as `Microsoft.Extensions.Localization.Abstractions`. Do not introduce a UI-framework-specific localization library.

## Scope

Languages for this plan:

| Culture | Language | Role |
|---|---|---|
| `en-US` | English | Default fallback and existing UI source language |
| `zh-CN` | Simplified Chinese | Required second language |

Initial page migration order:

1. Infrastructure and resource tests.
2. Shell/navigation and title bar.
3. Settings page with language switcher.
4. System-MT pages already used in release readiness evidence.
5. Legacy Method-MT pages.
6. Error and abnormal-state strings.

Out of scope for this plan:

- Translating scientific catalog data from `SUT/**/catalog.json`.
- Translating generated reports beyond UI labels unless already exposed in WPF pages.
- Changing persistence schemas.
- Changing non-UI numeric/date formatting in System-MT evidence and scientific calculations, which must continue using `InvariantCulture` where already required.

## File Structure

Create:

- `MetBench_UI.Localization/MetBench_UI.Localization.csproj`
  - Plain `net8.0` class library with no `UseWPF`, no `net8.0-windows`, and no UI framework package reference.
- `MetBench_UI.Localization/IAppLocalizationService.cs`
  - UI-neutral interface for current culture, available cultures, `SetCulture`, `GetString`, and `CultureChanged`.
- `MetBench_UI.Localization/AppLocalizationService.cs`
  - ResourceManager-backed implementation with fallback and no WPF/Avalonia references.
- `MetBench_UI.Localization/AppCultureOption.cs`
  - Small immutable option model for Settings language selector.
- `MetBench_UI.Localization/LocalizedTextProvider.cs`
  - `INotifyPropertyChanged` indexer provider for XAML bindings in WPF now and Avalonia later.
- `MetBench_UI.Localization/Resources/Strings.resx`
  - English default resources.
- `MetBench_UI.Localization/Resources/Strings.zh-CN.resx`
  - Simplified Chinese resources.
- `MetBench_SystemMT.Tests/ClientI18n/LocalizationResourceTests.cs`
  - Resource parity and lookup tests.
- `MetBench_SystemMT.Tests/ClientI18n/AppLocalizationServiceTests.cs`
  - Service behavior, culture switching, fallback tests.
- `docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/README.md`
  - VM evidence manifest.

Modify:

- `MetBench_Client/MetBench_Client.csproj`
  - Reference `MetBench_UI.Localization`; do not add WPF-only localization packages.
- `MetBench_Client/App.xaml`
  - Keep existing WPF resources; do not configure localization through WPF-only markup extensions.
- `MetBench_Client/App.xaml.cs`
  - Register `IAppLocalizationService`.
- `MetBench_Client/ViewModels/MainWindowViewModel.cs`
  - Generate navigation labels from localization keys and refresh on culture change.
- `MetBench_Client/Views/Windows/MainWindow.xaml`
  - Localize search placeholder and any shell text.
- `MetBench_Client/ViewModels/SettingsViewModel.cs`
  - Add culture options, selected culture, and change command.
- `MetBench_Client/Views/Pages/SettingsPage.xaml`
  - Add language selector and convert static labels to bindings backed by `LocalizedTextProvider`.
- `MetBench_Client/Views/Pages/SystemMtExecutionPage.xaml`
- `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml`
- `MetBench_Client/Views/Pages/SystemMtSutCatalogPage.xaml`
- `MetBench_Client/Views/Pages/SystemMtEquationCatalogPage.xaml`
- `MetBench_Client/Views/Pages/SystemMtSampleCaseCatalogPage.xaml`
- `MetBench_Client/Views/Pages/SystemMtExecutionHistoryPage.xaml`
- `MetBench_Client/Views/Pages/AnomalyListPage.xaml`
- `MetBench_Client/Views/Pages/ReplayResultPage.xaml`
- `MetBench_Client/Views/Pages/MTReportGeneratorPage.xaml`
  - Convert user-facing static text to localized bindings in later page tasks.
- `tools/smokeshot/`
  - Add or extend UIA smoke flow for culture switching and bilingual screenshots.

Do not modify:

- `SUT/**`
- System-MT runtime semantics.
- Typed catalog predicates/kernels.
- Report renderer numeric/date invariant formatting.

## Subagent Dispatch Rules

For every task below, dispatch subagents in this order:

1. Implementer subagent: model `gpt5.4`.
2. Spec compliance reviewer subagent: model `gpt5.4`.
3. Code quality reviewer subagent: model `gpt5.4`.

Every subagent prompt must include:

```text
You are running inside Windows VM path C:\MetBench-V2.1.4_2.
Use all shell commands in PowerShell, not macOS shell.
Use TDD: write the failing test first, run it and capture the expected failure, then implement.
Do not edit production code before RED is captured.
Commit only your task's files after GREEN verification and self-review.
```

Spec reviewer acceptance:

```text
Approve only if every requirement in the task is implemented, no task scope was skipped, and no unrelated production behavior was changed.
```

Code quality reviewer acceptance:

```text
Approve only if the implementation follows existing WPF/MVVM patterns, keeps the localization core UI-neutral and Avalonia-ready, has no duplicate localization lookup logic, keeps resource keys stable, and does not introduce ad hoc string parsing for localization.
```

## Task 1: Localization Infrastructure

**Files:**
- Create: `MetBench_UI.Localization/MetBench_UI.Localization.csproj`
- Modify: `MetBench_Client/MetBench_Client.csproj`
- Modify: `MetBench_Client/App.xaml`
- Modify: `MetBench_Client/App.xaml.cs`
- Create: `MetBench_UI.Localization/IAppLocalizationService.cs`
- Create: `MetBench_UI.Localization/AppLocalizationService.cs`
- Create: `MetBench_UI.Localization/AppCultureOption.cs`
- Create: `MetBench_UI.Localization/LocalizedTextProvider.cs`
- Create: `MetBench_UI.Localization/Resources/Strings.resx`
- Create: `MetBench_UI.Localization/Resources/Strings.zh-CN.resx`
- Create: `MetBench_SystemMT.Tests/ClientI18n/LocalizationResourceTests.cs`
- Create: `MetBench_SystemMT.Tests/ClientI18n/AppLocalizationServiceTests.cs`

**Preconditions:**

- VM branch is `codex/client-i18n` from `origin/main`.
- `dotnet restore MetBench.sln` succeeds on Windows.
- No uncommitted changes except this task.

**Core steps:**

- [ ] **Step 1: Write failing resource parity tests**

Create `MetBench_SystemMT.Tests/ClientI18n/LocalizationResourceTests.cs`:

```csharp
using System.Globalization;
using System.Linq;
using System.Resources;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class LocalizationResourceTests
{
    [Fact]
    public void English_and_chinese_resources_contain_required_shell_keys()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(AppLocalizationService).Assembly);

        var keys = new[]
        {
            "App_Title",
            "Nav_SystemMtExecution",
            "Nav_Settings",
            "Settings_Personalization",
            "Settings_Language",
            "Settings_Language_English",
            "Settings_Language_Chinese",
            "Common_Search",
            "Common_NotAvailable"
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))));
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))));
        }
    }

    [Fact]
    public void Localization_core_has_no_ui_framework_references()
    {
        var referenced = typeof(AppLocalizationService).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        Assert.DoesNotContain("PresentationFramework", referenced);
        Assert.DoesNotContain("WindowsBase", referenced);
        Assert.DoesNotContain("Wpf.Ui", referenced);
        Assert.DoesNotContain("Avalonia", referenced);
    }
}
```

- [ ] **Step 2: Run RED**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.LocalizationResourceTests" --logger "console;verbosity=minimal"
```

Expected RED:

```text
FAIL: type or namespace name 'MetBench_UI' could not be found
```

Add a test project reference before implementing production code:

```xml
<ProjectReference Include="..\MetBench_UI.Localization\MetBench_UI.Localization.csproj" />
```

Then rerun and expect failure because resources or keys do not exist.

- [ ] **Step 3: Add resource skeleton**

Create `MetBench_UI.Localization/MetBench_UI.Localization.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

Modify `MetBench_Client/MetBench_Client.csproj`:

```xml
<ProjectReference Include="..\MetBench_UI.Localization\MetBench_UI.Localization.csproj" />
```

Add the project to `MetBench.sln`:

```powershell
dotnet sln MetBench.sln add MetBench_UI.Localization\MetBench_UI.Localization.csproj
```

Do not add `WPFLocalizationExtension` or any WPF-only localization package.

Create `MetBench_UI.Localization/Resources/Strings.resx` with these key/value pairs:

| Key | Value |
|---|---|
| `App_Title` | `MetBench` |
| `Nav_SystemMtExecution` | `System MT` |
| `Nav_Settings` | `Settings` |
| `Settings_Personalization` | `Personalization` |
| `Settings_Language` | `Language` |
| `Settings_Language_English` | `English` |
| `Settings_Language_Chinese` | `中文` |
| `Common_Search` | `Search` |
| `Common_NotAvailable` | `N/A` |

Create `MetBench_UI.Localization/Resources/Strings.zh-CN.resx` with:

| Key | Value |
|---|---|
| `App_Title` | `MetBench` |
| `Nav_SystemMtExecution` | `系统级蜕变测试` |
| `Nav_Settings` | `设置` |
| `Settings_Personalization` | `个性化` |
| `Settings_Language` | `语言` |
| `Settings_Language_English` | `English` |
| `Settings_Language_Chinese` | `中文` |
| `Common_Search` | `搜索` |
| `Common_NotAvailable` | `不可用` |

- [ ] **Step 4: Write failing localization service tests**

Create `MetBench_SystemMT.Tests/ClientI18n/AppLocalizationServiceTests.cs`:

```csharp
using System.Globalization;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class AppLocalizationServiceTests
{
    [Fact]
    public void SetCulture_changes_lookup_language()
    {
        var service = new AppLocalizationService();

        service.SetCulture(new CultureInfo("zh-CN"));
        Assert.Equal("系统级蜕变测试", service.GetString("Nav_SystemMtExecution"));

        service.SetCulture(new CultureInfo("en-US"));
        Assert.Equal("System MT", service.GetString("Nav_SystemMtExecution"));
    }

    [Fact]
    public void Unsupported_culture_falls_back_to_english()
    {
        var service = new AppLocalizationService();

        service.SetCulture(new CultureInfo("fr-FR"));

        Assert.Equal("System MT", service.GetString("Nav_SystemMtExecution"));
        Assert.Equal("en-US", service.CurrentCulture.Name);
    }

    [Fact]
    public void Missing_key_returns_visible_fallback()
    {
        var service = new AppLocalizationService();

        Assert.Equal("??Missing_Key??", service.GetString("Missing_Key"));
    }
}
```

- [ ] **Step 5: Run RED**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.AppLocalizationServiceTests" --logger "console;verbosity=minimal"
```

Expected RED:

```text
FAIL: type or namespace name 'MetBench_UI.Localization' could not be found
```

- [ ] **Step 6: Implement minimal service**

Create `MetBench_UI.Localization/AppCultureOption.cs`:

```csharp
using System.Globalization;

namespace MetBench_UI.Localization;

public sealed record AppCultureOption(string DisplayName, CultureInfo Culture);
```

Create `MetBench_UI.Localization/IAppLocalizationService.cs`:

```csharp
using System.Collections.ObjectModel;
using System.Globalization;

namespace MetBench_UI.Localization;

public interface IAppLocalizationService
{
    event EventHandler? CultureChanged;

    CultureInfo CurrentCulture { get; }

    ReadOnlyCollection<AppCultureOption> AvailableCultures { get; }

    void SetCulture(CultureInfo culture);

    string GetString(string key);
}
```

Create `MetBench_UI.Localization/AppLocalizationService.cs`:

```csharp
using System.Collections.ObjectModel;
using System.Globalization;
using MetBench_UI.Localization.Resources;

namespace MetBench_UI.Localization;

public sealed class AppLocalizationService : IAppLocalizationService
{
    private static readonly CultureInfo English = new("en-US");
    private static readonly CultureInfo Chinese = new("zh-CN");

    private static readonly ReadOnlyCollection<AppCultureOption> Cultures = new(
        new[]
        {
            new AppCultureOption("English", English),
            new AppCultureOption("中文", Chinese),
        });

    public AppLocalizationService()
    {
        CultureInfo.CurrentUICulture = English;
    }

    public CultureInfo CurrentCulture { get; private set; } = English;

    public event EventHandler? CultureChanged;

    public ReadOnlyCollection<AppCultureOption> AvailableCultures => Cultures;

    public void SetCulture(CultureInfo culture)
    {
        var selected = ResolveCulture(culture);
        if (selected.Name == CurrentCulture.Name)
        {
            return;
        }

        CurrentCulture = selected;
        CultureInfo.CurrentUICulture = selected;
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key)
    {
        var value = Strings.ResourceManager.GetString(key, CurrentCulture);
        return string.IsNullOrWhiteSpace(value) ? $"??{key}??" : value;
    }

    private static CultureInfo ResolveCulture(CultureInfo culture)
    {
        var exact = Cultures.FirstOrDefault(c => c.Culture.Name == culture.Name)?.Culture;
        if (exact is not null)
        {
            return exact;
        }

        var neutral = Cultures.FirstOrDefault(c => c.Culture.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName)?.Culture;
        return neutral ?? English;
    }
}
```

Create `MetBench_UI.Localization/LocalizedTextProvider.cs`:

```csharp
using System.ComponentModel;

namespace MetBench_UI.Localization;

public sealed class LocalizedTextProvider : INotifyPropertyChanged
{
    private readonly IAppLocalizationService _localization;

    public LocalizedTextProvider(IAppLocalizationService localization)
    {
        _localization = localization;
        _localization.CultureChanged += (_, _) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => _localization.GetString(key);
}
```

Register `IAppLocalizationService` in `MetBench_Client/App.xaml.cs` using the repository's existing host/service registration pattern:

```csharp
services.AddSingleton<IAppLocalizationService, AppLocalizationService>();
services.AddSingleton<LocalizedTextProvider>();
```

- [ ] **Step 7: Run GREEN**

Run:

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n" --logger "console;verbosity=minimal"
```

Expected GREEN:

```text
Passed: 5, Failed: 0
```

- [ ] **Step 8: Commit**

```powershell
git add MetBench_UI.Localization MetBench_Client MetBench_SystemMT.Tests
git commit -m "feat(client): add i18n localization infrastructure"
```

**Acceptance criteria:**

- Chinese and English resource files exist and have parity for infrastructure keys.
- Localization core targets plain `net8.0` and has no WPF, WPF-UI, Avalonia, or Windows framework references.
- `AppLocalizationService` switches `en-US` and `zh-CN`.
- Unsupported culture falls back to English.
- Missing key fallback is visible.
- Tests follow RED then GREEN and evidence is recorded in `vm-status.jsonl`.

## Task 2: Shell Navigation And Runtime Culture Refresh

**Files:**
- Modify: `MetBench_Client/ViewModels/MainWindowViewModel.cs`
- Modify: `MetBench_Client/Views/Windows/MainWindow.xaml`
- Modify: `MetBench_UI.Localization/Resources/Strings.resx`
- Modify: `MetBench_UI.Localization/Resources/Strings.zh-CN.resx`
- Create: `MetBench_SystemMT.Tests/ClientI18n/MainWindowLocalizationTests.cs`

**Preconditions:**

- Task 1 commit exists.
- `ClientI18n` tests are green.
- VM branch has no uncommitted changes.

**Core steps:**

- [ ] **Step 1: Add failing navigation tests**

Create `MetBench_SystemMT.Tests/ClientI18n/MainWindowLocalizationTests.cs`:

```csharp
using System.Globalization;
using MetBench_UI.Localization;
using MetBench_Client.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class MainWindowLocalizationTests
{
    [WpfFact]
    public void Navigation_labels_refresh_when_culture_changes()
    {
        var localization = new AppLocalizationService();
        var vm = new MainWindowViewModel(new NavigationService(), localization, new LocalizedTextProvider(localization));

        localization.SetCulture(new CultureInfo("zh-CN"));
        vm.RefreshLocalizedText();

        var systemMt = vm.NavigationItems.OfType<NavigationViewItem>()
            .Single(item => item.TargetPageType == typeof(MetBench_Client.Views.Pages.SystemMtExecutionPage));
        var settings = vm.NavigationFooter.OfType<NavigationViewItem>().Single();

        Assert.Equal("系统级蜕变测试", systemMt.Content);
        Assert.Equal("设置", settings.Content);

        localization.SetCulture(new CultureInfo("en-US"));
        vm.RefreshLocalizedText();

        Assert.Equal("System MT", systemMt.Content);
        Assert.Equal("Settings", settings.Content);
    }
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.MainWindowLocalizationTests" --logger "console;verbosity=minimal"
```

Expected RED:

```text
FAIL: constructor MainWindowViewModel(INavigationService, IAppLocalizationService, LocalizedTextProvider) does not exist
```

- [ ] **Step 3: Add navigation resource keys**

Add these keys to both resource files:

| Key | en-US | zh-CN |
|---|---|---|
| `Nav_MrDisplay` | `MR Display` | `MR 展示` |
| `Nav_MrManagement` | `MR Management` | `MR 管理` |
| `Nav_ApplicationManagement` | `Application Management` | `应用管理` |
| `Nav_DomainManagement` | `Domain Management` | `领域管理` |
| `Nav_MtExecution` | `MT Execution` | `方法级蜕变测试执行` |
| `Nav_SystemMtExecution` | `System MT` | `系统级蜕变测试` |
| `Nav_SystemMtMrCatalog` | `System MT MR Catalog` | `系统级 MR 目录` |
| `Nav_SystemMtSutCatalog` | `System MT SUT Catalog` | `系统级 SUT 目录` |
| `Nav_SystemMtEquationCatalog` | `System MT Equation Catalog` | `系统级方程目录` |
| `Nav_SystemMtSampleCaseCatalog` | `System MT Sample Case Catalog` | `系统级样例目录` |
| `Nav_SystemMtExecutionHistory` | `System MT Execution History` | `系统级执行历史` |
| `Nav_Anomalies` | `Anomalies` | `异常` |
| `Nav_Discovery` | `Discovery` | `发现` |
| `Nav_CandidateReview` | `Candidate Review` | `候选评审` |
| `Nav_Mutation` | `Mutation` | `变异` |
| `Nav_Replay` | `Replay` | `回放` |
| `Nav_Coverage` | `Coverage` | `覆盖率` |
| `Nav_MetaPatterns` | `MetaPatterns` | `元模式` |
| `Nav_MrDetection` | `MR Detection` | `MR 检测` |
| `Nav_MrRecommendation` | `MR Recommendation` | `MR 推荐` |
| `Nav_MrReportGenerator` | `MR ReportGenerator` | `MR 报告生成` |
| `Nav_Settings` | `Settings` | `设置` |
| `Tray_Home` | `Home` | `主页` |

- [ ] **Step 4: Implement localized navigation map**

Change `MainWindowViewModel` to inject `IAppLocalizationService` and build each item from a stable resource key:

```csharp
private readonly IAppLocalizationService _localization;
private readonly List<(NavigationViewItem Item, string Key)> _localizedNavigation = new();
private readonly List<(NavigationViewItem Item, string Key)> _localizedFooter = new();

public MainWindowViewModel(INavigationService navigationService, IAppLocalizationService localization, LocalizedTextProvider localizedText)
{
    _localization = localization;
    Localization = localizedText;
    _localization.CultureChanged += (_, _) => RefreshLocalizedText();
    if (!_isInitialized)
    {
        InitializeViewModel();
    }
}

public LocalizedTextProvider Localization { get; }

public void RefreshLocalizedText()
{
    ApplicationTitle = _localization.GetString("App_Title");
    foreach (var pair in _localizedNavigation)
    {
        pair.Item.Content = _localization.GetString(pair.Key);
    }

    foreach (var pair in _localizedFooter)
    {
        pair.Item.Content = _localization.GetString(pair.Key);
    }

    foreach (var item in TrayMenuItems)
    {
        if (Equals(item.Tag, "tray_home"))
        {
            item.Header = _localization.GetString("Tray_Home");
        }
    }
}
```

When constructing each `NavigationViewItem`, add it with helper:

```csharp
private NavigationViewItem LocalizedNav(string key, SymbolRegular symbol, Type targetPageType)
{
    var item = new NavigationViewItem
    {
        Content = _localization.GetString(key),
        Icon = new SymbolIcon { Symbol = symbol },
        TargetPageType = targetPageType,
    };
    _localizedNavigation.Add((item, key));
    return item;
}
```

Use a matching `LocalizedFooter` helper for Settings.

Change search placeholder:

```xml
<ui:AutoSuggestBox
    x:Name="AutoSuggestBox"
    PlaceholderText="{Binding ViewModel.Localization[Common_Search]}">
```

- [ ] **Step 5: Run GREEN**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.MainWindowLocalizationTests" --logger "console;verbosity=minimal"
```

Expected GREEN:

```text
Passed: 1, Failed: 0
```

- [ ] **Step 6: Run regression**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n" --logger "console;verbosity=minimal"
```

Expected:

```text
All ClientI18n tests pass.
```

- [ ] **Step 7: Commit**

```powershell
git add MetBench_Client MetBench_SystemMT.Tests
git commit -m "feat(client): localize shell navigation"
```

**Acceptance criteria:**

- All navigation items have resource keys.
- Switching culture refreshes navigation labels without restarting.
- Search placeholder uses `LocalizedTextProvider` binding, not a WPF-only markup extension.
- No page navigation target type changes.

## Task 3: Settings Language Switcher

**Files:**
- Modify: `MetBench_Client/ViewModels/SettingsViewModel.cs`
- Modify: `MetBench_Client/Views/Pages/SettingsPage.xaml`
- Modify: `MetBench_UI.Localization/Resources/Strings.resx`
- Modify: `MetBench_UI.Localization/Resources/Strings.zh-CN.resx`
- Create: `MetBench_SystemMT.Tests/ClientI18n/SettingsLanguageTests.cs`

**Preconditions:**

- Task 2 commit exists.
- Navigation localization tests pass.

**Core steps:**

- [ ] **Step 1: Write failing SettingsViewModel tests**

Create `MetBench_SystemMT.Tests/ClientI18n/SettingsLanguageTests.cs`:

```csharp
using System.Globalization;
using MetBench_UI.Localization;
using MetBench_Client.ViewModels;
using Wpf.Ui;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class SettingsLanguageTests
{
    [WpfFact]
    public void Settings_exposes_english_and_chinese_options()
    {
        var localization = new AppLocalizationService();
        var vm = new SettingsViewModel(localization, new LocalizedTextProvider(localization));

        vm.OnNavigatedTo();

        Assert.Contains(vm.AvailableCultures, c => c.Culture.Name == "en-US");
        Assert.Contains(vm.AvailableCultures, c => c.Culture.Name == "zh-CN");
    }

    [WpfFact]
    public void Changing_selected_culture_updates_localization_service()
    {
        var localization = new AppLocalizationService();
        var vm = new SettingsViewModel(localization, new LocalizedTextProvider(localization));

        vm.ChangeCultureCommand.Execute("zh-CN");

        Assert.Equal("zh-CN", localization.CurrentCulture.Name);

        vm.ChangeCultureCommand.Execute("fr-FR");

        Assert.Equal("en-US", localization.CurrentCulture.Name);
    }
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.SettingsLanguageTests" --logger "console;verbosity=minimal"
```

Expected RED:

```text
FAIL: SettingsViewModel constructor with IAppLocalizationService and LocalizedTextProvider does not exist
```

- [ ] **Step 3: Add Settings resource keys**

Add:

| Key | en-US | zh-CN |
|---|---|---|
| `Settings_Theme` | `Theme` | `主题` |
| `Settings_Theme_Light` | `Light` | `浅色` |
| `Settings_Theme_Dark` | `Dark` | `深色` |
| `Settings_About` | `About MetBench` | `关于 MetBench` |
| `Settings_More` | `More` | `更多` |
| `Settings_ProjectIntroduction` | `MetBench project introduction` | `MetBench 项目介绍` |
| `Settings_SourceCode` | `MetBench SourceCode` | `MetBench 源代码` |
| `Settings_CloneRepository` | `To clone this repository` | `克隆此仓库` |
| `Settings_InvalidCultureFallback` | `Unsupported language falls back to English.` | `不支持的语言将回退到英文。` |

- [ ] **Step 4: Implement Settings language API**

Add to `SettingsViewModel`:

```csharp
private readonly IAppLocalizationService _localization;

public LocalizedTextProvider Localization { get; }

[ObservableProperty]
private IReadOnlyList<AppCultureOption> _availableCultures = Array.Empty<AppCultureOption>();

[ObservableProperty]
private AppCultureOption? _selectedCulture;

public SettingsViewModel(IAppLocalizationService localization, LocalizedTextProvider localizedText)
{
    _localization = localization;
    Localization = localizedText;
}

[RelayCommand]
private void OnChangeCulture(string cultureName)
{
    _localization.SetCulture(new System.Globalization.CultureInfo(cultureName));
    SelectedCulture = AvailableCultures.FirstOrDefault(c => c.Culture.Name == _localization.CurrentCulture.Name);
}
```

In `InitializeViewModel()`:

```csharp
AvailableCultures = _localization.AvailableCultures;
SelectedCulture = AvailableCultures.FirstOrDefault(c => c.Culture.Name == _localization.CurrentCulture.Name);
```

- [ ] **Step 5: Localize SettingsPage XAML**

Replace static text:

```xml
<TextBlock Text="{Binding ViewModel.Localization[Settings_Personalization]}" />
<TextBlock Text="{Binding ViewModel.Localization[Settings_Theme]}" />
<RadioButton Content="{Binding ViewModel.Localization[Settings_Theme_Light]}" />
<RadioButton Content="{Binding ViewModel.Localization[Settings_Theme_Dark]}" />
<TextBlock Text="{Binding ViewModel.Localization[Settings_Language]}" />
<ComboBox
    ItemsSource="{Binding ViewModel.AvailableCultures}"
    SelectedItem="{Binding ViewModel.SelectedCulture, Mode=TwoWay}"
    DisplayMemberPath="DisplayName" />
<Button
    Command="{Binding ViewModel.ChangeCultureCommand}"
    CommandParameter="{Binding ViewModel.SelectedCulture.Culture.Name}"
    Content="{Binding ViewModel.Localization[Settings_Language]}" />
```

Keep the existing theme controls.

- [ ] **Step 6: Run GREEN**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.SettingsLanguageTests" --logger "console;verbosity=minimal"
```

Expected:

```text
Passed: 2, Failed: 0
```

- [ ] **Step 7: Commit**

```powershell
git add MetBench_Client MetBench_SystemMT.Tests
git commit -m "feat(client): add settings language switcher"
```

**Acceptance criteria:**

- Settings exposes English and Chinese choices.
- Unsupported culture falls back to English.
- Settings page labels are localized.
- Theme behavior remains unchanged.

## Task 4: System-MT Page Migration

**Files:**
- Modify: `MetBench_Client/Views/Pages/SystemMtExecutionPage.xaml`
- Modify: `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml`
- Modify: `MetBench_Client/Views/Pages/SystemMtSutCatalogPage.xaml`
- Modify: `MetBench_Client/Views/Pages/SystemMtEquationCatalogPage.xaml`
- Modify: `MetBench_Client/Views/Pages/SystemMtSampleCaseCatalogPage.xaml`
- Modify: `MetBench_Client/Views/Pages/SystemMtExecutionHistoryPage.xaml`
- Modify: `MetBench_Client/Views/Pages/AnomalyListPage.xaml`
- Modify: `MetBench_Client/Views/Pages/ReplayResultPage.xaml`
- Modify: `MetBench_Client/Views/Pages/MTReportGeneratorPage.xaml`
- Modify: `MetBench_UI.Localization/Resources/Strings.resx`
- Modify: `MetBench_UI.Localization/Resources/Strings.zh-CN.resx`
- Create: `MetBench_SystemMT.Tests/ClientI18n/SystemMtPageResourceTests.cs`

**Preconditions:**

- Task 3 commit exists.
- Settings language switcher tests pass.
- VM can build `MetBench.sln`.

**Core steps:**

- [ ] **Step 1: Write failing page resource key test**

Create `MetBench_SystemMT.Tests/ClientI18n/SystemMtPageResourceTests.cs`:

```csharp
using System.Globalization;
using System.Resources;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class SystemMtPageResourceTests
{
    [Fact]
    public void System_mt_page_resource_keys_exist_in_english_and_chinese()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(MetBench_UI.Localization.AppLocalizationService).Assembly);

        var keys = new[]
        {
            "SystemMt_Run",
            "SystemMt_SelectedMr",
            "SystemMt_Source",
            "SystemMt_FollowUp",
            "SystemMt_Result",
            "Catalog_LoadedManifests",
            "Catalog_LoadedSuts",
            "Catalog_LoadedEquations",
            "Catalog_LoadedSampleCases",
            "History_ExecutionHistory",
            "Anomaly_Title",
            "Anomaly_ApplyTransition",
            "Replay_Title",
            "ReportGenerator_Title",
            "ReportGenerator_Export"
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))), key);
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))), key);
        }
    }
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.SystemMtPageResourceTests" --logger "console;verbosity=minimal"
```

Expected RED:

```text
FAIL: at least one System-MT resource key is missing
```

- [ ] **Step 3: Add page resources**

Add the exact keys from the test to both `.resx` files. Use concise translations:

| Key | en-US | zh-CN |
|---|---|---|
| `SystemMt_Run` | `Run` | `运行` |
| `SystemMt_SelectedMr` | `Selected MR` | `选定 MR` |
| `SystemMt_Source` | `Source` | `源输入` |
| `SystemMt_FollowUp` | `Follow-up` | `后续输入` |
| `SystemMt_Result` | `Result` | `结果` |
| `Catalog_LoadedManifests` | `Loaded manifests` | `已加载清单` |
| `Catalog_LoadedSuts` | `Loaded SUTs` | `已加载 SUT` |
| `Catalog_LoadedEquations` | `Loaded equations` | `已加载方程` |
| `Catalog_LoadedSampleCases` | `Loaded sample cases` | `已加载样例` |
| `History_ExecutionHistory` | `Execution History` | `执行历史` |
| `Anomaly_Title` | `Anomalies` | `异常` |
| `Anomaly_ApplyTransition` | `Apply transition` | `应用状态转换` |
| `Replay_Title` | `Replay` | `回放` |
| `ReportGenerator_Title` | `MR Report Generator` | `MR 报告生成器` |
| `ReportGenerator_Export` | `Export` | `导出` |

- [ ] **Step 4: Convert static labels page by page**

For each listed XAML page:

1. Ensure the page or page view model exposes `LocalizedTextProvider` as `Localization`.
2. Replace static user-facing `Text`, `Content`, `Header`, `PlaceholderText`, and `ToolTip` values with bindings like `{Binding ViewModel.Localization[KeyName]}` or `{Binding Localization[KeyName]}` depending on the existing page DataContext pattern.
3. Do not localize bound scientific data, MR ids, SUT ids, metric names, evidence strings, or persisted values.

Example:

```xml
<Button Content="{Binding ViewModel.Localization[SystemMt_Run]}" />
<TextBlock Text="{Binding ViewModel.Localization[Anomaly_Title]}" />
<ui:Button Content="{Binding ViewModel.Localization[ReportGenerator_Export]}" />
```

- [ ] **Step 5: Run GREEN**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.SystemMtPageResourceTests" --logger "console;verbosity=minimal"
```

Expected:

```text
Passed: 1, Failed: 0
```

- [ ] **Step 6: Build WPF client**

```powershell
dotnet build MetBench.sln
```

Expected:

```text
0 Error(s)
```

- [ ] **Step 7: Commit**

```powershell
git add MetBench_Client MetBench_SystemMT.Tests
git commit -m "feat(client): localize system mt pages"
```

**Acceptance criteria:**

- System-MT page chrome is localized in Chinese and English.
- Bound scientific identifiers remain unchanged.
- WPF build passes on VM.

## Task 5: Legacy Page Migration

**Files:**
- Modify: `MetBench_Client/Views/Pages/MRDisplayPage.xaml`
- Modify: `MetBench_Client/Views/Pages/MRManagementPage.xaml`
- Modify: `MetBench_Client/Views/Pages/ApplicationManagementPage.xaml`
- Modify: `MetBench_Client/Views/Pages/DomainManagementPage.xaml`
- Modify: `MetBench_Client/Views/Pages/MTExecutionPage.xaml`
- Modify: `MetBench_Client/Views/Pages/DiscoveryPage.xaml`
- Modify: `MetBench_Client/Views/Pages/CandidateReviewPage.xaml`
- Modify: `MetBench_Client/Views/Pages/MutationCampaignPage.xaml`
- Modify: `MetBench_Client/Views/Pages/CoverageDashboardPage.xaml`
- Modify: `MetBench_Client/Views/Pages/MetaPatternsPage.xaml`
- Modify: `MetBench_Client/Views/Pages/AutoDetectMRPage.xaml`
- Modify: `MetBench_Client/Views/Pages/MRRecommendationPage.xaml`
- Modify: `MetBench_UI.Localization/Resources/Strings.resx`
- Modify: `MetBench_UI.Localization/Resources/Strings.zh-CN.resx`
- Create: `MetBench_SystemMT.Tests/ClientI18n/LegacyPageResourceTests.cs`

**Preconditions:**

- Task 4 commit exists.
- WPF client builds.

**Core steps:**

- [ ] **Step 1: Write failing legacy resource key test**

Create `MetBench_SystemMT.Tests/ClientI18n/LegacyPageResourceTests.cs`:

```csharp
using System.Globalization;
using System.Resources;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class LegacyPageResourceTests
{
    [Fact]
    public void Legacy_page_resource_keys_exist_in_english_and_chinese()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(MetBench_UI.Localization.AppLocalizationService).Assembly);

        var keys = new[]
        {
            "Legacy_Add",
            "Legacy_Delete",
            "Legacy_Modify",
            "Legacy_Query",
            "Legacy_Save",
            "Legacy_Cancel",
            "Legacy_Name",
            "Legacy_Description",
            "Legacy_Domain",
            "Legacy_Application",
            "Legacy_Recommendation",
            "Legacy_Detection",
            "Legacy_Coverage"
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))), key);
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))), key);
        }
    }
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.LegacyPageResourceTests" --logger "console;verbosity=minimal"
```

Expected RED:

```text
FAIL: at least one Legacy_* key is missing
```

- [ ] **Step 3: Add legacy common resources**

Add:

| Key | en-US | zh-CN |
|---|---|---|
| `Legacy_Add` | `Add` | `新增` |
| `Legacy_Delete` | `Delete` | `删除` |
| `Legacy_Modify` | `Modify` | `修改` |
| `Legacy_Query` | `Query` | `查询` |
| `Legacy_Save` | `Save` | `保存` |
| `Legacy_Cancel` | `Cancel` | `取消` |
| `Legacy_Name` | `Name` | `名称` |
| `Legacy_Description` | `Description` | `描述` |
| `Legacy_Domain` | `Domain` | `领域` |
| `Legacy_Application` | `Application` | `应用` |
| `Legacy_Recommendation` | `Recommendation` | `推荐` |
| `Legacy_Detection` | `Detection` | `检测` |
| `Legacy_Coverage` | `Coverage` | `覆盖率` |

- [ ] **Step 4: Convert legacy page static labels**

For each page in this task, localize only stable UI chrome:

```xml
<Button Content="{Binding ViewModel.Localization[Legacy_Add]}" />
<Button Content="{Binding ViewModel.Localization[Legacy_Query]}" />
<TextBlock Text="{Binding ViewModel.Localization[Legacy_Description]}" />
```

Do not localize database entity values, MR names, generated candidate text, or algorithm output.

- [ ] **Step 5: Run GREEN**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.LegacyPageResourceTests" --logger "console;verbosity=minimal"
```

Expected:

```text
Passed: 1, Failed: 0
```

- [ ] **Step 6: Build WPF client**

```powershell
dotnet build MetBench.sln
```

Expected:

```text
0 Error(s)
```

- [ ] **Step 7: Commit**

```powershell
git add MetBench_Client MetBench_SystemMT.Tests
git commit -m "feat(client): localize legacy pages"
```

**Acceptance criteria:**

- High-traffic legacy page controls have Chinese and English resource keys.
- Dynamic data remains untouched.
- Build remains green.

## Task 6: Abnormal Scenarios And Fallback UX

**Files:**
- Modify: `MetBench_UI.Localization/AppLocalizationService.cs`
- Modify: `MetBench_UI.Localization/Resources/Strings.resx`
- Modify: `MetBench_UI.Localization/Resources/Strings.zh-CN.resx`
- Create: `MetBench_SystemMT.Tests/ClientI18n/LocalizationAbnormalScenarioTests.cs`

**Preconditions:**

- Task 5 commit exists.
- All `ClientI18n` tests pass.

**Core steps:**

- [ ] **Step 1: Write abnormal scenario tests**

Create `MetBench_SystemMT.Tests/ClientI18n/LocalizationAbnormalScenarioTests.cs`:

```csharp
using System.Globalization;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class LocalizationAbnormalScenarioTests
{
    [Fact]
    public void Null_or_empty_key_returns_visible_fallback()
    {
        var service = new AppLocalizationService();

        Assert.Equal("??null??", service.GetString(null!));
        Assert.Equal("??empty??", service.GetString(""));
        Assert.Equal("??empty??", service.GetString("   "));
    }

    [Fact]
    public void Neutral_chinese_culture_maps_to_simplified_chinese()
    {
        var service = new AppLocalizationService();

        service.SetCulture(new CultureInfo("zh"));

        Assert.Equal("zh-CN", service.CurrentCulture.Name);
        Assert.Equal("系统级蜕变测试", service.GetString("Nav_SystemMtExecution"));
    }

    [Fact]
    public void Neutral_english_culture_maps_to_english()
    {
        var service = new AppLocalizationService();

        service.SetCulture(new CultureInfo("en"));

        Assert.Equal("en-US", service.CurrentCulture.Name);
        Assert.Equal("System MT", service.GetString("Nav_SystemMtExecution"));
    }
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.LocalizationAbnormalScenarioTests" --logger "console;verbosity=minimal"
```

Expected RED:

```text
FAIL: null/empty or neutral culture behavior is not implemented
```

- [ ] **Step 3: Implement fallback behavior**

Update `AppLocalizationService.SetCulture`:

```csharp
public void SetCulture(CultureInfo culture)
{
    var selected = culture.TwoLetterISOLanguageName switch
    {
        "zh" => Chinese,
        "en" => English,
        _ => Cultures.FirstOrDefault(c => c.Culture.Name == culture.Name)?.Culture ?? English,
    };

    if (selected.Name == CurrentCulture.Name)
    {
        return;
    }

    CurrentCulture = selected;
    CultureInfo.CurrentUICulture = selected;
    CultureChanged?.Invoke(this, EventArgs.Empty);
}
```

Update `GetString`:

```csharp
public string GetString(string key)
{
    if (key is null)
    {
        return "??null??";
    }

    if (string.IsNullOrWhiteSpace(key))
    {
        return "??empty??";
    }

    var value = Strings.ResourceManager.GetString(key, CurrentCulture);
    return string.IsNullOrWhiteSpace(value) ? $"??{key}??" : value;
}
```

- [ ] **Step 4: Run GREEN**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n.LocalizationAbnormalScenarioTests" --logger "console;verbosity=minimal"
```

Expected:

```text
Passed: 3, Failed: 0
```

- [ ] **Step 5: Run full ClientI18n regression**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n" --logger "console;verbosity=minimal"
```

Expected:

```text
All ClientI18n tests pass.
```

- [ ] **Step 6: Commit**

```powershell
git add MetBench_UI.Localization MetBench_SystemMT.Tests
git commit -m "feat(client): harden localization fallbacks"
```

**Acceptance criteria:**

- Null key, empty key, missing key, unsupported culture, `zh`, and `en` are covered.
- Abnormal behavior is visible and deterministic.
- No exceptions are thrown for user-facing fallback cases.

## Task 7: VM UIA System Test And Evidence

**Files:**
- Modify or create: `tools/smokeshot/Flows.cs`
- Modify or create: `tools/smokeshot/Program.cs`
- Modify or create: `tools/smokeshot/UiaHelpers.cs`
- Create: `docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/README.md`
- Create: `docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/vm-status.jsonl`
- Create: `docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/vm-summary.md`
- Create screenshots listed in `Execution Model`.

**Preconditions:**

- Task 6 commit exists.
- `dotnet build MetBench.sln` exits 0.
- VM display session is available.

**Core steps:**

- [ ] **Step 1: Write failing UIA smokeshot test command**

Extend `tools/smokeshot` with a command named `i18n-smoke`. It must fail before implementation with:

```text
Unknown command: i18n-smoke
```

Run:

```powershell
dotnet run --project tools\smokeshot\smokeshot.csproj -- i18n-smoke --out docs\superpowers\specs\2026-05-30-client-i18n-vm-evidence
```

- [ ] **Step 2: Implement minimal UIA i18n smoke**

The command must:

1. Launch or attach to `MetBench_Client.exe`.
2. Open Settings.
3. Switch to Chinese.
4. Capture `02-settings-language-switch-zh.png`.
5. Capture navigation in Chinese as `04-navigation-zh.png`.
6. Navigate to System MT and capture `06-system-mt-page-zh.png`.
7. Switch to English.
8. Capture `03-settings-language-switch-en.png`.
9. Capture navigation in English as `05-navigation-en.png`.
10. Navigate to System MT and capture `07-system-mt-page-en.png`.
11. Force unsupported culture `fr-FR` through Settings or a test hook and capture `08-invalid-culture-fallback.png`.
12. Display a missing-key probe and capture `09-missing-key-fallback.png`.

If Settings UI cannot directly force invalid culture or missing key, generate the last two screenshots from a small WPF diagnostic window that uses `IAppLocalizationService`. Document that in `vm-summary.md`.

- [ ] **Step 3: Run UIA smoke**

```powershell
dotnet build MetBench.sln
dotnet build tools\smokeshot\smokeshot.csproj
dotnet run --project tools\smokeshot\smokeshot.csproj -- i18n-smoke --out docs\superpowers\specs\2026-05-30-client-i18n-vm-evidence
```

Expected:

```text
i18n-smoke PASS
```

- [ ] **Step 4: Write VM evidence summary**

Create `vm-summary.md`:

```markdown
# Client I18n VM Evidence Summary

| Field | Value |
|---|---|
| Branch | codex/client-i18n |
| Languages | en-US, zh-CN |
| Localization core | .NET resx ResourceManager + IAppLocalizationService |
| ClientI18n tests | PASS |
| Full MetBench_SystemMT.Tests | actual result |
| WPF build | PASS |
| UIA screenshots | 9/9 PASS |
| Final decision | PASS or BLOCKED |

## Abnormal Scenarios

- Unsupported culture fallback: PASS/BLOCKED
- Missing key fallback: PASS/BLOCKED
```

- [ ] **Step 5: Commit**

```powershell
git add tools/smokeshot docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence
git commit -m "test(client): add i18n uia smoke evidence"
```

**Acceptance criteria:**

- UIA screenshots exist and are non-empty.
- Chinese and English screenshots visibly differ.
- Invalid culture and missing key scenarios are captured.
- `vm-status.jsonl` records setup, RED/GREEN checks, UIA smoke, final.

## Task 8: Final Regression, PR Package, And Coordinator Handoff

**Files:**
- Modify: `docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/vm-summary.md`
- No production files unless fixing review findings through TDD.

**Preconditions:**

- Task 7 commit exists.
- All task-level spec and quality reviews approved.

**Core steps:**

- [ ] **Step 1: Run final focused tests**

```powershell
dotnet test MetBench_SystemMT.Tests --filter "FullyQualifiedName~ClientI18n" --logger "console;verbosity=minimal"
```

Expected:

```text
ClientI18n tests PASS, 0 failed.
```

- [ ] **Step 2: Run full cloud-safe test suite**

```powershell
dotnet test MetBench_SystemMT.Tests --logger "console;verbosity=minimal"
```

Expected:

```text
0 failed. Environment-gated OpenMC/OpenMOC skips are allowed only with explicit skip reasons.
```

- [ ] **Step 3: Build WPF solution**

```powershell
dotnet build MetBench.sln
```

Expected:

```text
0 Error(s)
```

- [ ] **Step 4: Run final code review subagent**

Dispatch final reviewer with model `gpt5.4`:

```text
Review the full codex/client-i18n branch against docs/superpowers/plans/2026-05-30-metbench-client-multilingual-i18n-plan.md.
Check spec compliance, TDD evidence, WPF localization design, resource parity, abnormal scenarios, and UIA evidence.
Return findings first, ordered by severity, with file/line references.
```

- [ ] **Step 5: Push branch**

```powershell
git status --short --branch
git push -u origin codex/client-i18n
```

Expected:

```text
Working tree clean.
Branch pushed to origin/codex/client-i18n.
```

- [ ] **Step 6: Coordinator validates**

Coordinator runs on macOS:

```bash
rtk git fetch origin codex/client-i18n
rtk git diff --check origin/main..origin/codex/client-i18n
rtk git show origin/codex/client-i18n:docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/vm-summary.md
rtk git show origin/codex/client-i18n:docs/superpowers/specs/2026-05-30-client-i18n-vm-evidence/vm-status.jsonl | rtk tail -n 20
```

Expected:

```text
diff --check clean.
vm-summary final decision PASS.
vm-status final event PASS.
```

**Acceptance criteria:**

- Branch is pushed and ready for PR.
- Focused and broad tests are recorded.
- WPF build is recorded.
- UIA screenshots are present.
- Final reviewer has no blocking findings.

## Subagent Task Summary

| Task | Implementer model | Spec reviewer model | Quality reviewer model | Main acceptance |
|---|---|---|---|---|
| 1 Infrastructure | `gpt5.4` | `gpt5.4` | `gpt5.4` | Resource/service tests green |
| 2 Shell navigation | `gpt5.4` | `gpt5.4` | `gpt5.4` | Navigation refreshes between zh/en |
| 3 Settings switcher | `gpt5.4` | `gpt5.4` | `gpt5.4` | User can choose zh/en; invalid fallback covered |
| 4 System-MT pages | `gpt5.4` | `gpt5.4` | `gpt5.4` | System-MT UI chrome localized |
| 5 Legacy pages | `gpt5.4` | `gpt5.4` | `gpt5.4` | Common legacy actions localized |
| 6 Abnormal scenarios | `gpt5.4` | `gpt5.4` | `gpt5.4` | Null/empty/missing/unsupported culture tests |
| 7 UIA evidence | `gpt5.4` | `gpt5.4` | `gpt5.4` | 9 screenshots and final VM summary |
| 8 Final handoff | `gpt5.4` | `gpt5.4` | `gpt5.4` | Branch pushed, coordinator accepts evidence |

## Final Self-Review

Spec coverage:

- Mature multilingual foundation: covered by official .NET `.resx` / `ResourceManager`, `IAppLocalizationService`, and `LocalizedTextProvider` in Tasks 1-3.
- Avalonia support: core resources and culture switching are UI-neutral; WPF work is limited to current-client bindings that an Avalonia adapter can reuse.
- Chinese and English: covered in every resource test and UIA screenshot task.
- Infrastructure first: Tasks 1-3 precede page migration.
- Page-by-page migration: Tasks 4-5 split System-MT and legacy pages.
- TDD: every production task starts with RED, then GREEN.
- Subagent-driven development: required in execution model and task summary.
- VM execution: explicit VM path, startup prompt, evidence directory, coordinator validation.
- Normal and abnormal scenarios: normal zh/en culture switch plus unsupported culture and missing key fallback.

Placeholder scan:

- No `TBD`, `TODO`, or unspecified "add tests" steps remain.
- Every task lists exact files, commands, expected outputs, and acceptance criteria.

Type consistency:

- `IAppLocalizationService`, `AppLocalizationService`, and `AppCultureOption` are defined before use.
- `ClientI18n` test namespace is consistent across tasks.
- Resource key names are stable across tests and XAML examples.
