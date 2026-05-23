# Working on MetBench (Claude / Agent Notes)

This file is for AI agents and contributors who land in the repo cold. It
captures the **non-obvious conventions** the codebase has settled on so new
work fits in cleanly. For project intent and the staged plan, see
[`AGENTS.md`](AGENTS.md). For build/test, see [`README.md`](README.md).

## 1. 行为约束（Behavioral Constraints — 最高优先级）

> 本节为 AI agent 与贡献者的强制行为约束，**凌驾于本文件其余所有约定**。
> 与下文任何条目冲突时，以本节为准。

### 核心规则

#### 1. 先读再写

修改前必须先阅读相关文件、导出接口、调用方和现有约定。不得重复已有逻辑，不得凭感觉新增相似实现。

#### 2. 最小修改

只修改完成目标所必需的内容。不得顺手重构、扩大范围、引入投机性功能或不必要抽象。

#### 3. 确定性逻辑交给代码

AI 可用于分类、摘要、草拟、解释等语言任务。路由、重试、状态处理、校验、权限判断等确定性逻辑必须由代码实现，不得交给模型判断。

#### 4. 真实验证

验证必须覆盖真实业务意图，而不是只验证表面现象。测试应能在关键逻辑错误时失败，否则该测试无效。

#### 5. 冲突挑明

遇到规则、实现或风格冲突时，必须明确指出冲突，选择其中一条路径，并把另一条标记为待清理项。禁止混合两套互相矛盾的方案。

#### 6. 显式报错

不得静默跳过、吞掉异常、掩盖不确定性或粉饰部分失败。跳过项、失败项、不确定假设和未验证内容必须明确说明。

---

### §0. 不允许只说不做（最高优先级，ANTI-CLAIM-WITHOUT-ACTION）

> 任何回复中出现以下表述时，必须 *在同一回合内* 真实执行对应工具调用并将证据回显给用户。

#### §0.1 触发表述（中英文均触发）

- **中文**：`已记住`、`已保存`、`已添加`、`已修改`、`已写入`、`已更新`、`已删除`、`已重命名`、`已提交`
- **英文**：`I've noted`、`I've saved`、`I'll remember`、`I've added`、`I've modified`、`I've updated`、`I've committed`、`I've removed`

#### §0.2 强制验证规则

| 触发表述            | 必须执行的验证                                               |
| ------------------- | ------------------------------------------------------------ |
| "已修改文件 X"      | 紧接 `Read` 工具调用，显示修改后的相关行段（≥ 3 行上下文）   |
| "已记住偏好 Y"      | 紧接 `Write` 工具调用，写入 `<MEMORY_DIR>/feedback_*.md` 并更新 `MEMORY.md` 索引 |
| "已添加任务 Z"      | 紧接 `TaskCreate` 工具调用 / 更新 `NEXT_STEPS.md`，并在响应中给出 task ID |
| "已提交 commit ABC" | 紧接 `Bash git log --oneline -1` 显示实际 commit hash        |
| "已删除 / 已移除"   | 紧接 `Bash ls` 或 `grep` 验证目标已消失                      |
| "已重命名 X → Y"    | 紧接 `Bash ls` 验证两个名字的存在状态                        |

#### §0.3 禁止行为

1. **只说不做**：声称"已记住"但未写入 memory 文件
2. **假执行**：声称"已修改"但实际未调用 Edit / Write 工具
3. **推迟**：用"稍后会..."、"将会..."替代立即执行
4. **模糊化**：用"已处理"、"已完成"等不可验证的笼统表述

#### §0.4 例外

仅当所述操作 *已在前序回合中以工具调用形式完成*，且本回合只是回顾汇报时，可免重复执行——但必须以 `（前序 commit ABC / Edit at L123 已完成）` 之类的引用替代裸声明。

---

### §0.5. 禁止自发生成与自发改写（ANTI-UNREQUESTED-EDIT，次高优先级）

> 仅次于 §0。AI 的默认倾向是"顺便优化"——修一处 bug 时重写整段、加符号时改周边散文、打包时"整理"未被提到的文件。本节全部禁止。

#### §0.5.1 核心原则

只做被要求做的事，不多做一分。每次 Edit 工具调用的 `old_string` 必须精确定位被要求修改的位置，`new_string` 只改 delta，不引入任何其他变化。

#### §0.5.2 数字前缀术语零容忍

任何形如 `\b\d+[-–]\w+` 的术语（如 "5-MP"、"12-PUT"、"60-cell"、"3-layer"、"two-stage"）：

- **必须逐字来自原文或被引用文献**，不得由 AI 自行发明或"合理推断"。

- AI 生成的数字前缀术语往往听起来合理但与实际不符，一旦进入论文将造成事实错误。

- **发现即还原**：输出中出现原文不存在的此类术语，立即指出并还原为原文措辞。

- 投稿前 grep 自检（应可逐一对照原文核实，除已记录在案的合法术语外）：

  ```bash
  grep -oP '\b\d+[-–]\w+' <PAPER_SOURCE> | sort -u
  ```

#### §0.5.3 禁止自发改写（只改被指定位置）

| 被要求的操作         | 允许范围       | 禁止行为                           |
| -------------------- | -------------- | ---------------------------------- |
| 修复 L123 的符号错误 | 仅改 L123      | 重写 L120–L130 整段"以更清晰"      |
| 修正拼写错误         | 仅改拼写目标   | "顺便"修改周边语序或标点           |
| 修复 LaTeX overfull  | 仅改溢出行     | 重写整个公式块或"优化"换行         |
| 更新一处数字         | 仅改该数字     | 更新其他"看起来也过时"的数字       |
| 打包 zip             | 仅打包指定文件 | 删除 / 移动 / 重命名未被提及的文件 |

#### §0.5.4 禁止自发添加内容

未被要求时**一律不添加**：额外的 caveat / disclaimer / limitation 句、"This is important because…" 类解释、新的 transition sentence（"Moreover," / "In addition,"）、注释 / TODO / 脚注、新的参考文献条目、任何新的章节 / 小节 / bullet point。

#### §0.5.5 禁止自发删除内容

未被明确要求删除时**一律保留**：已有段落 / 句子 / bullet point、已有 `\cite{}` 引用、已有 LaTeX 注释（`% ...`）、已有图 / 表 / 附录。

#### §0.5.6 违规自检

每次完成编辑后对照以下清单：

```
□ 改动范围是否超出被要求的位置？
□ 是否引入了原文中不存在的术语（含数字前缀术语）？
□ 是否删除了任何未被要求删除的原有内容？
□ 是否添加了任何原文中不存在的句子或段落？
□ 数字（效应量 / p 值 / 计数 / 百分比）是否逐字来自数据源？
```

任一项"是"→ 立即还原多余改动，仅重新执行被指定的操作。

---

## 2. 项目状态概览

> 路线图、分阶段交付、待完善项见 [`AGENTS.md`](AGENTS.md)；本节只给冷启动 agent 一个全局快照。

### 2.1 目标

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

### 2.2 核心功能（分层）

> T0 为核心；T1–T6 为围绕核心的功能层。

**T0 · 核心 —— 系统级 MT 流程**

测试输入生成 → 衍生输入转换 → 执行 SUT → 验证源/衍生输出是否满足 MR。实现为
System-MT 引擎 + Launcher facade（`ISystemMtMrLauncher` 单一入口）+ LiteDB 持久化。
**验收标准：流程端到端走通**，不以覆盖全部方程为准（覆盖见 T3）。

**T1 · 直接支撑与操作入口**

- **SUT 运行环境适配** —— 进程调用 / 超时 / 退出码 / 工作目录对接（落实 T0 第 3 步）。
- **输入 / 输出文件适配** —— 非结构化文件 SUT 的解析、参数映射、生成。
- **同源异构程序差分测试** —— 同方程异构实现（数值 / MC / Surr / PINN）在相同 MR 下
  结果比对（OpenMOC × OpenMC 即一对，已检出一例疑似缺陷待确认）。
- **CRUD** —— 应用程序 / 方程 / MR / 基础算例 / 测试过程数据。
- **WPF 客户端** —— 操作入口与页面导航。

**T2 · 可视化与报表**

图表展示 + 4 端（PDF / Word / Excel / HTML）报告生成。

**T3 · 覆盖**

按 ODE / PDE 选代表性方程，每个方程至少 1 个可执行 MT 的 SUT；反应堆物理 5 方程
（boltzmann / diffusion / bateman / fourier / NS）为优先锚定子集。覆盖度量由
Coverage 子系统统计；选型见 [`docs/t3-program-selection.md`](docs/t3-program-selection.md)。

**T4 · MR 识别**

可插拔 `IMRDiscoverer` 框架下三条技术路线：基于元模式的 meta-prompt、multi-LLM
共识、语义因果图（从 SUT 的 `scg.json` 挖 direct-cause / mediator / confounder 模式
得候选 MR）。

**T5 · 异常**

MT 检出的违例进入异常调查工作流（查询 / 过滤 / 状态机 / 共性分析）；确认缺陷封存
入库，支持回放、定位、分类，与「程序版本 × MR × 测试输入」三元组绑定。

**T6 · 变异**

向 SUT 注入变异体、由 MR suite 去「杀」，统计杀死率 / 存活率 / 覆盖率 / 误报率，
据此搜寻最小 MR 完备子集。

## 3. Project topology

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

## 4. WPF stack (do not mix in alternatives)

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

## 5. Page ↔ ViewModel pairing pattern

Each page = triple of files matched 1:1 to a ViewModel:

```
Views/Pages/SomePage.xaml          (root <Page>)
Views/Pages/SomePage.xaml.cs       (code-behind; near-empty)
ViewModels/SomeViewModel.cs        (logic)
```

- **XAML root** — `<Page>` with `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`,
  `ui:Design.Background="{DynamicResource ApplicationBackgroundBrush}"`,
  `Foreground="{DynamicResource TextFillColorPrimaryBrush}"`; bindings as `{Binding ViewModel.X}`.
- **Code-behind** — `partial class SomePage : INavigableView<ViewModels.SomeViewModel>`
  from `Wpf.Ui.Controls`; ctor injects ViewModel, sets `DataContext = this`,
  calls `InitializeComponent()`.
- **ViewModel** — `partial class SomeViewModel : ObservableObject, INavigationAware`
  (`CommunityToolkit.Mvvm.ComponentModel` + `Wpf.Ui.Controls`); use `[ObservableProperty]`
  / `[RelayCommand]`. `OnNavigatedTo` may be `async void`, other handlers use `Task`.
- **DI** in `App.xaml.cs`: `services.AddScoped<Views.Pages.SomePage>()` +
  `services.AddScoped<ViewModels.SomeViewModel>()` per pair.
- **Nav menu** in `MainWindowViewModel.InitializeViewModel()`: add
  `NavigationViewItem { Content="...", Icon=new SymbolIcon{Symbol=SymbolRegular.X24}, TargetPageType=typeof(Views.Pages.SomePage) }`.

## 6. System-MT facade rules (Stage 4 / post-W12 命名统一)

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

## 7. Async & UI-thread conventions

- ViewModels marshal nothing manually onto the UI thread; `[ObservableProperty]` setters dispatch via `INotifyPropertyChanged` and WPF handles cross-thread re-entry for binding targets.
- `async void` is reserved for `INavigationAware.OnNavigatedTo` only (event-handler-style entry point). Everywhere else use `Task` / `async Task`.
- Long-running operations must surface progress through observable properties; do **not** block the dispatcher with `.Result` or `.Wait()`.

## 8. Build & test

| Command | Where it works |
|---------|----------------|
| `dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj` | Linux + Windows |
| `dotnet test MetBench_SystemMT.Tests` | Linux + Windows |
| `dotnet build MetBench.sln` | **Windows only** (WPF SDK targets) |
| `dotnet build MetBench_Client/MetBench_Client.csproj` | **Windows only** |
| `dotnet run --project MetBench_Client` | **Windows only** |

CI (`.github/workflows/dotnet-test.yml`, `ubuntu-24.04`) runs **only** the cross-platform projects. WPF code is not compiled by CI; visual / runtime verification is the developer's responsibility on a Windows host (Parallels VM or otherwise).

OpenMOC + OpenMC tests skip cleanly when the respective venv is missing (`OpenMocTestPaths.OpenMocImportable()` / `OpenMcTestPaths.OpenMcImportable()`); CI does not install either. To run them locally, use `.claude/web-setup.sh` (Linux) or set `METBENCH_OPENMOC_PYTHON` / `METBENCH_OPENMC_PYTHON` (any OS) to a Python where the package is importable. OpenMC additionally requires the `openmc` binary on PATH (or in the same venv `bin/`); the setup script handles this via cmake source build.

## 9. Cross-environment workflow (Linux cloud + Windows VM)

| Track | Lives in | What it does |
|-------|----------|--------------|
| Cloud | this Claude Code Web session | BLL.Core / DAL / SystemMT.Tests / docs / CI workflow. Pushes PRs that CI gates. |
| VM | Windows + VS 2022 (e.g. Parallels) | WPF UI work in `MetBench_Client/` and (rarely) `MetBench_BLL/`. Builds, runs, and visually verifies. Pushes PRs targeting `main`. |

Tracks coordinate via the launcher facade: Cloud owns the contract; VM consumes it. Cloud agents must not modify `*.xaml*` files in `MetBench_Client/` or `MetBench_BLL/` without explicit user direction (they cannot compile them locally to verify the change). VM agents must not modify `MetBench_BLL.Core/SystemMT/*` public types without first proposing a Cloud-side change (CI catches breakage).

## 10. v2 BLL.Core namespaces (P1-P8 ship)

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

## 11. 计划工作流（superpowers plan）

`docs/superpowers/plans/` 下的实施计划，遵循以下**闭环**制订与维护 —— 流程对每个
session 透明、可核验。

### 11.1 闭环（4 步）

1. **读上下文** —— `AGENTS.md`（路线图：当前 Stage、下一步）+ 顺指针读相关既有
   plan + 本文件 §2 与各节约定（功能模型 T0–T6、硬约束）。
2. **写 plan** —— 存 `docs/superpowers/plans/`（`YYYY-MM-DD-<topic>-plan.md`）；
   含目标 & 验收标准、frontmatter「状态」字段；phase / 工时 / 决策点 / 不交付
   **视需要**列。
3. **执行**。
4. **执行后回写** —— 更新 plan 对应 phase / 状态；**就地更新** `AGENTS.md` 对应
   Stage 的交付记录（Stage 粒度，非逐 phase 追加）；若执行改动了路线图（新 Stage /
   范围），一并更路线图层。

### 11.2 验收准则（只列不与步骤重复的）

- [ ] 所列事实（`file:line`、已实现 / 未实现判断）已**对当前分支核实**，非凭记忆。
- [ ] `AGENTS.md` / plan / `CLAUDE.md` 三者无内容复制，只用指针互引。
- [ ] 执行后 plan 与 `AGENTS.md` 状态已同步。

### 11.3 文档职责与边界（唯一事实源，杜绝漂移）

| 文档 | 职责（唯一拥有） | 边界（不放） |
|---|---|---|
| `CLAUDE.md` | 编码 / 协作约定、当前功能模型（§2 T0–T6） | 不放路线图、不放单次实施细节 |
| `AGENTS.md` | 路线图、分阶段交付日志、指向 plan 的指针 | 不放 phase 级实施细节（指针引用） |
| `docs/superpowers/plans/` | 单次工作的实施细节：phase / 工时 / 决策点 | 不放路线图（由 `AGENTS.md` 持有） |
| `README.md` | 构建 / 测试入口 | — |

四者互不复制内容，只用指针相互引用。

## 12. Roadmap pointers

- 📘 全息项目结构: [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md)（含 4 SUT + 测试矩阵 + 命名约定）
- 🧭 Staged plan: [`AGENTS.md`](AGENTS.md)（含 Stage 7 W11-W12 交付清单）
- 📜 Release Notes: [`RELEASE_NOTES.md`](RELEASE_NOTES.md)（v2.1.0 涉及 PR 一览）
- 🗒 Per-stage implementation plans: [`docs/superpowers/plans/`](docs/superpowers/plans/)
- 🟢 当前活跃 RFC: [`docs/superpowers/plans/2026-05-17-f11-status.md`](docs/superpowers/plans/2026-05-17-f11-status.md)（F11 m_adj 月度监控）
