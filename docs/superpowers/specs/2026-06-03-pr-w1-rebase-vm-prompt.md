# VM Agent Prompt — pr-w1 SystemMtResultPage rebase onto current main

> **新会话使用方式**：在 **Windows VM** 上发指令 `读取 docs/superpowers/specs/2026-06-03-pr-w1-rebase-vm-prompt.md，执行任务`。
> 本文件即完整执行指令：依次完成「前置条件检查 → 核心步骤 → 验收标准 → 结论要求」四段。
>
> 角色：VM 轨（Windows + VS 2022 + WPF，**不经 CI**）。Cloud 已确认 rebase 可行但无法编译验证(§9)，故交 VM。
> 背景：`claude/pr-w1-systemmt-result-page` 是 i18n 迁移**之前**的 WPF 结果页功能(SystemMtResultPage + SystemMt 专用 plotter)，经 Cloud diff 确认是 main 真实缺口(非重复)。需 rebase 到当前 main(已 i18n 化)。

## 前置条件检查（任一不满足即停并报告）

1. **环境是 Windows**：`dotnet build MetBench_Client/MetBench_Client.csproj` 能跑(Linux 必 MSB4019)。先空跑当前 main 确认基线可编译。
2. `git fetch origin` 后，分支 `claude/pr-w1-systemmt-result-page` 存在于远端。
3. **BLL.Core 依赖已在 main(Cloud 已核验)**：`MetBench_BLL.Core/SystemMT/Reporting/Charts/` 下有 `HistoricalTrendProjector.cs` / `BinaryRunPointProjector.cs` / `PhaseConvergenceProjector.cs`(含 `ChartFigure`)。命令：`ls MetBench_BLL.Core/SystemMT/Reporting/Charts/`。缺失则停(依赖前提不成立)。
4. 确认 main 的 `MainWindowViewModel.cs` 已是 i18n 版(用 `LocalizedNav(key,...)` + `IAppLocalizationService`)、`MetBench_UI.Localization` 的 .resx 有 `Nav_*` 键族。

## 核心步骤

> Cloud 已实际跑过一次 rebase 并理清全部冲突，解法如下，照做即可。

1. **建分支 + 起 rebase**：
   ```
   git checkout -b claude/pr-w1-rebase origin/claude/pr-w1-systemmt-result-page
   git rebase origin/main
   ```
   预期在第 2 个 commit(`56a0f43 PR-W1 part 2/2`)冲突于 `App.xaml.cs` + `MainWindowViewModel.cs`。新增文件(Services/Plotting/SystemMt/*、SystemMtResultViewModel、SystemMtResultPage.xaml(.cs)、InverseBoolConverter)**不冲突**。

2. **解 `MetBench_Client/App.xaml.cs`(3 处冲突,全是 keep-both)**：
   - using 段：保留 HEAD 的 `using MetBench_BLL.SystemMT.Persistence.Editing;`。
   - `ISystemMtSutEditor` 单例注册：保留 HEAD。
   - 第 3 处：**两侧都留** —— HEAD 的编辑器页注册(SUT/Equation/SampleCase/ExecutionHistory)之后，紧接 PR-W1 的结果查看器注册块：
     ```csharp
     // === SystemMT result viewer (PR-W1) ===
     services.AddSingleton<Services.Plotting.SystemMt.BinaryRunPlotter>();
     services.AddSingleton<Services.Plotting.SystemMt.PhaseConvergencePlotter>();
     services.AddSingleton<Services.Plotting.SystemMt.HistoricalTrendPlotter>();
     services.AddSingleton<Services.Plotting.SystemMt.SystemMtChartPlotterFactory>();
     services.AddSingleton<MetBench_BLL.SystemMT.Reporting.Charts.HistoricalTrendProjector>();
     services.AddScoped<Views.Pages.SystemMtResultPage>();
     services.AddScoped<ViewModels.SystemMtResultViewModel>();
     ```

3. **解 `MetBench_Client/ViewModels/MainWindowViewModel.cs`(整文件冲突)**：
   - main(HEAD)已为 i18n **完全重写**；pr-w1 是旧硬编码版。**整体取 HEAD(i18n 版)**，丢弃 pr-w1 那一侧。
   - 然后在 HEAD 的 `NavigationItems` 列表里(建议放在 `Nav_SystemMtExecutionHistory` 项之后、`Nav_Anomalies` 之前)**新增一行**结果页导航：
     ```csharp
     LocalizedNav("Nav_SystemMtResult",  SymbolRegular.DataBarVertical24, typeof(Views.Pages.SystemMtResultPage),  _localizedNavigation),
     ```
   - 这是 pr-w1 原本那个硬编码 `Content="SystemMT 结果"` 导航项的 i18n 等价物。

4. **加本地化键**：在 `MetBench_UI.Localization` 的 zh / en `.resx` 各加一条 `Nav_SystemMtResult`（zh: `SystemMT 结果`；en: `SystemMT Result`），与既有 `Nav_*` 键同表。

5. **i18n 化结果页(若需要)**：检查 pr-w1 的 `SystemMtResultPage.xaml` / `SystemMtResultViewModel.cs` 是否有硬编码中文/英文串；若有，按 main 现行约定(`Localization` provider 绑定)本地化。硬编码串**能编译**但不符 i18n 规范——至少保证编译，i18n 润色可作 follow-up。

6. **完成 rebase + 构建**：
   ```
   git add -A && git rebase --continue
   dotnet build MetBench_Client/MetBench_Client.csproj
   ```
   修复任何编译错误(projector API 签名若与 pr-w1 假设不符，按当前 main 的 `HistoricalTrendProjector` / `BinaryRunPointProjector` / `PhaseConvergenceProjector` 真实签名调整调用点)。

7. **运行 + 视觉验证**：`dotnet run --project MetBench_Client` → 导航到「SystemMT 结果」页 → 确认图表(binary-run / phase-convergence / historical-trend)正常渲染。

## 验收标准

- **AC-1 编译**：`dotnet build MetBench_Client/MetBench_Client.csproj` **0 error**（贴构建输出尾部截图）。
- **AC-2 契约零改动**：`git diff origin/main -- MetBench_BLL.Core MetBench_DAL` 为空(本 rebase 只动 `MetBench_Client/` + .resx)。
- **AC-3 导航出现**：主窗口导航出现「SystemMT 结果」项(截图)。
- **AC-4 结果页渲染**：进入该页，三类图表至少有一类用真实/样例数据渲染出来(截图)。
- **AC-5 既有页不回归**：随机进 3 个既有页(MT Execution / Anomalies / Coverage)正常(截图或录屏)。

## 结论要求（实事求是，真实数据支撑）

1. 贴 `dotnet build MetBench_Client` 的**真实 error/warning 计数**，不写「构建通过」之类无数字断言。
2. 逐条 AC 标 pass/fail，附**本次运行真截图文件名**(归档 `docs/superpowers/specs/2026-06-03-pr-w1-rebase-vm-verification/`)。
3. 若 projector API 签名与 pr-w1 假设不符而做了调整，**明确列出改了哪些调用点**。
4. 若结果页仅编译通过但 i18n 未完成 / 某类图表无数据，**显式说明**，不得用「已完成」掩盖(接 §0 / §6)。
5. AC-1 未过则结论写「rebase 未完成 + 阻塞原因」，禁止声称页面已验证。

## 完成后

- rebase 成功并 VM 验证通过 → 推 `claude/pr-w1-rebase` → 开 PR targeting main(标 Windows-only，CI 不编译 WPF)。
- 旧分支 `claude/pr-w1-systemmt-result-page` 在新 PR 合入后再删。
