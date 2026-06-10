# PR-4 ObservableProperty 迁移 — VM 运行时验证 Summary

> 任务来源：`docs/superpowers/vm-prompts/2026-06-10-wpf-mvvm-pr4-observable-property-vm-prompt.md`
> 计划：`docs/superpowers/plans/2026-06-10-wpf-mvvm-pr4-observable-property-vm-plan.md`
> 结论：**PR-4 的 6 处属性迁移在真实 WPF 运行时下 PropertyChanged 全部仍生效，UI 刷新无回归。**

## 环境

- branch：`claude/pr4-vm-verify`（从 `origin/main` 起，只新增 VM 证据，不改被迁移源文件）
- head：`52b6071e8764d3244ae0d954513e50146cf570ff`（含 #348 PR-4 代码 + #349 plan/prompt）
- `git config core.autocrlf`：`true`
- OS：Windows 11 Pro，10.0.26200.8457
- .NET SDK：9.0.306（解决方案目标 net8.0 / net8.0-windows7.0）
- 显示缩放：driver 实测 `scale=1`（本会话桌面非 HiDPI）

## 前置条件（prompt §1，逐条核验）

| 条 | 结果 |
|---|---|
| §1.1 PR-4 已入 main（#348 `2977af6`，`ObservableProperty (PR-4)`；`ApplicationEx.cs` 含 `[ObservableProperty]`×1） | ✅ |
| §1.2 从 origin/main 起验证分支 `claude/pr4-vm-verify` | ✅ |
| §1.3 第 7 条 guard `No_ViewModel_calls_OnPropertyChanged_manually` 在位 | ✅ |
| §1.4 工作区无 tracked 改动 | ✅（仅有既存未跟踪 `_worktrees/`(gitignored) 与 `tools/uia-verify-i18n.ps1`，与 PR-4 无关，未提交） |
| §1.5 基线编译锚点 = AC-V1 实测 | ✅（见下） |

## 命令与输出

### AC-V1 — 解决方案编译（`build-output.txt`）
- `dotnet restore MetBench.sln`：成功（仅 pre-existing NU1701 SkiaSharp 警告，与 PR-4 无关）。
- `dotnet build MetBench.sln --no-restore -v:minimal`：**退出码 0，0 errors，10667 warnings**。
- CommunityToolkit.Mvvm 源生成器诊断（`MVVMTK*`）：**0**。6 个被迁移文件内无任何 `[ObservableProperty]`/`[NotifyPropertyChangedFor]` 相关新增 CS 警告；现存警告均为全仓库 pre-existing StyleCop(SA*)/NU1701 基线。
- 警告数即基线（代码已在 main），无新增 → 零回归。

### AC-V2 — source-guard 焦点测试（`focus-test-output.txt`）
- `dotnet test MetBench_SystemMT.Tests --no-restore --filter "FullyQualifiedName~WpfMvvmConvergence" --logger "console;verbosity=minimal"`
- 结果：**通过 7，失败 0，跳过 0，总计 7**（896 ms）。含第 7 条 `No_ViewModel_calls_OnPropertyChanged_manually`（已 `--list-tests` 核名）。

### AC-V3 — 运行时刷新冒烟（`drive-pr4.ps1` UIA driver，`driver-results.txt`）
driver 自动启动 `MetBench_Client.exe`，逐页交互并截图。退出码 0（AC-V5 亦满足）。

为使多选 ComboBox 有可勾选项，验证前用一次性 net8.0 seeder（经真实 `DomainService`/`ApplicationService`/`LiteDbSystemMtResultRepository` 写入，非绕过校验）向 **legacy** `MetBench_DataBase\MR.Litedb`（DbConfig 解析 `|DataDirectory|` 实际位置）补 2 个 Domain（ReactorPhysics/HeatTransfer）+ 2 个 App；SystemMT.Litedb 既有 17 条记录（`advection-amplitude-linearity` 15 条 → CanShowHistoricalView 成立），无需补。seeder 在仓库外（`%TEMP%`），不进提交。

## 逐条 AC 判定

| AC | 迁移点 / 机制 | 触发 → 观察 | 截图 | 结果 |
|---|---|---|---|---|
| **AC-V1** | 编译（源生成器） | build 退出码 0 / 0 err / 无 MVVMTK | `build-output.txt` | **pass** |
| **AC-V2** | 第 7 条 source-guard | 7/7 绿 | `focus-test-output.txt` | **pass** |
| **AC-V3a** | `ApplicationEx`/`DomainEx.IsChecked` `[ObservableProperty]` | App 管理页 Domain 多选 ComboBox 勾选 ReactorPhysics → 复选框 ToggleState=On | `01-checkbox-ischecked.png` | **pass** |
| **AC-V3b** | `ApplicationManagementViewModel.SelectedText` `SetProperty` | 勾选触发 `ItemPropertyChanged`（依赖 IsChecked 的 PropertyChanged）→ `SelectedText` 重建 → 可编辑框文本变 `ReactorPhysics`（非类名 `MetBench_Client.Models.DomainEx`） | `02-appmgmt-domain-selectedtext.png`（亦见 01） | **pass** |
| **AC-V3c** | `MRManagementViewModel.SelectedText` `SetProperty` | MR 管理页 Application 多选 ComboBox 勾选 → 文本变 `_test-csv`（非类名 `ApplicationEx`） | `03-mrmgmt-application-selectedtext.png` | **pass** |
| **AC-V3d** | `MTReportGeneratorViewModel.SelectedValue` `SetProperty`+仅变化触发副作用 | ReportType 切 Word → 弹模态「无目标文件！」（`SetProperty` 返回 true，副作用触发）；再选 Word（同值）→ **不弹**（`SetProperty` 短路）；切 Excel（异值）→ 再弹 | `04-report-type-switch.png` / `05-report-type-same-no-retrigger.png` | **pass** |
| **AC-V3e** | `SystemMtResultViewModel.IsBinaryView`（`[NotifyPropertyChangedFor]`） | 结果页选 advection-amplitude-linearity 记录使 Historical 可用 → 点 Historical：`IsHistoricalView=true` → 派生 `IsBinaryView` 经 NotifyPropertyChangedFor 同步刷新 → Binary 单选钮由 On→Off、Historical→On、图表切历史趋势 | `06-systemmt-binary-before.png` / `06-systemmt-binary-historical-toggle.png` | **pass** |
| **AC-V4** | 证据齐全 | 本目录含 vm-summary.md + build-output.txt + focus-test-output.txt + driver-results.txt + 截图 | — | **pass** |
| **AC-V5（可选）** | UIA driver | `drive-pr4.ps1` 退出码 0，5/5 AC-V3 pass | `driver-results.txt` | **pass** |

## 显式说明（CLAUDE.md §6）

- **无 VM-only 源码修复**：未改任何被迁移的 ViewModel/Model 源文件，未改任何 XAML（PR-4 纯后端属性机制迁移，绑定路径不变）。driver 失败仅出在自动化脚本本身，逐轮修复（导航需滚动到下方虚拟化 NavigationViewItem；多选 ComboBox 项 UIA Name 为类名须按内部 TextBlock 文本定位；legacy DB 实际在 `MetBench_DataBase\MR.Litedb` 而非 bin；native MessageBox 阻塞 UI 线程致 UIA 枚举失效，改用 Win32 `FindWindow`/`PostMessage` 检测与关闭）。
- **种子数据**：仅为让多选下拉有项；经真实 BLL/DAL 写入，未绕过持久化前校验。seeder 工程在 `%TEMP%\pr4seed`，**不提交**。
- **简化/跳过**：无 AC 跳过；6 处迁移点全部以真实交互 + 截图覆盖。
- **生成器告警**：无 `MVVMTK*` 告警。

## Windows Classification

WPF 运行时验证（启动并交互 `MetBench_Client.exe`）只能在 Windows 主机完成；云端（Linux SDK 无 WindowsDesktop targets）不可编译/运行本验证。本 PR 仅新增 VM 证据，不含跨平台 BLL.Core/DAL/SystemMT 改动。

## Blockers

无。AC-V1–V5 全部 pass。
