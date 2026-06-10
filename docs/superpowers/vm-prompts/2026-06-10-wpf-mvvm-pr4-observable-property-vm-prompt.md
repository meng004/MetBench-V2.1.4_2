# WPF MVVM PR-4 — ObservableProperty 迁移 VM 验证 Prompt

> **新会话使用方式（用户只发一条指令）**：
> `从最新 origin/main 起验证分支 claude/pr4-vm-verify，读取 docs/superpowers/vm-prompts/2026-06-10-wpf-mvvm-pr4-observable-property-vm-prompt.md，执行任务`

**执行人**：Windows VM 上的 Claude Code 会话（CLAUDE.md §9 VM track；VS 2022 / Parallels，能本地编译运行 WPF）。云端 agent 不编译 WPF（Linux SDK 无 WindowsDesktop targets，`dotnet build MetBench_Client.csproj` 报 MSB4019），只能改源 + 跑 source-scan 测试，**WPF 编译与运行时刷新必须在此 VM 验证**。

**背景**：PR-4 是 WPF MVVM 收敛链（`docs/superpowers/plans/2026-06-06-wpf-mvvm-convergence-plan.md`）的最后一个实现 PR，**代码已经由 PR #348 合并入 `main`（`2977af6`）**：6 个文件的手写 `OnPropertyChanged(...)` 已迁移到 CommunityToolkit.Mvvm 惯用法（`[ObservableProperty]` / `SetProperty` / `[NotifyPropertyChangedFor]`），第 7 条 source-guard（`No_ViewModel_calls_OnPropertyChanged_manually`）云端 7/7 绿。**但 source-guard 只证明源文本无手写调用，不证明运行时 PropertyChanged 仍触发 UI 刷新** —— 本任务即在 main 上提供这层运行时证据。完整代码 delta、迁移手法、冒烟矩阵见 [`docs/superpowers/plans/2026-06-10-wpf-mvvm-pr4-observable-property-vm-plan.md`](../plans/2026-06-10-wpf-mvvm-pr4-observable-property-vm-plan.md)。

---

## 1. 前置条件检查（逐条可核验，任一不满足即停并报告）

1. **【硬阻塞】PR-4 代码已入 main**：`git fetch origin && git log origin/main --oneline | grep "ObservableProperty (PR-4)"` 必须命中（#348 `2977af6`）；`git show origin/main:MetBench_Client/Models/ApplicationEx.cs | grep -c "\[ObservableProperty\]"` ≥ 1。若未命中，说明在错误基线，**先停**。
2. **从 main 起验证分支**：`git switch -c claude/pr4-vm-verify origin/main`；`git branch --show-current` = `claude/pr4-vm-verify`。本任务只新增 VM 证据（截图 / summary / 可选 driver），**不改任何被迁移的源文件**。
3. **第 7 条 guard 在位**：`git show origin/main:MetBench_SystemMT.Tests/SystemMT/Architecture/WpfMvvmConvergenceGuardTests.cs | grep -c "No_ViewModel_calls_OnPropertyChanged_manually"` = 1（PR-3 #346 + PR-4 #348 都已入 main）。
4. **工作区干净**：`git status --porcelain` 为空（避免把本机未提交噪音混进证据）。
5. **基线编译锚点**：先 `dotnet build MetBench.sln --no-restore -v:minimal`，记录 errors/warnings 数作为零回归对照。因代码已在 main，这一步同时就是 AC-V1 的实测——若非 0 errors，直接进 Blockers。

## 2. 核心步骤（骨架；完整 delta 见 plan §2）

> 代码改动**云端已完成并 commit**，VM **不重写代码**，只编译 + 运行时验证 + 必要时修复 VM-only 编译破点。

1. **编译验证**：`dotnet build MetBench.sln --no-restore -v:minimal` → 期望 0 errors，警告数与前置 §1.5 基线相当（不引入新警告）。
   - 若出现 `[ObservableProperty]` 源生成器相关错误（如类未 `partial`、字段命名冲突），先停并对照 plan §1 迁移手法表核对该文件改动是否完整；任何 VM-only 修复回报到 summary，不擅自回退云端迁移。
2. **source-guard 焦点测试**：`dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfMvvmConvergence" --logger "console;verbosity=minimal"` → 期望 **7/7 绿**（含新第 7 条 `No_ViewModel_calls_OnPropertyChanged_manually`）。
3. **运行时刷新冒烟**（plan §1 表的 6 处迁移 → 下列 5 类路径，逐一确认 PropertyChanged 真的触发 UI 刷新）：
   - **AC-V3a `IsChecked`（ApplicationEx / DomainEx，`[ObservableProperty]`）**：MR 管理页 / 应用程序管理页多选 ComboBox 勾选/取消若干项 → 勾选态视觉变化 + 下游 `Where(b => b.IsChecked)` 过滤行为正常。
   - **AC-V3b `SelectedText`（ApplicationManagementViewModel，Domain ComboBox，`SetProperty`）**：应用程序管理页 Domain 多选 ComboBox（ApplicationManagementPage.xaml:240）编辑/选择 → `Text` 刷新，不回退到类名 `MetBench_Client.Models.DomainEx`（自定义 getter 空串兜底仍生效）。
   - **AC-V3c `SelectedText`（MRManagementViewModel，Application ComboBox，`SetProperty`）**：MR 管理页 Application 多选 ComboBox（MRManagementPage.xaml:295）同上，针对 ApplicationEx 类名兜底。
   - **AC-V3d `SelectedValue`（MTReportGeneratorViewModel，`SetProperty` + 仅变化时触发副作用）**：报告生成页 `ReportTypeComboBox`（AutomationId 已有）切换 Pdf/Word/Excel/Html → 每次切换触发 `HandleSelectionChangeAsync`（预览切换 / "无目标文件" 提示按预期）；连选同一项**不重复触发**（`SetProperty` 返回 false 短路）。
   - **AC-V3e 派生属性 `IsBinaryView`（SystemMtResultViewModel，`[NotifyPropertyChangedFor]`）**：System MT 结果页 Binary/Historical 两个 ToggleButton（SystemMtResultPage.xaml.cs:78/87 TwoWay）互斥切换 → 切 `IsHistoricalView` 时 `IsBinaryView` 经 `[NotifyPropertyChangedFor]` 同步刷新，两按钮高亮互斥。
4. **UIA driver（可选但推荐）**：参考 #317 `drive-gapfill-a1-a3.ps1` 模板写最小 driver 跑 AC-V3d（`ReportTypeComboBox` 有 AutomationId，最易自动化），退出码 0。

## 3. 验收标准（VM = 截图证据，逐条对应 plan §3）

- **AC-V1**：`dotnet build MetBench.sln` 0 errors，警告数 ≤ 基线（截图或 `build-output.txt` 末尾汇总行）。
- **AC-V2**：`WpfMvvmConvergence` 焦点测试 **7/7 绿**（含第 7 条 guard）—— 控制台输出截图或文本。
- **AC-V3a（IsChecked）**：`01-checkbox-ischecked.png` —— MR 管理页 / 应用程序管理页多选项勾选/取消，勾选态变化 + 下游过滤可见。
- **AC-V3b（ApplicationManagement.SelectedText）**：`02-appmgmt-domain-selectedtext.png` —— Domain 多选 ComboBox `Text` 随选择刷新，不显类名。
- **AC-V3c（MRManagement.SelectedText）**：`03-mrmgmt-application-selectedtext.png` —— Application 多选 ComboBox `Text` 同上。
- **AC-V3d（SelectedValue）**：`04-report-type-switch.png` —— 切换 ReportType 触发联动；附 `05-report-type-same-no-retrigger.png` 或在 summary 文字说明「连选同项不重复触发」。
- **AC-V3e（IsBinaryView）**：`06-systemmt-binary-historical-toggle.png` —— Binary/Historical 互斥切换，派生 `IsBinaryView` 视觉同步。
- **AC-V4（evidence 齐全）**：证据目录含 `vm-summary.md` + `build-output.txt` + 上述截图，数据真实。
- **AC-V5（可选）**：UIA driver 退出码 0（`drive-pr4.ps1` + 控制台截图）。

> 证据目录：`docs/superpowers/specs/2026-06-10-wpf-mvvm-pr4-vm-evidence/`。

## 4. 结论要求（实事求是，接 CLAUDE.md §0 / §6）

`vm-summary.md` 必须包含**真实可信数据**：

- **环境**：branch、`git log --oneline -1` 的 head、`git config core.autocrlf` 值、OS/VS 版本。
- **命令与输出**：build 退出码 + errors/warnings 数（基线 vs 完工对照）；焦点测试 pass/fail/skip 计数；（可选）UIA 退出码。
- **逐条 AC**：每条 AC-VN 标 **pass / fail / skip**，附触发命令或截图文件名。
- **显式列出**：任何 VM-only 编译修复（文件:行 + 原因）、任何简化 / 跳过项、任何 `[ObservableProperty]` 生成器告警。
- **禁止**：「测试通过」式无数字断言；伪造 / 复用截图；未达成却声称完成。任一 AC 失败 → 写「未完成 + 阻塞原因」并回报云端，**不要**绕过或注释掉 guard。

## 5. Blockers 模板（如出现）

若某条运行时刷新冒烟失败（例如某属性迁移后 UI 不再刷新），写入 `vm-summary.md` 的 `## Blockers` 段，附：属性名、绑定位置、预期 vs 实际行为、相关源行号。**不要**自行回退迁移或改绑定方式绕过 —— 回报云端讨论（可能是 `[ObservableProperty]` 生成属性名与 XAML 绑定名不一致，或 `SetProperty` 相等判断语义差异）。

---

## 6. 云端后续 Follow-up（VM PR 合并后）

VM 证据入 `main` 后，云端据实把 4 处文档收口为 **Controlled**：
1. `docs/superpowers/plans/2026-06-06-wpf-mvvm-convergence-plan.md` —— 状态头改「4 PR 全部完成 + 7 guard 全绿 + VM 运行时证据齐」。
2. `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` —— 该行 Active → Expired（到期条件已满足）。
3. `docs/status/current.md` §6 WPF MVVM 收敛行 —— 补 VM 证据指针，标 Controlled。
4. （若 VM 有编译修复）评估能否转成新 source-guard 或 Roslyn 规则锁住。
