# Working on MetBench (Claude / Agent Notes)

This file is for AI agents and contributors who land in the repo cold. It
captures the **non-obvious conventions** the codebase has settled on so new
work fits in cleanly. For project intent and the staged plan, see
[`AGENTS.md`](AGENTS.md). For build/test, see [`README.md`](README.md).

## 项目状态概览

> 路线图与分阶段计划见 [`AGENTS.md`](AGENTS.md)；本节只给冷启动 agent 一个全局快照。

### 1. 目标

反应堆物理科学计算软件的**系统级蜕变测试（System-level MT）基准平台** —— 以元模式
驱动的蜕变关系（MR）自动发现并执行 MT，检出科学计算软件缺陷。现升级为 P-series MR
库的「可执行存储 + MT 执行载体」。

### 2. 核心功能与子系统

**核心 —— 系统级 MT 流程**：测试输入生成 → 衍生输入转换 → 执行 SUT 被测程序 →
验证源输出与衍生输出是否满足蜕变关系。

**配套功能**：

- **结果可视化 + 报表**：图表展示 + 4 端（PDF / Word / Excel / HTML）报告生成。
- **CRUD**：应用程序 / 数学物理方程 / 蜕变关系 / 基础算例 / 测试过程数据。
- **非结构化文件支持**：针对输入、输出为非结构化文件的 SUT，提供文件解析、参数
  映射、文件生成。
- **MR 识别模块**：针对「蜕变关系从 0 到 1」的问题，提供可持续集成新识别技术
  （如 LLM-driven 识别）的框架。

对应实现子系统：System-MT 引擎 + Launcher facade、LiteDB 持久化、4 SUT 适配器
（OpenMOC / OpenMC / home-grown 热传导）、MR Discovery + 三层验证、Anomaly 异常
调查、Mutation / Coverage / Trend 分析、WPF 客户端、BDD 测试（xUnit + Reqnroll）。

### 3. 完成情况（已实现的主要功能）

- **v2.1.0 / .1 / .2 已发布**；Stage 1-7 全部交付。
- 已实现：System-MT 的 BDD 执行、输入生成与衍生输入推导、OpenMOC / OpenMC 接入、
  批量执行 + 4 端报表、v2 的 8 个 BLL.Core 子系统（Discovery / Anomaly / Mutation /
  Coverage / Trend / Reporting 等）、multi-LLM 共识（60/60 真实跑通、100% accuracy）、
  scenario→MR 命名统一。
- cloud baseline ~560 测试 0 fail；Windows UAT 双轮 PASS。
- polish 批次：HandyControl 移除、UAT runbook 对齐、Anomaly severity / category 分级。

### 4. 尚待完善

- **Stage 8 / v2.2 主线未启动** —— 5 方程 × 4 程序类型 × 5 元模式的 MR 库
  （17 cells、84 候选 MR）。*必要性：这是论文核心交付，当前仅覆盖 boltzmann 方程
  + 少量 MR；地基 5D 索引 schema（Phase 8.0）须先落地，否则后续工作无处挂载。*
- **5 个 UAT UI 缺口** —— Dashboard 导航入口、HTML 报告内嵌查看等。*必要性：部分
  后端能力已实现但 UI 上不可见，价值未释放。*
- **DP-3 配置绑定** —— severity 阈值的 `appsettings` 绑定（WPF 侧）未接，现回退默认值。
- **F11 m_adj 路径、第 5 个 SUT** —— 受外部依赖（OpenMOC 伴随模式、商业程序获取）
  阻塞，被动监控中。

## Project topology

| Project | Target framework | Where it runs | Notes |
|---------|------------------|---------------|-------|
| `MetBench_BLL.Core/` | `net8.0` | Anywhere (incl. Linux CI) | All cross-platform business logic. **System-MT runner, adapters, persistence contracts, reporting renderer, launcher facade live here.** |
| `MetBench_Domain/`, `MetBench_IDAL/` | `net8.0` | Anywhere | Legacy method-level entities + DAL contracts. |
| `MetBench_DAL/` | `net8.0` | Anywhere | LiteDB-backed implementations. References `MetBench_BLL.Core` for the new system-MT result repository. |
| `MetBench_BLL/` | `net8.0` | Anywhere (incl. Linux CI) | Legacy method-level MT business orchestration + cross-platform `MTVisualizationSerive` (LiveCharts data, no WPF) + Word/Excel/PDF report generators. **WPF chart plotters were extracted to `MetBench_Client/Services/Plotting/`** so BLL stays portable. |
| `MetBench_Client/` | **`net8.0-windows7.0`**, `<UseWPF>true</UseWPF>` | Windows only | The WPF UI app. Entry point. |
| `MetBench_SystemMT.Tests/` | `net8.0` | Anywhere (incl. Linux CI) | All tests. xUnit + Reqnroll. |

**Hard rule for cross-environment work**:

- Code that needs to run in CI / Linux cloud → `MetBench_BLL.Core/` / `MetBench_DAL/` / `MetBench_BLL/` (all `net8.0`, all build on Linux).
- Code that touches WPF (XAML, dispatcher, WinForms interop, Win32, LiveCharts WPF chart controls) → `MetBench_Client/` only (`net8.0-windows7.0`). Linux dotnet SDK ships without `Microsoft.NET.Sdk.WindowsDesktop.targets`, so `dotnet build MetBench_Client.csproj` **fails on Linux** with MSB4019. Cloud agents can edit WPF source but cannot compile it.

## WPF stack (do not mix in alternatives)

| Concern | Library | Notes |
|---------|---------|-------|
| UI controls + theming | **`Wpf.Ui`** (lepoco WPF-UI) | XAML namespace `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`. Use `ui:Button`, `ui:DataGrid`, `ui:TextBox`, `{ui:SymbolIcon Symbol=...}`. Theme keys: `{DynamicResource ApplicationBackgroundBrush}`, `{DynamicResource TextFillColorPrimaryBrush}`, `{DynamicResource ControlStrokeColorDefaultBrush}`. |
| MVVM | **`CommunityToolkit.Mvvm`** | `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`, `[NotifyCanExecuteChangedFor(...)]`. Don't write manual `INotifyPropertyChanged`. |
| DI / hosting | **`Microsoft.Extensions.Hosting`** generic host | Registered in `MetBench_Client/App.xaml.cs` via `Host.CreateDefaultBuilder().ConfigureServices(...)`. Service locator: `App.GetService<T>()`. |
| Page-based navigation | `Wpf.Ui` `INavigationService` + `INavigableView<TViewModel>` | Pages implement `INavigableView<TViewModel>` from `Wpf.Ui.Controls`. ViewModels implement `INavigationAware` for nav lifecycle hooks (`OnNavigatedTo` / `OnNavigatedFrom`). |
| Behaviors / event-to-command | **`HandyControl`** (legacy, in 6 files) | `xmlns:hc="https://handyorg.github.io/handycontrol"`, used as `hc:EventToCommand`, `hc:Pagination`. New code should prefer `Microsoft.Xaml.Behaviors.Wpf` when adding fresh views; HandyControl removal is tracked as a follow-up. |
| Charts | `LiveChartsCore.SkiaSharpView.WPF` | Used for visualization on existing pages. |
| HTML hosting in WPF | `Microsoft.Web.WebView2` | Available; suitable for embedding `HtmlSystemMtResultReportRenderer` output. |

`Stylet` is referenced and used on `MTExecutionPage.xaml` only (action target binding `s:View.ActionTarget`). Do **not** introduce Stylet on new pages — match the simpler pattern used by `SettingsPage`.

## Page ↔ ViewModel pairing pattern

Every page is a triple of files matched 1:1 with a ViewModel:

```
Views/Pages/SomePage.xaml          (the XAML; root <Page>)
Views/Pages/SomePage.xaml.cs       (code-behind; almost empty)
ViewModels/SomeViewModel.cs        (the logic)
```

### XAML root template

```xml
<Page x:Class="MetBench_Client.Views.Pages.SomePage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      ui:Design.Background="{DynamicResource ApplicationBackgroundBrush}"
      Foreground="{DynamicResource TextFillColorPrimaryBrush}"
      mc:Ignorable="d">
  <!-- bindings reference {Binding ViewModel.PropertyName} -->
</Page>
```

### Code-behind template

```csharp
using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages
{
    public partial class SomePage : INavigableView<ViewModels.SomeViewModel>
    {
        public ViewModels.SomeViewModel ViewModel { get; }

        public SomePage(ViewModels.SomeViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;     // bindings use {Binding ViewModel.X}
            InitializeComponent();
        }
    }
}
```

### ViewModel template

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public partial class SomeViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty]
        private string _someText = string.Empty;

        public void OnNavigatedTo() { /* lazy init here */ }
        public void OnNavigatedFrom() { }

        [RelayCommand]
        private async Task DoSomethingAsync() { /* ... */ }
    }
}
```

### DI registration (App.xaml.cs)

For every Page+ViewModel pair, add **two scoped registrations**:

```csharp
services.AddScoped<Views.Pages.SomePage>();
services.AddScoped<ViewModels.SomeViewModel>();
```

### Navigation menu entry (MainWindowViewModel.cs)

Add a `NavigationViewItem` in `InitializeViewModel()`:

```csharp
new NavigationViewItem()
{
    Content = "Some Page",
    Icon = new SymbolIcon { Symbol = SymbolRegular.Play24 },
    TargetPageType = typeof(Views.Pages.SomePage)
},
```

## System-MT facade rules (Stage 4 / post-W12 命名统一)

The launcher facade in `MetBench_BLL.Core/SystemMT/Launcher/` exposes the **only** entry point WPF code should use to run a system-level metamorphic test:

```csharp
ISystemMtMrLauncher
    Task<IReadOnlyList<MrSummary>> ListAvailableAsync(ct)
    Task<MrRunResult> RunAsync(mrId, parameterOverrides?, ct)
    Task<IReadOnlyList<MrRunResult>> RunBatchAsync(requests, progress?, ct)
```

> 历史命名（已废弃）：`ISystemMtScenarioLauncher` / `ScenarioDescriptor` / `ScenarioRunResult` / `scenarioId` 等。post-W12（PR #58）彻底改名以消除与 BDD Gherkin Scenario 的撞名混淆。Persistence 层的 `ScenarioName` 字段同步改为 `MrName` 并附 LiteDB 自动 schema migration（PR #62）。详见 [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md) §8。

**Type-leakage rule** — public method signatures use only:

- primitives, `string`, `Dictionary<string, string>`,
- record DTOs from `MetBench_BLL.SystemMT.Launcher.*`（`MrSummary` / `MrRunResult` / `BatchMrRunRequest` / `BatchProgress`），
- `SystemMtResultRecord` from `MetBench_BLL.SystemMT.Persistence`.

Do **not** expose `MrTransformation`, `SystemMtTask`, `SystemMtRunner`, `IMrAssertion`, `SystemMtResult`, `SystemMtCase`, or any other engine-internal type through the facade. WPF must remain insulated so the planned IR refactor can change internals without breaking views.

DI registration for system-MT (in `App.xaml.cs`):

```csharp
services.AddSingleton(provider => new LauncherOptions(
    SutRoot: Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, "SUT"),
    SystemPython: OperatingSystem.IsWindows() ? "python" : "python3",
    OpenMocPython: Environment.GetEnvironmentVariable("METBENCH_OPENMOC_PYTHON")
        ?? (OperatingSystem.IsWindows() ? "python" : "python3"),
    OpenMcPython: Environment.GetEnvironmentVariable("METBENCH_OPENMC_PYTHON")
        ?? (OperatingSystem.IsWindows() ? "python" : "python3")));

services.AddSingleton<ISystemMtResultRepository>(provider =>
{
    var dataDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
    return new LiteDbSystemMtResultRepository($"Filename={Path.Combine(dataDir, "SystemMT.Litedb")}");
});

services.AddSingleton<ISystemMtMrLauncher, SystemMtMrLauncher>();
services.AddSingleton<ISystemMtResultReportRenderer, HtmlSystemMtResultReportRenderer>();
```

The system-MT LiteDB file (`SystemMT.Litedb`) is intentionally separate from the legacy MetBench DB (`MR.Litedb`) — `LiteDbSystemMtResultRepository` uses an isolated `BsonMapper` so the two schemas never interact.

## Async & UI-thread conventions

- ViewModels marshal nothing manually onto the UI thread; `[ObservableProperty]` setters dispatch via `INotifyPropertyChanged` and WPF handles cross-thread re-entry for binding targets.
- `async void` is reserved for `INavigationAware.OnNavigatedTo` only (event-handler-style entry point). Everywhere else use `Task` / `async Task`.
- Long-running operations must surface progress through observable properties; do **not** block the dispatcher with `.Result` or `.Wait()`.

## Build & test

| Command | Where it works |
|---------|----------------|
| `dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj` | Linux + Windows |
| `dotnet test MetBench_SystemMT.Tests` | Linux + Windows |
| `dotnet build MetBench.sln` | **Windows only** (WPF SDK targets) |
| `dotnet build MetBench_Client/MetBench_Client.csproj` | **Windows only** |
| `dotnet run --project MetBench_Client` | **Windows only** |

CI (`.github/workflows/dotnet-test.yml`, `ubuntu-24.04`) runs **only** the cross-platform projects. WPF code is not compiled by CI; visual / runtime verification is the developer's responsibility on a Windows host (Parallels VM or otherwise).

OpenMOC + OpenMC tests skip cleanly when the respective venv is missing (`OpenMocTestPaths.OpenMocImportable()` / `OpenMcTestPaths.OpenMcImportable()`); CI does not install either. To run them locally, use `.claude/web-setup.sh` (Linux) or set `METBENCH_OPENMOC_PYTHON` / `METBENCH_OPENMC_PYTHON` (any OS) to a Python where the package is importable. OpenMC additionally requires the `openmc` binary on PATH (or in the same venv `bin/`); the setup script handles this via cmake source build.

## Cross-environment workflow (Linux cloud + Windows VM)

| Track | Lives in | What it does |
|-------|----------|--------------|
| Cloud | this Claude Code Web session | BLL.Core / DAL / SystemMT.Tests / docs / CI workflow. Pushes PRs that CI gates. |
| VM | Windows + VS 2022 (e.g. Parallels) | WPF UI work in `MetBench_Client/` and (rarely) `MetBench_BLL/`. Builds, runs, and visually verifies. Pushes PRs targeting `main`. |

Tracks coordinate via the launcher facade: Cloud owns the contract; VM consumes it. Cloud agents must not modify `*.xaml*` files in `MetBench_Client/` or `MetBench_BLL/` without explicit user direction (they cannot compile them locally to verify the change). VM agents must not modify `MetBench_BLL.Core/SystemMT/*` public types without first proposing a Cloud-side change (CI catches breakage).

## v2 BLL.Core namespaces (P1-P8 ship)

Once a feature has been cloud-side TDD-tested, it lives in one of these
`MetBench_BLL.Core/` subtrees:

| Namespace | Purpose | Key types |
|-----------|---------|-----------|
| `MetBench_BLL.SystemMT.*` | Pipeline + Launcher + Persistence + Reporting | `SystemMtPipeline`, `ISystemMtMrLauncher`, `HtmlSystemMtResultReportRenderer` |
| `MetBench_BLL.SystemMT.Anomaly` | Anomaly viewer + commonality | `AnomalyService`, `CommonalityReport` |
| `MetBench_BLL.Discovery` | MR Discovery + Validation | `IMRDiscoverer`, `DiscoveryService`, `ValidationService`, `ILlmGateway` |
| `MetBench_BLL.Discovery.Validators` | Day-1 validators | `EmpiricalValidator`, `TheoreticalLlmValidator`, `AdversarialMutmutValidator` |
| `MetBench_BLL.Mutation` | Mutation campaign matrix | `MutationCampaignService`, `MutationCellRunner` |
| `MetBench_BLL.Coverage` | 4-dim coverage report | `CoverageService`, `CoverageReport` |
| `MetBench_BLL.Trend` | Weekly trend + WoW + burst detection | `TrendAnalysisService`, `WeeklyReport` |
| `MetBench_BLL.Reporting` | 5-scope report generator | `SystemMtReportService` |

All services are stateless and inject only IDAL repository interfaces +
optional gateway abstractions (`ILlmGateway`, `MutationCellRunner`,
`IProcessExecutor`). Tests inject fakes, prod injects LiteDB + real
process / LLM.

## Roadmap pointers

- 📘 全息项目结构: [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md)（含 4 SUT + 测试矩阵 + 命名约定）
- 🧭 Staged plan: [`AGENTS.md`](AGENTS.md)（含 Stage 7 W11-W12 交付清单）
- 📜 Release Notes: [`RELEASE_NOTES.md`](RELEASE_NOTES.md)（v2.1.0 涉及 PR 一览）
- 🗒 Per-stage implementation plans: [`docs/superpowers/plans/`](docs/superpowers/plans/)
- 🟢 当前活跃 RFC: [`docs/superpowers/plans/2026-05-17-f11-status.md`](docs/superpowers/plans/2026-05-17-f11-status.md)（F11 m_adj 月度监控）
