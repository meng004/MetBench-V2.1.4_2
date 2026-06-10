# PR-4 ObservableProperty 迁移 — VM 验证计划（2026-06-10）

> **状态：Planned（待 VM 执行）。**
> 隶属 [WPF MVVM 收敛计划](2026-06-06-wpf-mvvm-convergence-plan.md) 的 PR-4 收口环节。
> 云端已完成 6 文件的 `[ObservableProperty]` 机械迁移 + 第 7 条 source-guard（经 PR #348
> 合并入 `main` `2977af6`；早期分支的 PR #347 被关闭由 #348 取代），但 CLAUDE.md §9 规定 WPF
> **不在云端编译**——本计划负责 VM 侧的真实编译 + 运行时绑定回归验证。
> **REQUIRED SUB-SKILL**：superpowers:executing-plans，TDD-first，VM track。

## 0. 背景与「为什么需要 VM」

PR-4 把 4 个 ViewModel + 2 个 Model 里**残留的手动 `OnPropertyChanged(...)` 调用**全部
迁移到 CommunityToolkit.Mvvm 源生成路径（`[ObservableProperty]` / `[NotifyPropertyChangedFor]`
/ `SetProperty`）。这 6 处都是 **UI 数据绑定的通知源**——源生成器必须在编译期正确生成
属性 + `PropertyChanged` 触发，否则运行时 UI 不再刷新（静默失效，云端测试看不见）。

云端能跑的是 `WpfMvvmConvergenceGuardTests`（7/7 绿，纯源码文本扫描），但**它不编译
WPF、不验证源生成器输出、不验证运行时绑定**。因此必须 VM 真实编译 + 交互验证每个
被迁移属性的 UI 刷新仍然有效。

## 1. 云端已完成（VM 不重做，仅作前置核验锚点）

| 文件 | 迁移手法 | 被迁移的绑定属性 |
|---|---|---|
| `MetBench_Client/Models/ApplicationEx.cs` | `class`→`partial class`；`_isChecked` 加 `[ObservableProperty]`，删手写 `IsChecked` 属性 + `OnPropertyChanged("IsChecked")` | `IsChecked`（多选 ComboBox 勾选态） |
| `MetBench_Client/Models/DomainEx.cs` | 同上 | `IsChecked` |
| `MetBench_Client/ViewModels/ApplicationManagementViewModel.cs` | `SelectedText` setter 改 `SetProperty(ref _selectedText, value)`（自定义 getter 保留） | `SelectedText`（Domain 多选 ComboBox 文本） |
| `MetBench_Client/ViewModels/MRManagementViewModel.cs` | 同上 | `SelectedText`（Application 多选 ComboBox 文本） |
| `MetBench_Client/ViewModels/MTReportGeneratorViewModel.cs` | `SelectedValue` setter 改 `if (SetProperty(ref _selectedValue, value)) { _ = HandleSelectionChangeAsync(); }`（副作用仅在值变化时触发） | `SelectedValue`（报告类型 ComboBox 选择 → 预览切换） |
| `MetBench_Client/ViewModels/SystemMtResultViewModel.cs` | `_isHistoricalView` 加 `[NotifyPropertyChangedFor(nameof(IsBinaryView))]`，删 `OnIsHistoricalViewChanged` 里手动 `OnPropertyChanged(nameof(IsBinaryView))` | 派生属性 `IsBinaryView`（Binary/Historical 视图切换 ToggleButton） |

source-guard 第 7 条 `No_ViewModel_calls_OnPropertyChanged_manually` 已锁死 `ViewModels/**/*.cs`
中 `OnPropertyChanged(` 调用计数 = 0。

## 2. VM 任务（按顺序）

### Task V0 — 前置核验（任一不满足即停）

- [ ] PR-3 + PR-4 代码均已入 `main`（`git log --oneline origin/main | grep "ObservableProperty (PR-4)"` 命中 #348 `2977af6`；guard 文件含 `No_ViewModel_calls_OnPropertyChanged_manually`）。
- [ ] 从最新 `origin/main` 起验证分支：`git switch -c claude/pr4-vm-verify origin/main`。本环节只新增 VM 证据，不改被迁移源文件。
- [ ] 记录基线：`dotnet build MetBench.sln --no-restore -v:minimal` 退出码 + 警告数
      （代码已在 main，此步同时即 AC-V1 实测）。

### Task V1 — WPF 编译验证（源生成器关键验证点）

- [ ] `dotnet build MetBench.sln --no-restore -v:minimal` → **0 errors**；警告数 ≤ 改前基线
      （`[ObservableProperty]` 不得引入新 CS 警告；若 partial class 缺失会编译失败，这正是
      要 VM 捕获的）。
- [ ] 确认源生成器为两个 Model 生成了 `IsChecked` 属性（编译通过即证明，因 XAML 绑定
      `IsChecked` 在编译期被 XAML 编译校验）。

### Task V2 — 焦点 source-guard 复跑（VM 侧再确认一次）

- [ ] `dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore
      --filter "FullyQualifiedName~WpfMvvmConvergence" --logger "console;verbosity=minimal"`
      → 7/7 绿。

### Task V3 — 运行时绑定交互验证（核心：每个被迁移属性 UI 刷新仍有效）

逐个验证 §1 表里 6 处迁移对应的 UI 刷新路径（截图为证）：

- [ ] **AC-V3a `IsChecked`（ApplicationEx / DomainEx）**：进 MR 管理页 / 应用程序管理页的
      多选 ComboBox，勾选/取消若干项 → 勾选态 UI 即时反映，且下游依赖 `IsChecked` 的
      过滤逻辑（如 `ApplicationExs.Where(b => b.IsChecked)`）行为正常。
- [ ] **AC-V3b `SelectedText`（ApplicationManagement Domain ComboBox）**：编辑/选择多选
      ComboBox → 文本框显示正确，不回退到类名 `MetBench_Client.Models.DomainEx`
      （自定义 getter 的空串兜底仍生效）。
- [ ] **AC-V3c `SelectedText`（MRManagement Application ComboBox）**：同上，针对 ApplicationEx。
- [ ] **AC-V3d `SelectedValue`（MTReportGenerator 报告类型）**：切换 ReportType ComboBox
      （Pdf/Word/Excel/Html）→ 每次切换触发 `HandleSelectionChangeAsync`（预览区切换 /
      "无目标文件" 提示按预期出现），且**重复选同一项不再重复触发**（`SetProperty` 相等短路）。
- [ ] **AC-V3e 派生属性 `IsBinaryView`（SystemMtResult 视图切换）**：在结果页切换
      Binary/Historical ToggleButton → 两个 ToggleButton 的 IsChecked 互斥联动正确
      （改 `IsHistoricalView` 时 `IsBinaryView` 通过 `[NotifyPropertyChangedFor]` 同步刷新）。

### Task V4 — Evidence 固化

- [ ] 输出到 `docs/superpowers/specs/2026-06-10-wpf-mvvm-pr4-vm-evidence/`（截图清单见 prompt §3 验收标准 + 末尾证据目录说明）。
- [ ] 从 VM 分支开 PR 回 `main`（PR-4 代码已经由 #348 入 main；本环节只补 VM 运行时证据）。

## 3. 验收标准（逐条对应 prompt AC）

- AC-V1：WPF build 0 errors，警告数 ≤ 基线。
- AC-V2：`WpfMvvmConvergence` 焦点测试 7/7 绿。
- AC-V3a–e：5 类被迁移属性的 UI 刷新路径各有截图证据，行为与迁移前一致。
- AC-V4：evidence 目录齐全（vm-summary.md + build log + 截图），数据真实。

## 4. 关键设计决策（已在云端锁定，VM 不得擅改）

1. **`SelectedText` 保留自定义 getter**：两处 getter 含「等于类名时返回空串」的兜底逻辑
   （UAT round-1 UC-A5 bug 修复），**不能**改成裸 `[ObservableProperty]`（那会生成纯自动
   属性丢掉 getter 逻辑）。云端用 `SetProperty(ref field, value)` 保留 getter，VM 不得回退。
2. **`SelectedValue` 副作用仅在值变化时触发**：`SetProperty` 返回 bool，仅 true 时调
   `HandleSelectionChangeAsync`——这与旧代码 `if (== value) return;` 早退语义**等价**，
   VM 验证时须确认重复选同项不重复触发（AC-V3d）。
3. **`IsBinaryView` 用 `[NotifyPropertyChangedFor]` 而非手动 raise**：派生属性的标准
   CommunityToolkit 写法，VM 验证互斥联动即可。

## 5. 不交付（明确排除）

- 不改任何 XAML（本 PR 纯 ViewModel/Model 后端属性机制迁移，XAML 绑定路径不变）。
- 不动 PR-1/2/3 已收敛的依赖（Prism/Stylet/Fody 已移除）。
- 不碰 BLL.Core / BLL / DAL / Domain / IDAL。
- 不引入新 MVVM 机制。

## 6. Self-Review

- 6 处迁移点逐一对应一条 AC-V3，无遗漏（CLAUDE.md §4 真实验证）。
- VM 第一条恒为「PR-3 契约已入 main」硬阻塞 + 「记录改前基线」零回归锚点。
- 关键设计决策 §4 已挑明（自定义 getter / 副作用短路 / 派生属性），VM 不得擅改（CLAUDE.md §0.5）。
- prompt 只列骨架 + 指针指向本 plan 取完整上下文（DRY，CLAUDE.md §11.3）。
