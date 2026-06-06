# P4 WPF Deadlock Surface — VM Cleanup Prompt

**执行人**：Windows VM 上的 Claude Code 会话（CLAUDE.md §9 VM track；VS 2022 / Parallels，能本地编译运行 WPF）。云端 agent 不编译 WPF，只能改源 + 提供精确清单。

**怎么起**：在 Windows 仓库里
1. `git fetch origin && git switch main && git pull`（基于最新 `main`，含 P3 棘轮 #325 #326 #327）。
2. 从 `main` 起新分支：`git switch -c claude/p4-wpf-deadlock`。
3. 读取本文件并按清单执行。
4. 完成后从该 VM 分支开 PR 回 `main`。

## 背景

成熟度评估发现 WPF ViewModel 层有 **18 处 `.ShowDialogAsync().Result`**（同步阻塞 await，WPF dispatcher 死锁面）+ **1 处 `async void HandleSelectionChange`**（崩溃无捕获）。云端不能编译验证 WPF，必须 VM 跑。修复计划 Phase 4 即此任务。

## 精确缺口清单（18 + 1）

### 18 处 `.ShowDialogAsync().Result`

模式 100% 一致，均为 `var messageResult = uiMessageBox.ShowDialogAsync().Result.ToString();`，均位于含 `showMessage(string, string) -> bool` 之类的同步辅助方法内。

| # | 文件 | 行 |
|---|---|---|
| 1 | `MetBench_Client/ViewModels/MRRecommendationViewModel.cs` | 165 |
| 2 | `MetBench_Client/ViewModels/MRRecommendationViewModel.cs` | 193 |
| 3 | `MetBench_Client/ViewModels/MTReportGeneratorViewModel.cs` | 168 |
| 4 | `MetBench_Client/ViewModels/MTReportGeneratorViewModel.cs` | 242 |
| 5 | `MetBench_Client/ViewModels/DomainManagementViewModel.cs` | 134 |
| 6 | `MetBench_Client/ViewModels/DomainManagementViewModel.cs` | 205 |
| 7 | `MetBench_Client/ViewModels/DomainManagementViewModel.cs` | 315 |
| 8 | `MetBench_Client/ViewModels/AutoDetectMRViewModel.cs` | 168 |
| 9 | `MetBench_Client/ViewModels/AutoDetectMRViewModel.cs` | 188 |
| 10 | `MetBench_Client/ViewModels/AutoDetectMRViewModel.cs` | 305 |
| 11 | `MetBench_Client/ViewModels/AutoDetectMRViewModel.cs` | 379 |
| 12 | `MetBench_Client/ViewModels/ApplicationManagementViewModel.cs` | 346 |
| 13 | `MetBench_Client/ViewModels/ApplicationManagementViewModel.cs` | 493 |
| 14 | `MetBench_Client/ViewModels/ApplicationManagementViewModel.cs` | 555 |
| 15 | `MetBench_Client/ViewModels/MRManagementViewModel.cs` | 407 |
| 16 | `MetBench_Client/ViewModels/MRManagementViewModel.cs` | 501 |
| 17 | `MetBench_Client/ViewModels/MRManagementViewModel.cs` | 594 |
| 18 | `MetBench_Client/ViewModels/MTExecutionViewModel.cs` | 297 |

### 1 处 `async void`

| # | 文件 | 行 | 方法 |
|---|---|---|---|
| 19 | `MetBench_Client/ViewModels/MTReportGeneratorViewModel.cs` | 48 | `private async void HandleSelectionChange()` |

## 修复模式（每处复用）

### 18 处 `.Result` —— 改异步 + 调用方传递

**对每个 `showMessage` 类辅助方法**：

旧：
```csharp
public bool showMessage(string message, string title)
{
    // ... 构造 uiMessageBox ...
    var messageResult = uiMessageBox.ShowDialogAsync().Result.ToString();
    // ... 处理 messageResult ...
    return ...;
}
```

新：
```csharp
public async Task<bool> showMessageAsync(string message, string title)
{
    // ... 构造 uiMessageBox ...
    var messageResult = (await uiMessageBox.ShowDialogAsync()).ToString();
    // ... 处理 messageResult ...
    return ...;
}
```

**调用方传递**：每个调用 `showMessage(...)` 的位置改为 `await showMessageAsync(...)`，包含它的方法/命令签名变为 `async Task`（对于 RelayCommand 同步命令变为 `[RelayCommand] async Task ...`，CommunityToolkit.Mvvm 自动支持）。

### #19 `async void HandleSelectionChange` —— 改 `async Task` + 调用方 await

`MTReportGeneratorViewModel.cs:48`：

旧：
```csharp
private async void HandleSelectionChange() { ... await ... }
```

新：
```csharp
private async Task HandleSelectionChangeAsync() { ... await ... }
```

调用方（属性 setter / OnPropertyChanged 路径）：
- 如果调用方本就是 `async void` 事件处理 → 加 `await HandleSelectionChangeAsync();`
- 如果调用方是 setter 等同步路径 → 用 `_ = HandleSelectionChangeAsync();` fire-and-forget + 在 method 内 `try/catch` 兜底（不能让 async void 风险移到 setter）。

## 验证步骤

1. `dotnet build MetBench.sln --no-restore -v:minimal` —— 期望 0 errors，警告数与改前相当（不引入新警告）。
2. **Source 守卫**：`grep -rn "\.ShowDialogAsync().Result\|\.ShowDialogAsync().GetAwaiter().GetResult()" MetBench_Client/ViewModels --include=*.cs` 必须**为空**。
3. **`async void` 守卫**：`grep -rn "async void [A-Z]" MetBench_Client/ViewModels --include=*.cs | grep -v "OnNavigatedTo"` 必须**为空**。
4. WPF 启动 → 至少跑两个对话框触发路径（如 MR 管理页 / 应用程序管理页的 Save 流程），确认对话框正常弹出且应用不卡顿/不死锁。截图：对话框出现 + 点确认后回到正常 UI 状态。
5. 焦点测试：`dotnet test MetBench_SystemMT.Tests\MetBench_SystemMT.Tests.csproj --no-restore --filter "FullyQualifiedName~WpfAsync" --logger "console;verbosity=minimal"` —— 期望全绿。
6. UIA driver（可选，参考 #317 的 `drive-gapfill-a1-a3.ps1` 模板）跑一次完整对话框路径冒烟。

## Evidence 输出

把以下放到 `docs/superpowers/specs/2026-06-06-p4-wpf-deadlock-vm-evidence/`：

- `vm-summary.md`（branch、head、commands、grep 输出、build 退出码、测试计数、UIA 退出码）
- `build-output.txt`
- `01-mr-management-save-dialog.png`
- `02-application-management-save-dialog.png`
- `03-mt-report-generator-selection-change.png`（HandleSelectionChange 触发的路径）

## Acceptance

- ✅ 全部 18 处 `.ShowDialogAsync().Result` 消失（grep 为空）
- ✅ 全部 `async void`（除 OnNavigatedTo）消失（grep 为空）
- ✅ WPF build 0 errors
- ✅ 至少 3 个对话框路径在运行 WPF 里验证（截图）
- ✅ 焦点测试 + UIA 退出 0
- ❌ **不准** 用 `.GetAwaiter().GetResult()` 或 `Task.Run(...).Result` 假装修了（同等死锁面，等价违规）

## Remaining Blockers 模板（如出现）

如有阻塞（例如某个 setter 路径无法接受 fire-and-forget），写入 `vm-summary.md` 的 `## Blockers` 段，**不要**绕过——回报给云端讨论。

---

## 云端后续 Follow-up（VM PR 合并后）

VM 改动入 `main` 后，云端**立即**加一个 source-guard 测试（位置：`MetBench_SystemMT.Tests/SystemMT/Architecture/WpfAsyncCorrectnessGuardTests.cs`），断言：

1. `MetBench_Client/ViewModels/*.cs` 中 `.ShowDialogAsync().Result` 计数 = 0
2. `MetBench_Client/ViewModels/*.cs` 中非 `OnNavigatedTo` 的 `async void` 计数 = 0

这样将来任何新增同款回潮都会让 CI `test` 失败，等于把 P4 的成果棘轮锁住。
