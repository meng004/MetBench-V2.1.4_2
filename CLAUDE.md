# Working on MetBench (Claude / Agent Notes)

This file is for AI agents and contributors who land in the repo cold. It
captures the **non-obvious conventions** the codebase has settled on so new
work fits in cleanly. For project intent and the staged plan, see
[`AGENTS.md`](AGENTS.md). For build/test, see [`README.md`](README.md).

## 项目状态概览

> 路线图与分阶段计划见 [`AGENTS.md`](AGENTS.md)；本节只给冷启动 agent 一个全局快照。

### 1. 目标

为 MT 研究与工程社区提供一个**完整的系统级蜕变测试（System-level MT）平台与基线**
—— 提供 MT 工作流、程序 / MR / 测试记录的 CRUD，以及 SUT、MR 识别方法、MR 有效性
验证、测试用例生成方法的**快速接入与原型实验**。以元模式驱动的蜕变关系（MR）自动
发现并执行 MT，检出科学计算软件缺陷。

原则上，凡求解**具有显式数学物理方程**的程序皆可作为 SUT。方程从数学上分 ODE / PDE
两类，平台从中选取代表性强、流传广、使用多的方程与程序（选型见
[`docs/t3-program-selection.md`](docs/t3-program-selection.md)）；反应堆物理 5 个核心
控制方程为优先锚定域。

平台面向**科研场景**，聚焦缺陷检出与 MR 库建设；不自研项目管理类功能 —— 未来如有
需要，以对接成熟工具（数据推送 / 交换）的方式实现，不重复造同质功能。

### 2. 核心功能（分层）

> T0 为核心；T1–T6 为围绕核心的功能层（次序经人工指定）。

**T0 · 核心 —— 系统级 MT 流程**

测试输入生成 → 衍生输入转换 → 执行 SUT 被测程序 → 验证源输出与衍生输出是否满足
蜕变关系。实现为 System-MT 引擎 + Launcher facade（`ISystemMtMrLauncher` 单一入口）
+ LiteDB 持久化。**验收标准：流程端到端走通**，不以覆盖全部方程为准（覆盖见 T3）。

**T1 · 直接支撑与操作入口**

- **SUT 运行环境适配**：把核心第 3 步「执行 SUT」落地 —— 进程调用、超时、退出码、
  工作目录等运行环境对接。*必要性：无适配则无 SUT 可执行，核心流程断在第 3 步。*
- **输入 / 输出文件适配**：对输入、输出为非结构化文件的 SUT，提供文件解析、参数
  映射、文件生成。*必要性：科学计算软件多以非结构化文件交互，是 SUT 可接入的前提。*
- **同源异构程序差分测试**：两程序实现同一数学物理方程、但实现技术异构（数值模拟 /
  概率 / 机器学习代理 / PINN），在相同 MR 下比对结果一致性（OpenMOC × OpenMC 即一对，
  已检出一例疑似缺陷，待确认）。*必要性：跨异构实现的一致性偏离是检出科学计算缺陷
  最有力的信号之一。*
- **CRUD**：应用程序 / 数学物理方程 / 蜕变关系 / 基础算例 / 测试过程数据的增删改查。
  *必要性：核心与各层的数据底座，需可维护。*
- **WPF 客户端**：操作入口与页面导航。*必要性：人机交互载体。*

**T2 · 可视化与报表**

图表展示 + 4 端（PDF / Word / Excel / HTML）报告生成。*必要性：科研需要可读的
图表与报告作为论文 / 评审材料。*

**T3 · 覆盖**

覆盖代表性的数学物理方程 —— 按 ODE / PDE 选取流传广、使用多的方程，每个方程至少
对应一个可执行 MT 的 SUT；反应堆物理 5 个核心控制方程（boltzmann / diffusion /
bateman / fourier / NS）为优先锚定子集。覆盖度量由 Coverage 子系统统计；选型见
[`docs/t3-program-selection.md`](docs/t3-program-selection.md)。*必要性：方程维广度
是 MT 基线的核心交付；当前仅 boltzmann 有成熟覆盖。*

**T4 · MR 识别**

把蜕变关系从 0 到 1 识别出来，可插拔 `IMRDiscoverer` 框架下三条技术路线 ——
基于元模式的 meta-prompt 方法、multi-LLM 共识方法、语义因果图方法（从 SUT 的
语义因果图 `scg.json` 挖 direct-cause / mediator / confounder 模式得候选 MR）。
*必要性：核心第 4 步验证的对象就是 MR，没有 MR 核心无的放矢；「从 0 到 1」是
MR 库建设的入口。*

**T5 · 异常**

MT 检出的违例进入异常调查工作流（查询 / 过滤 / 状态机 / 共性分析）；确认的缺陷
封存入库，支持回放、缺陷定位、缺陷分类，并与「程序版本 × MR × 测试输入」三元组
绑定。*必要性：检出只是第一步，封存才能复现、定位、归类，形成可追溯的缺陷资产。*

**T6 · 变异**

向 SUT 注入变异体、由 MR suite 去「杀」，统计杀死率 / 存活率 / 覆盖率 / 误报率，
据此搜寻最小 MR 完备子集。已实现 campaign 矩阵 + 四项统计；语义 / 语法句法变异
分型、等价变异体识别、最小完备子集搜寻规划中（见 §4）。*必要性：评估 MR 集的
查错能力并据此去冗余。*

### 3. 完成情况（已实现的主要功能）

- **v2.1.0 / .1 / .2 已发布**；Stage 1-7 全部交付。
- 已实现：System-MT 的 BDD 执行、输入生成与衍生输入推导、OpenMOC / OpenMC 接入、
  批量执行 + 4 端报表、v2 的 BLL.Core 子系统群（Discovery / Anomaly / Mutation /
  Coverage / Reporting 等）、multi-LLM 共识（60/60 真实跑通、100% accuracy）、
  scenario→MR 命名统一。
- cloud baseline ~560 测试 0 fail；Windows UAT 双轮 PASS。
- polish 批次：HandyControl 移除、UAT runbook 对齐、Anomaly severity / category 分级。

### 4. 尚待完善

- **Stage 8 / v2.2 主线未启动** —— 5 方程 × 4 程序类型 × 5 元模式的 MR 库
  （17 cells、84 候选 MR）。*必要性：这是论文核心交付，当前仅覆盖 boltzmann 方程
  + 少量 MR；地基 5D 索引 schema（Phase 8.0）须先落地，否则后续工作无处挂载。*
- **变异模块增强** —— 语义变异与语法 / 句法变异的分型生成、等价变异体识别、最小
  MR 完备子集搜寻尚未实现。*必要性：Stage 8 将产出 84 候选 MR，需客观证明其检错
  能力并剔除冗余；等价变异体若不识别会人为压低杀死率、污染有效性结论；最小完备
  子集让 MT 以最少 MR 达到同等检错力、降低执行成本。*
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

## 计划工作流（superpowers plan）

`docs/superpowers/plans/` 下的实施计划，遵循以下**闭环**制订与维护 —— 流程对每个
session 透明、可核验。

### 闭环

1. **读 [`AGENTS.md`](AGENTS.md)** —— 路线图锚点：当前 Stage、已交付、下一步、相关 plan 指针。
2. **顺指针读相关既有 plan** —— 避免重复造、保持衔接。
3. **读本文件 §2 + 各节约定** —— 当前功能模型（T0–T6）与硬约束。
4. **写 plan** —— 存 `docs/superpowers/plans/`，命名 `YYYY-MM-DD-<topic>-plan.md`。
5. **执行**。
6. **执行后标记（必做）** —— 每完成一次执行（一个 phase 或一份 plan），**同时标记
   两处**：① **plan 文档** —— 更新对应 phase / plan 状态（phase 勾 ✅、frontmatter
   「状态」字段）；② **`AGENTS.md`** —— 对应 Stage 的交付记录加 / 更新一行（轻量：
   交付内容 + PR / commit 号）。若执行还改动了路线图（新 Stage / 范围），一并回写
   路线图层（不复制 plan 细节）。

### 验收准则（每个 plan 须满足）

- [ ] 含：目标 & 验收标准、phase 分解、工时、决策点、不交付（scope 外）。
- [ ] frontmatter 有「状态」字段：draft / approved / 执行中 / 已交付。
- [ ] 所列事实（`file:line`、已实现 / 未实现判断）已**对当前分支核实**，非凭记忆。
- [ ] 若改动路线图 → `AGENTS.md` 已回写（指针层，无细节复制）。
- [ ] **每次执行后**（必做）→ plan 对应 phase / 状态已标记；`AGENTS.md` 对应 Stage
  交付记录已加 / 更新一行。

### 唯一事实源（杜绝漂移）

| 内容 | 唯一所在 |
|---|---|
| 路线图 / 交付日志 | `AGENTS.md` |
| 实施细节（phase / 工时 / 决策点） | `docs/superpowers/plans/` 下 plan 文档 |
| 当前功能模型 + 编码约定 | 本文件（`CLAUDE.md`） |

三者互不复制内容，只用指针相互引用。

## Roadmap pointers

- 📘 全息项目结构: [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md)（含 4 SUT + 测试矩阵 + 命名约定）
- 🧭 Staged plan: [`AGENTS.md`](AGENTS.md)（含 Stage 7 W11-W12 交付清单）
- 📜 Release Notes: [`RELEASE_NOTES.md`](RELEASE_NOTES.md)（v2.1.0 涉及 PR 一览）
- 🗒 Per-stage implementation plans: [`docs/superpowers/plans/`](docs/superpowers/plans/)
- 🟢 当前活跃 RFC: [`docs/superpowers/plans/2026-05-17-f11-status.md`](docs/superpowers/plans/2026-05-17-f11-status.md)（F11 m_adj 月度监控）
