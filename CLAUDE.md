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
System-MT 引擎 + Launcher facade（`ISystemMtLauncher` 单一入口）+ LiteDB 持久化。
**验收标准：流程端到端走通**，不以覆盖全部方程为准（覆盖见 T3）。
截至代码测试基线 `e839214`（2026-05-25），System-MT 已切到 `ISystemMtLauncher` / `SystemMtLauncher`
+ provider-backed catalog 路径；WPF 默认注册 `ManifestMrCatalogProvider`，但 launcher
launcher 已移除生产路径的 `HardcodedMrCatalogProvider` 过渡 fallback，现要求显式注入 `IMrCatalogProvider`。

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
| `MetBench_BLL.Core/` | `net8.0` | Anywhere (incl. Linux CI) | All cross-platform business logic. **System-MT pipeline, provider-backed launcher facade, persistence contracts, reporting renderer, metadata/evidence path live here.** |
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
ISystemMtLauncher
    Task<IReadOnlyList<MrSummary>> ListAvailableAsync(ct)
    Task<MrRunResult> RunAsync(mrId, parameterOverrides?, ct)
    Task<IReadOnlyList<MrRunResult>> RunBatchAsync(requests, progress?, ct)
```

> 历史命名（已废弃）：`ISystemMtMrLauncher` / `SystemMtMrLauncher` / `ISystemMtScenarioLauncher`
> / `ScenarioDescriptor` / `ScenarioRunResult` / `scenarioId` 等。当前代码线已统一为
> `ISystemMtLauncher` / `SystemMtLauncher`。Persistence 层的 `ScenarioName` 字段同步改为
> `MrName` 并附 LiteDB 自动 schema migration（PR #62）。详见
> [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md)。

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
        ?? (OperatingSystem.IsWindows() ? "python" : "python3"),
    // PR-1 T1: add new runtime families via the generic map — no new LauncherOptions field needed.
    // Manifest authors declare a runtime key (e.g. "fenics") in catalog.json; WPF reads the
    // corresponding env var here and feeds it into RuntimePythons. Unknown non-system keys
    // fail closed at resolution time with `RuntimeEnvironmentResolutionException`.
    RuntimePythons: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Optional examples; add only the keys this WPF build needs.
        // ["fenics"] = Environment.GetEnvironmentVariable("METBENCH_FENICS_PYTHON") ?? "",
        // ["fipy"]   = Environment.GetEnvironmentVariable("METBENCH_FIPY_PYTHON")   ?? "",
    }));

services.AddSingleton<ISystemMtResultRepository>(provider =>
{
    var dataDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
    return new LiteDbSystemMtResultRepository($"Filename={Path.Combine(dataDir, "SystemMT.Litedb")}");
});

services.AddSingleton<IMrCatalogProvider>(provider =>
    new ManifestMrCatalogProvider(
        provider.GetRequiredService<LauncherOptions>()));

services.AddScoped<ISystemMtLauncher, SystemMtLauncher>();
services.AddSingleton<ISystemMtResultReportRenderer, HtmlSystemMtResultReportRenderer>();
```

The system-MT LiteDB file (`SystemMT.Litedb`) is intentionally separate from the legacy MetBench DB (`MR.Litedb`) — `LiteDbSystemMtResultRepository` uses an isolated `BsonMapper` so the two schemas never interact.

Current caveats on `main`:

- `App.xaml.cs` now resolves `ISystemMtCatalogReader` for `LauncherCatalogV2Importer`;
  the concrete cast from `ISystemMtLauncher` to `SystemMtLauncher` has been removed.
- `SystemMtExecutionRecorder` now writes `ExecutionEvidence.SampleTraces` for the current target field
  using source / follow-up input snapshots plus follow-up output metrics
  until sample-level capture is wired in.
- `MetBench_BLL.Core/SystemMT/V12Catalog/` is now a live Stage 8 execution surface rather than a proposal-only folder:
  PR #97–#110 have merged `PR-0` through `PR-10` plus retrospective hardening of the v1.2 roadmap
  (`ba7a9a1` → `e839214`), covering typed schema foundation, fail-closed validators,
  scalar runtime, applicability/status gating, convergence, sequence/subadditive,
  field/derived-invariant runtime, statistical/cross-method runtime, property runtime,
  exponential-growth runtime, typed migration gates, and review-fix hardening for
  invalid golden fixtures / coverage semantics.
- The v1.2 implementation line is complete for the current roadmap on `main`.
  Inventory truth should now be read as **44 MR + 4 Property** from the merged migration assets and gates;
  an older report summary mentioned 43 MR, but repository truth has moved to the explicit migrated inventory.
- **PR-1 T1 manifest-driven runtime environments** (`LauncherOptions.RuntimePythons` + `ResolvePythonExecutable`):
  new SUT runtime families (FEniCS, FiPy, torch-surrogate, ...) belong in `catalog.json`'s
  `python_executable_kind` value, resolved through `LauncherOptions.RuntimePythons` or the
  corresponding environment variable in WPF DI — **not** by adding a new field to
  `LauncherOptions` for every dependency family, and **not** by extending
  `PythonExecutableKinds.All`. Unknown non-system keys fail closed at resolution time
  with `RuntimeEnvironmentResolutionException` naming the missing key. Built-in
  `system` / `openmoc` / `openmc` / `scipy` behaviour is preserved via compat fields;
  the resolver prefers `RuntimePythons[key]` (case-insensitive, non-blank) over the
  compat field when both are set.

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
| `MetBench_BLL.SystemMT.*` | Pipeline + Launcher + Persistence + Reporting | `SystemMtPipeline`, `ISystemMtLauncher`, `HtmlSystemMtResultReportRenderer` |
| `MetBench_BLL.SystemMT.Anomaly` | Anomaly viewer + commonality | `AnomalyService`, `CommonalityReport` |
| `MetBench_BLL.Discovery` | MR Discovery + Validation | `IMRDiscoverer`, `DiscoveryService`, `ValidationService`, `ILlmGateway` |
| `MetBench_BLL.Discovery.Validators` | Day-1 validators | `EmpiricalValidator`, `TheoreticalLlmValidator` |
| `MetBench_BLL.Mutation` | Mutation campaign matrix | `MutationCampaignService`, `MutationCellRunner` |
| `MetBench_BLL.Coverage` | 4-dim coverage report | `CoverageService`, `CoverageReport` |
| `MetBench_BLL.Reporting` | 4-scope report generator | `SystemMtReportService` |

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
   如遇计划过多、阶段切换或状态冲突，先读
   `docs/status/current.md`、
   `docs/superpowers/specs/2026-05-25-metbench-project-control-rules.md`
   和
   `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`，
   只按活跃计划执行，不把历史计划当当前真相层。
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

### 11.3 文档职责与边界（状态账本 + 投影文档，杜绝漂移）

| 文档 | 职责 | 边界（不放） |
|---|---|---|
| `docs/status/current.md` | 当前状态账本：主线状态解释、代码测试基线、活跃风险、执行顺序 | 不复制实时 `origin/main` 头提交；由 git 实时解析 |
| `CLAUDE.md` | 编码 / 协作约定、当前功能模型投影（§2 T0–T6） | 不放路线图、不放单次实施细节 |
| `AGENTS.md` | 路线图、分阶段交付日志、指向 plan 的投影 | 不放 phase 级实施细节（指针引用） |
| `docs/superpowers/plans/` | 单次工作的实施细节、活跃计划索引、phase / 工时 / 决策点 | 不重新定义当前状态账本 |
| `README.md` | 构建 / 测试入口 | — |

这些文档互不复制状态结论，只用指针相互引用；若有冲突，先读 `docs/status/current.md`，再读 project-control rules 与 active plan index。

## 12. PR 提交与门禁（Hard Test + Inline Governance Grep）

> 本节约束所有目标分支为 `main` 的 PR 流程；自 2026-05-26 起生效，2026-05-27 起把
> 早期的 dual AI review (`openai/codex-action@v1` + `anthropics/claude-code-action@v1`)
> 整体替换为**内联 grep 治理检查**（理由见 §12.1 末尾）。规则细节见
> [`docs/superpowers/templates/pr-gate-checklist.md`](docs/superpowers/templates/pr-gate-checklist.md) +
> [`.github/workflows/dotnet-test.yml`](.github/workflows/dotnet-test.yml) 的 `governance` job。
>
> 历史 spec [`docs/superpowers/specs/2026-05-26-pr-soft-review-via-claude-code-action.md`](docs/superpowers/specs/2026-05-26-pr-soft-review-via-claude-code-action.md)
> 已 retired（保留作历史记录），对应 workflow `pr-soft-review.yml` 已删除。

### 12.1 两层并行 + 三/四层防御

| Gate | 目标 | 内容 | Branch protection |
|---|---|---|---|
| **Hard `test`** | 保代码正确性 + 跨 PR contract guard | `dotnet build` + xUnit + Reqnroll + §12.4 R1/R4 mechanized facts + §12.5 第四层 guard | ✅ Required；阻塞合并 |
| **Inline `governance` grep** | 保项目不失控 / 不失真 | `dotnet-test.yml` 内 `governance` job 跑 5 条 grep：plan traceability / status truth / Windows classification / docs-only baseline misclaim / PR Gate Checklist 7 节存在 | ❌ **永不入** required；advisory only，输出 `::warning::` |

两层**并行起跑**，互不替代。`test` 负责可合并性；`governance` 负责治理门禁（grep / 文件触发匹配）。**AI review 已撤除**——dual AI review 在 2026-05-27 之前两个月的实战中：(a) 经常因 OAuth quota / OpenAI quota / anti-injection 401 fail-fast；(b) 实际 catch 数远低于 §12.4 / §12.5 的 mechanical guards；(c) 每 PR 烧 ~5 min runner + LLM token。机械 grep 跑 < 10 秒、确定性、零 token 成本，配合 §12.4 R1/R4 + §12.5 已经覆盖了 dual AI review 试图守护的所有范围。

#### §12.4 第三层 / §12.5 第四层

- **§12.4 第三层**：跨 PR 一致性 + parity-test 纪律。**人** + **流程**约束（R1-R4），由 PR 作者 + post-merge holistic review 兜底
- **§12.5 第四层**：把 §12.4 R1/R3/R4 编进 hard `test` 的具体 fact 守护（`*ParityTests.cs` / `Audit_*_providers_produce_identical_matrices` / `Render_*_renders_<contract>`）

第四层成熟度决定第三层人工干预的频率；以后 finding 优先转 fourth-layer guard test。

### 12.2 强约束（违反 = process bug）

- 所有 PR description 必须填 [`pr-gate-checklist.md`](docs/superpowers/templates/pr-gate-checklist.md) 7 节（Scope / Facts / Tests / Windows / Review / Merge / Soft Review），缺节会被 `governance` job 的 grep check 5 抓 `::warning::`
- 改 `.github/workflows/dotnet-test.yml` 的 `governance` job 本身的 PR 是**自审的**——grep 跑在 PR head ref，不像旧 `anthropics/claude-code-action@v1` 有 anti-injection 401 拒绝。但 grep 规则改坏后果直接在本 PR 显现，建议在 PR 描述里 paste 一段 test 输出证明新规则不 false-positive
- Hard-gate 必须保持在 main branch protection required check 列表（当前 check 名 `test`）；`governance` job 永不入此列表
- 撤除的 secret：`OPENAI_API_KEY`、`CLAUDE_CODE_OAUTH_TOKEN`——撤除后仍可保留在 repo Settings 不影响（无 workflow 引用）；若需重新启用某种 AI review 服务，参考 §12.1 的历史 spec

### 12.3 PR 合并前应观察的事

1. `test` 绿（必须）
2. `governance` 已跑（在 `test` workflow 同一 PR 检查里，作为单独 job）；若产生 `::warning::` 行，逐条核对并在 PR 描述或评论里说明
3. PR body 7 节 checklist 都打勾 / 解释
4. 若你是 agent，merge 自己的 PR 前应 fetch origin/main 并核对 base.sha 是否需要 update branch

### 12.4 第三层防御：跨 PR 一致性 & parity-test 纪律

> **背景**：2026-05-27 T2/T3 6-phase chain 的 post-merge review 发现 11 项 finding；其中 5 项属"单 PR diff 可见"（AI review 大概率能抓），但**另外 6 项是跨 PR / 跨文件 / retrospective 性质**，soft / Codex / Claude review 在 PR-time 都看不见。本节把"跨 PR 一致性"作为**第三层防御**约束起来，不能仅靠 AI review 兜底。

#### R1 · Cross-projection parity test 强制

> 凡是一个 public type 有**两条以上投影路径**（典型例：`MrCatalogEntry.FromBlueprint` vs `ManifestMrCatalogProvider.MapToEntry`、entity 的 to-DTO/from-DTO 双向、HTML/Markdown/PDF/Word/Excel 多渲染器投影同一 record），**每加一个字段必须同步加 parity test**。
>
> - 守护文件命名约定：`<TypeName>ParityTests.cs`（如 `CatalogParityTests.cs`）
> - 投影路径的任意一侧改了 record/字段，PR 必须同时改对方 + 守护测试。AI review prompt 应被指示检查这一点
> - **历史教训**：PR-T3-7 / Phase 4 加 `MrCatalogEntry.MetaPattern`，只改了 `ManifestMrCatalogProvider`；`FromBlueprint` 没改 → 通过 hard + soft + Claude semantic review 全部 gate；post-merge review 才捕获（PR #199 修，加 parity assertion + `Audit_hardcoded_and_manifest_providers_produce_identical_matrices` 守护）。**约束**：以后任何加字段 PR，CI 失败提示要明确指向 missing parity assertion，不是 silent diff

#### R2 · 多 PR 链路必须 chain-end holistic review

> 任何**连续 ≥ 3 个 PR** 的 phased delivery（典型例：T2/T3 6-phase chain；W12 4-PR sequence；S8 P1-P5 chain），**最后一个 PR 合并后必须立刻开一个独立 review session** 跑 post-merge holistic review，发现 finding 写成一个 cleanup PR 才算 chain closure。
>
> - Trigger 条件：plan 文档里枚举 ≥ 3 个 sequential PRs，或 plan 用了 "Phase N" / "PR-X-N" 命名
> - Review session 必须是**独立 fresh agent**（新 context），不能由 chain 实施 session 自己审
> - 找到的 finding 进入 cleanup PR（一个 PR 多个 fix bundle 可接受，分两个 PR 更清晰），cleanup PR merge 后才能在 `docs/status/current.md` Stage-8 表里标 chain "Controlled"
> - **历史教训**：T2/T3 chain 在 Phase 6 (PR-LEDGER) 合并后立即声明 Controlled；之后 review 找 11 项 finding，被迫开 PR #195 + #199 两个 cleanup PR。规则化后：chain-end review 应是 plan 的 phase N+1，提前进 plan，不是事后补

#### R3 · Spec 文档对实施偏离的 retrospective 责任

> Phase-K (K < N) 的 spec 文档若推荐了候选 X，而 Phase-N 实施时换成候选 Y（empirical validation 失败 / SUT precondition 不满足 / 等），**Phase-N 的同一 PR 或紧随的 doc PR 必须 re-touch 该 spec**，把推荐措辞改为"原推荐已被 Y 替代，原因 …"。
>
> - 不允许留 stale "top-1 候选 = X" 在 main 上让下一个读者误解
> - 不允许仅在 commit body / status ledger 提一句"已替代"——spec doc 本身要改
> - Phase-N PR body 必须明示「本 PR 修改了 Phase-K spec 的 §N」，AI review 可据此核对
> - **历史教训**：Phase 5 (PR #192) ship `subchannel-friction-invariance` 而非 spec doc top-1 `burgers-timestep-convergence`；commit body 解释了但 spec doc §4 仍标 burgers 为 top-1；PR #199 才把 spec §4 改成"REJECTED with retrospective"。规则化后：偏离实施的同 PR 即修 spec

#### R4 · Public-contract ↔ fact 配对

> 凡是 public method 的 XML doc 声称「honors X」/「implements Y」/「supports Z」/「per ReportContext.Title」，必须在同 PR 加 fact 断言 X / Y / Z 在输出里**可观测**。**未断言的契约不算实现**。
>
> - PR Gate Checklist 「Tests」节明确把这一条作为 sub-check
> - AI review prompt 应被指示：扫描 public method XML doc 提取契约关键词，grep 对应测试文件确认 fact 存在
> - **历史教训**：`IExcelSystemMtResultReportRenderer` XML doc 提到 `ReportContext`，但 `ExcelSystemMtResultReportRenderer.Render` 内部 `_ = context ?? new ReportContext()` 立刻丢掉；测试没断言 Title 出现 → 全部 gate 通过；post-merge review 才发现（PR #195 修）。规则化后：每个声明契约的 public method 必带一个反映契约的 fact

### 12.5 第四层防御：固化跨 PR / contract guard 进 hard gate

> 第三层是流程约束；第四层是把流程约束**编译进** hard `test` gate，从根本上让违反它的 PR 直接红。这一层是 R1-R4 的**自动化具象化**：

| 名称 | 守的 finding 类型 | 对应规则 | 现状 |
|---|---|---|---|
| `*ParityTests.cs` | cross-projection 字段不对称（L1 类） | R1 | `CatalogParityTests` 已加 `MetaPattern` 断言；后续 record 加字段必加同款 |
| `Audit_*_providers_produce_identical_matrices` | 多 provider 实现产出不同结果（L1/M5 类） | R1 | `MetaPatternMatrixAuditorTests` 已加；后续多 provider 服务都加 |
| `Render_*_renders_<contract>` 测试 | public method XML doc 契约未实现（B1 类） | R4 | `Render_summary_sheet_renders_title` 等已加；后续每个 evidence-aware overload 都加 |
| 架构守护 `SemanticCatalogBoundaryTests` 系列 | 边界跨违反（pre-existing） | R1 | 已生效；不放松 |
| chain-end review checklist | 多 PR 链路漂移（D1/D2/T1/T2 类） | R2/R3 | 待引入：`docs/superpowers/templates/chain-end-review-checklist.md` |

**约束**：post-merge holistic review 每发现一个 finding，**第一优先级动作**是问"能否把这类 finding 转成第四层 guard test"。能就加守护；不能（或代价过高）则进 chain-end review checklist。**不允许只修该实例，不加守护**。

## 13. Roadmap pointers

- 📘 全息项目结构: [`docs/PROJECT-STRUCTURE.md`](docs/PROJECT-STRUCTURE.md)（含 4 SUT + 测试矩阵 + 命名约定）
- 🧭 Staged plan: [`AGENTS.md`](AGENTS.md)（含 Stage 7 W11-W12 交付清单）
- 📜 Release Notes: [`RELEASE_NOTES.md`](RELEASE_NOTES.md)（v2.1.0 涉及 PR 一览）
- 🗒 Per-stage implementation plans: [`docs/superpowers/plans/`](docs/superpowers/plans/)
- 🟢 当前活跃计划索引: [`docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`](docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md)
- 🗂 历史 RFC 参考: [`docs/superpowers/plans/2026-05-17-f11-status.md`](docs/superpowers/plans/2026-05-17-f11-status.md)（F11 m_adj 月度监控）
