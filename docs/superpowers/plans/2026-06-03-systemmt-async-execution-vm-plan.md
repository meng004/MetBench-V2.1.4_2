---
状态: Proposed (blocked on cloud plan)
环境: VM (Windows + VS 2022, WPF, 不经 CI)
依赖设计: docs/superpowers/specs/2026-06-03-systemmt-async-execution-polling-design.md
硬依赖: docs/superpowers/plans/2026-06-03-systemmt-async-execution-cloud-plan.md (契约方，必须先合入 main)
---

## VM Execution Note - 2026-06-03

Status: In progress / blocked on AC-V5 failure-state evidence.

Branch `claude/async-execution-vm` implements the WPF async execution consumer and collected real VM evidence in `docs/superpowers/specs/2026-06-03-async-execution-vm-verification/`. AC-V1, AC-V2, AC-V3, AC-V4, AC-V6, AC-V7, and AC-V8 are verified. AC-V5 remains blocked because all attempted dependency-sensitive failure candidates reached `Succeeded` on this VM, so no real Failed / TimedOut / ArtifactMissing screenshot was produced. Do not mark this plan Completed or Controlled until that evidence gap is resolved.

# System MT 异步执行 + Polling（VM 消费侧 / WPF）Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 WPF 客户端加一个异步 System MT 执行界面：用户提交一个长耗时 MR 运行后立即拿到 `JobId`，页面用**定时 + 手动刷新**两种方式 polling `ISystemMtJobService.GetStatusAsync` 展示状态机进度（不阻塞 dispatcher），终止后展示 `MrRunResult`。**只消费 Cloud 计划已落地的 job 契约，不新增 / 不修改任何 `MetBench_BLL.Core` public 类型。**

**Architecture:** 按 CLAUDE.md §5 的 Page↔ViewModel 三件套：新增 `SystemMtAsyncJobPage`（XAML + code-behind）+ `SystemMtAsyncJobViewModel`。ViewModel 注入 `ISystemMtJobService`，`SubmitCommand` 调 `SubmitAsync` 拿 `JobId`，启动一个 `DispatcherTimer`（或 `PeriodicTimer` + `async` 循环）按 spec §7 推荐间隔 polling，把 `SystemMtJobStatus` 投影到 `[ObservableProperty]`；终止态停表并拉 `GetResultAsync`。worker host（`SystemMtJobWorker` 的后台驱动循环）在 WPF 进程内由一个 `IHostedService` 托管。

**Tech Stack:** WPF（`net8.0-windows7.0`）、`Wpf.Ui`、`CommunityToolkit.Mvvm`、`Microsoft.Extensions.Hosting`（generic host + hosted worker）。

---

## 0. 范围与前置约束（先读）

### 0.1 硬依赖与环境分工（CLAUDE.md §9）

- 本计划是 **VM 轨**：只动 `MetBench_Client/`（WPF）。CI 不编译 WPF，验收靠 VM 上 VS 2022 构建 + 运行 + 截图（接 README §8 / 既有 `*-vm-verification` 约定）。
- **前置**：Cloud 计划（`...-cloud-plan.md`）的 `ISystemMtJobService` / `SystemMtJobService` / `SystemMtJobWorker` / DTO 必须已合入 `main`。本计划开工前先 `git fetch origin main` 并确认 `MetBench_BLL.Core/SystemMT/Jobs/ISystemMtJobService.cs` 存在。
- **禁止**（CLAUDE.md §9）：VM agent 不得修改 `MetBench_BLL.Core/SystemMT/*` public 类型。若发现契约缺字段（如需要 `SutName` 在 `SystemMtJobStatus` 里），**不要在 VM 侧改契约**——记为「需 Cloud 侧补充」反馈，按 §9 提 Cloud-side 变更。

### 0.2 spec 边界

spec §12 把 WPF UI 列为 v1 out-of-scope，本计划即「VM follow-up」（设计 §3 原则 7：cloud 先行，WPF 消费后行）。spec §7 明确要求：WPF 允许手动刷新、不得阻塞 dispatcher 线程。

### 0.3 与既有同步页面的关系（接 §1.5 冲突挑明）

现有同步执行入口（`MTExecutionPage` / launcher 直跑）**保留不动**。本异步页面是**新增并行入口**，面向长耗时 SUT（OpenMC、Docker、远程）。不改既有同步页，不把同步页迁到异步——两套入口并存是设计意图（短任务走同步，长任务走异步），非冲突。

---

## 1. File Structure

| 文件 | 职责 |
|---|---|
| `MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml` | `<Page>` 根：提交区（MR 下拉 + 提交按钮）+ 状态区（状态/相位/进度条/JobId/失败原因 + 手动刷新按钮）+ 结果区。 |
| `MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml.cs` | code-behind：`INavigableView<SystemMtAsyncJobViewModel>`，注入 VM、`DataContext=this`、`InitializeComponent()`。 |
| `MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs` | logic：`ObservableObject`+`INavigationAware`，提交 + polling 循环 + 结果投影。 |
| `MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs` | `IHostedService`：进程内后台循环，`DequeueAsync` → `SystemMtJobWorker.RunJobAsync`。 |
| `MetBench_Client/App.xaml.cs` | DI 注册（修改：加 job service / store / queue / worker host + page/VM 对）。 |
| `MetBench_Client/ViewModels/MainWindowViewModel.cs` | 导航菜单加一项（修改：`InitializeViewModel()`）。 |

无新增 `MetBench_BLL.Core` 文件。

---

## 2. 验收标准（Acceptance Criteria — 你点名要的）

VM 不经 CI，验收靠**可复现的人工证据**（截图 + 录屏 / 状态序列日志），全部满足才算 Done。证据落 `docs/superpowers/specs/2026-06-03-async-execution-vm-verification/`（沿用既有 `*-vm-verification` 目录约定）。

- **AC-V1 编译**：`dotnet build MetBench_Client/MetBench_Client.csproj`（在 Windows VM）0 error。证据：构建输出尾部截图。
- **AC-V2 异步提交即返回**：点「提交」后 UI **立即**显示一个非空 `JobId` 且状态为 `Queued`，按钮不卡死（dispatcher 未冻结，期间可拖动窗口 / 点其他控件）。证据：提交瞬间截图（含 JobId + Queued）。
- **AC-V3 polling 推进可见**：随后状态自动从 `Queued`→`Preparing`→`RunningSource`→…→终止态，进度条同步前进；用一个 fake / 短 SUT 即可。证据：≥3 个不同状态的连续截图，或 ViewModel 把每次 polling 的 `(State, Phase, Percent)` 追加到一个可见列表的截图。
- **AC-V4 手动刷新**：点「刷新」按钮立即触发一次 `GetStatusAsync` 并更新 UI（spec §7 要求）。证据：刷新前后截图。
- **AC-V5 终止展示结果**：到 `Succeeded` 后结果区显示 `MrRunResult`（通过/失败 + 摘要）；到 `Failed/TimedOut/ArtifactMissing/Cancelled` 显示 `FailureReason`，结果区为空或显式「无结果」。证据：成功 + 一种失败 各一张截图。
- **AC-V6 不阻塞 dispatcher**：polling 循环与提交均为 `async Task`，无 `.Result` / `.Wait()`（接 CLAUDE.md §7）。证据：code review 自查 + 提交时窗口可交互的录屏。
- **AC-V7 契约零改动**：`git diff origin/main -- MetBench_BLL.Core MetBench_DAL` 为空（VM 侧未碰契约，接 §9）。证据：该命令输出为空。
- **AC-V8 取消**：对一个运行中 job 点「取消」，状态转 `Cancelled`，UI 停表。证据：取消前后截图。

---

## 3. Tasks

### Task 1: ViewModel — 提交 + polling 循环（先搭骨架，DispatcherTimer 驱动）

**Files:**
- Create: `MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs`

> WPF 不经 CI，本计划无 xUnit 步骤；每个 Task 的「验证」是 VM 上构建 + 运行观察。TDD 替代：先写最小 VM、构建通过、再接线 UI 后运行核验对应 AC。

- [ ] **Step 1: 写 ViewModel（提交 + 定时 polling + 手动刷新 + 取消）**

```csharp
// MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs
using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using Wpf.Ui.Controls;
using INavigationAware = Wpf.Ui.Controls.INavigationAware;

namespace MetBench_Client.ViewModels;

public partial class SystemMtAsyncJobViewModel : ObservableObject, INavigationAware
{
    private readonly ISystemMtJobService _jobs;
    private readonly DispatcherTimer _pollTimer;
    private Guid? _currentJobId;

    [ObservableProperty] private string _selectedMrId = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _availableMrIds = new();
    [ObservableProperty] private string _jobIdDisplay = "—";
    [ObservableProperty] private string _stateDisplay = "—";
    [ObservableProperty] private string _phaseDisplay = "—";
    [ObservableProperty] private int _progressPercent;
    [ObservableProperty] private string? _failureReason;
    [ObservableProperty] private string _resultSummary = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _pollLog = new();
    [ObservableProperty] private bool _isRunning;

    public SystemMtAsyncJobViewModel(ISystemMtJobService jobs)
    {
        _jobs = jobs;
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };  // spec §7 local/docker 1-2s
        _pollTimer.Tick += async (_, _) => await PollOnceAsync();
    }

    public async void OnNavigatedTo()  // async void 仅限于此（CLAUDE.md §7）
    {
        var summaries = await _jobs is not null
            ? await LoadMrIdsAsync()
            : Array.Empty<string>();
        AvailableMrIds = new ObservableCollection<string>(summaries);
        if (AvailableMrIds.Count > 0 && string.IsNullOrEmpty(SelectedMrId))
            SelectedMrId = AvailableMrIds[0];
    }

    public void OnNavigatedFrom() => _pollTimer.Stop();

    // MR 列表来源：若 Cloud 契约暴露列举，用之；否则注入 ISystemMtLauncher.ListAvailableAsync。
    private async Task<IReadOnlyList<string>> LoadMrIdsAsync()
        => (await _launcher.ListAvailableAsync()).Select(s => s.Id).ToList();

    // 注入 launcher 仅用于列举 MR id（只读），不用于执行。
    private readonly ISystemMtLauncher _launcher = null!;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedMrId)) return;
        ResetForNewJob();
        var handle = await _jobs.SubmitAsync(new SystemMtJobRequest(SelectedMrId));
        _currentJobId = handle.JobId;
        JobIdDisplay = handle.JobId.ToString();
        IsRunning = true;
        await PollOnceAsync();   // 立即拉一次，满足 AC-V2 即时反馈
        _pollTimer.Start();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await PollOnceAsync();  // AC-V4 手动刷新

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (_currentJobId is { } id) { await _jobs.CancelAsync(id); await PollOnceAsync(); }
    }

    private async Task PollOnceAsync()
    {
        if (_currentJobId is not { } id) return;
        var status = await _jobs.GetStatusAsync(id);
        if (status is null) return;

        StateDisplay = status.State.ToString();
        PhaseDisplay = status.CurrentPhase;
        ProgressPercent = status.ProgressPercent;
        FailureReason = status.FailureReason;
        PollLog.Add($"{status.UpdatedAtUtc:HH:mm:ss} {status.State} / {status.CurrentPhase} / {status.ProgressPercent}%");

        if (status.State.IsTerminal())
        {
            _pollTimer.Stop();
            IsRunning = false;
            await LoadResultAsync(id, status.State);
        }
    }

    private async Task LoadResultAsync(Guid id, SystemMtJobState finalState)
    {
        if (finalState == SystemMtJobState.Succeeded)
        {
            MrRunResult? result = await _jobs.GetResultAsync(id);
            ResultSummary = result is null ? "(no result)" : DescribeResult(result);
        }
        else
        {
            ResultSummary = $"{finalState}: {FailureReason ?? "(no reason)"}";
        }
    }

    // 按 MrRunResult 真实字段实现（落地前 Read MrRunResult.cs）：通过/失败 + 摘要。
    private static string DescribeResult(MrRunResult r) => r.ToString();

    private void ResetForNewJob()
    {
        PollLog.Clear(); FailureReason = null; ResultSummary = string.Empty;
        ProgressPercent = 0; StateDisplay = "—"; PhaseDisplay = "—";
    }
}
```

> 落地注意：
> 1. MR 列举注入了 `ISystemMtLauncher`（只读用），构造函数需相应改为 `(ISystemMtJobService jobs, ISystemMtLauncher launcher)`；上面骨架用了 `_launcher = null!` 占位，落地时改成构造注入。
> 2. `DescribeResult` 按 `MrRunResult` 真实字段写（通过位 + 摘要），不要用 `ToString()` 凑数。

- [ ] **Step 2: 构建（VM）**

Run: `dotnet build MetBench_Client/MetBench_Client.csproj`
Expected: 0 error（接线 UI 前先保证 VM 编译）。

- [ ] **Step 3: Commit**

```bash
git add MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs
git commit -m "feat(client): add async System MT job view-model (submit + polling)"
```

---

### Task 2: Page XAML + code-behind（CLAUDE.md §5 三件套）

**Files:**
- Create: `MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml`
- Create: `MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml.cs`

- [ ] **Step 1: 写 XAML（提交区 + 状态区 + 手动刷新 + 结果区）**

```xml
<!-- MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml -->
<Page x:Class="MetBench_Client.Views.Pages.SystemMtAsyncJobPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
      ui:Design.Background="{DynamicResource ApplicationBackgroundBrush}"
      Foreground="{DynamicResource TextFillColorPrimaryBrush}">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 提交区 -->
        <StackPanel Orientation="Horizontal" Grid.Row="0">
            <ComboBox MinWidth="280"
                      ItemsSource="{Binding ViewModel.AvailableMrIds}"
                      SelectedItem="{Binding ViewModel.SelectedMrId, Mode=TwoWay}"/>
            <ui:Button Content="提交异步运行" Margin="12,0,0,0"
                       Command="{Binding ViewModel.SubmitCommand}"
                       IsEnabled="{Binding ViewModel.IsRunning, Converter={StaticResource InverseBoolConverter}}"/>
            <ui:Button Content="刷新" Margin="12,0,0,0" Command="{Binding ViewModel.RefreshCommand}"/>
            <ui:Button Content="取消" Margin="12,0,0,0" Command="{Binding ViewModel.CancelCommand}"
                       IsEnabled="{Binding ViewModel.IsRunning}"/>
        </StackPanel>

        <!-- 状态区 -->
        <StackPanel Grid.Row="1" Margin="0,16,0,0">
            <TextBlock Text="{Binding ViewModel.JobIdDisplay, StringFormat='Job: {0}'}"/>
            <TextBlock Text="{Binding ViewModel.StateDisplay, StringFormat='状态: {0}'}"/>
            <TextBlock Text="{Binding ViewModel.PhaseDisplay, StringFormat='相位: {0}'}"/>
            <ProgressBar Minimum="0" Maximum="100" Height="8" Margin="0,4,0,0"
                         Value="{Binding ViewModel.ProgressPercent}"/>
            <TextBlock Foreground="{DynamicResource SystemFillColorCriticalBrush}"
                       Text="{Binding ViewModel.FailureReason}"
                       Visibility="{Binding ViewModel.FailureReason, Converter={StaticResource NullToCollapsedConverter}}"/>
        </StackPanel>

        <!-- 结果区 + polling 日志 -->
        <Grid Grid.Row="2" Margin="0,16,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <TextBox Grid.Column="0" IsReadOnly="True" TextWrapping="Wrap"
                     VerticalScrollBarVisibility="Auto"
                     Text="{Binding ViewModel.ResultSummary, Mode=OneWay}"/>
            <ListView Grid.Column="1" Margin="12,0,0,0"
                      ItemsSource="{Binding ViewModel.PollLog}"/>
        </Grid>
    </Grid>
</Page>
```

> `InverseBoolConverter` / `NullToCollapsedConverter`：先确认 `MetBench_Client` 现有 converters（多数 WPF 项目已有）；缺则复用既有或在落地时加最小 converter，不新造重复实现（接 §1.1 先读再写）。

- [ ] **Step 2: 写 code-behind**

```csharp
// MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml.cs
using MetBench_Client.ViewModels;
using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages;

public partial class SystemMtAsyncJobPage : INavigableView<SystemMtAsyncJobViewModel>
{
    public SystemMtAsyncJobViewModel ViewModel { get; }

    public SystemMtAsyncJobPage(SystemMtAsyncJobViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
```

- [ ] **Step 3: 构建（VM）**

Run: `dotnet build MetBench_Client/MetBench_Client.csproj`
Expected: 0 error。

- [ ] **Step 4: Commit**

```bash
git add MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml \
        MetBench_Client/Views/Pages/SystemMtAsyncJobPage.xaml.cs
git commit -m "feat(client): add async System MT job page (page + code-behind)"
```

---

### Task 3: 进程内 worker hosted service

**Files:**
- Create: `MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs`

- [ ] **Step 1: 写 hosted service（后台 Dequeue → RunJobAsync 循环）**

```csharp
// MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs
using MetBench_BLL.SystemMT.Jobs;
using Microsoft.Extensions.Hosting;

namespace MetBench_Client.Hosting;

/// <summary>
/// WPF 进程内后台 worker host：循环从 IJobQueue 取 jobId，交 SystemMtJobWorker 执行。
/// 单 worker 串行；长耗时 SUT 不阻塞 UI（独立后台线程，不碰 dispatcher）。
/// </summary>
public sealed class SystemMtJobWorkerHostedService : BackgroundService
{
    private readonly IJobQueue _queue;
    private readonly SystemMtJobWorker _worker;

    public SystemMtJobWorkerHostedService(IJobQueue queue, SystemMtJobWorker worker)
    {
        _queue = queue;
        _worker = worker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;
            try { jobId = await _queue.DequeueAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }

            // worker 内部已 fail-closed 吞异常成 Failed 记录；这里再兜一层防 host 崩溃。
            try { await _worker.RunJobAsync(jobId, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch { /* worker 已落 Failed；host 继续取下一个 */ }
        }
    }
}
```

- [ ] **Step 2: 构建（VM）**

Run: `dotnet build MetBench_Client/MetBench_Client.csproj`
Expected: 0 error。

- [ ] **Step 3: Commit**

```bash
git add MetBench_Client/Hosting/SystemMtJobWorkerHostedService.cs
git commit -m "feat(client): host in-process System MT job worker as BackgroundService"
```

---

### Task 4: DI 注册 + 导航菜单接线

**Files:**
- Modify: `MetBench_Client/App.xaml.cs`
- Modify: `MetBench_Client/ViewModels/MainWindowViewModel.cs`

> 落地前 `Read App.xaml.cs` 现有 `ConfigureServices`，把下列注册插到 System-MT 既有注册块附近（CLAUDE.md §6 给出的 launcher / repo / catalog 注册之后）。

- [ ] **Step 1: App.xaml.cs 加 job 子系统注册**

```csharp
// 在 ConfigureServices(...) 内，既有 ISystemMtLauncher 注册之后追加：

// 异步 job 子系统（消费 Cloud 契约）。store 用 durable LiteDb；队列进程内 channel。
services.AddSingleton<IJobQueue, ChannelJobQueue>();
services.AddSingleton<IJobStore>(_ =>
{
    var dataDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
    return new LiteDbJobStore($"Filename={Path.Combine(dataDir, "SystemMtJobs.Litedb")}");
});
services.AddSingleton<ISystemMtAsyncPipeline>(sp =>
    new SystemMtAsyncPipeline(sp.GetRequiredService<ISystemMtLauncher>()));
services.AddSingleton(sp =>
    new SystemMtJobWorker(sp.GetRequiredService<IJobStore>(), sp.GetRequiredService<ISystemMtAsyncPipeline>()));
services.AddSingleton<ISystemMtJobService>(sp =>
    new SystemMtJobService(sp.GetRequiredService<IJobStore>(), sp.GetRequiredService<IJobQueue>()));
services.AddHostedService<SystemMtJobWorkerHostedService>();

// page ↔ viewmodel 对（CLAUDE.md §5）
services.AddScoped<Views.Pages.SystemMtAsyncJobPage>();
services.AddScoped<ViewModels.SystemMtAsyncJobViewModel>();
```

> 注意 using：`MetBench_BLL.SystemMT.Jobs`、`MetBench_DAL`、`Microsoft.Extensions.DependencyInjection`、`Microsoft.Extensions.Hosting`。
> `ISystemMtLauncher` 须为 Singleton 或 Scoped 与 worker 生命周期相容；既有注册是 `AddScoped<ISystemMtLauncher>`——worker 是 Singleton 不能直接持有 Scoped launcher。**冲突挑明（§1.5）**：两条路径择一——(a) 把 worker 的 launcher 解析改为每 job 开 scope（`IServiceScopeFactory` 注入 hosted service，在 `RunJobAsync` 前 `CreateScope()` 取 launcher）；(b) 若 launcher 无状态可安全提升为 Singleton。**选 (a)**（不改既有 launcher 生命周期，最小侵入）；据此 `SystemMtAsyncPipeline` 与 `SystemMtJobWorker` 的构造在 hosted service 内按 scope 重建，而非 Singleton 注册。落地时据此调整上面 worker / pipeline 的注册为 scope-per-job 模式，并在 PR body 说明该决策。

- [ ] **Step 2: MainWindowViewModel 加导航项**

```csharp
// 在 InitializeViewModel() 的 NavigationItems 集合里追加：
new NavigationViewItem
{
    Content = "异步执行",
    Icon = new SymbolIcon { Symbol = SymbolRegular.Timer24 },
    TargetPageType = typeof(Views.Pages.SystemMtAsyncJobPage)
},
```

- [ ] **Step 3: 构建（VM）**

Run: `dotnet build MetBench_Client/MetBench_Client.csproj`
Expected: 0 error。

- [ ] **Step 4: Commit**

```bash
git add MetBench_Client/App.xaml.cs MetBench_Client/ViewModels/MainWindowViewModel.cs
git commit -m "feat(client): wire async job DI + navigation entry"
```

---

### Task 5: VM 运行核验 + 截图证据（AC-V1…V8）

**Files:**
- Create: `docs/superpowers/specs/2026-06-03-async-execution-vm-verification/`（截图 + 状态序列说明）

- [ ] **Step 1: 运行 app**

Run: `dotnet run --project MetBench_Client`
导航到「异步执行」页。

- [ ] **Step 2: 逐条走 AC**

依次验证 AC-V2（提交即返回 JobId/Queued）、AC-V3（状态推进 + 进度条）、AC-V4（手动刷新）、AC-V5（成功结果 + 一种失败原因）、AC-V8（取消）。每条截图归档。建议先用一个**短/确定性 SUT**（已有的本地可跑 MR）保证状态机能在秒级走到终止态。

- [ ] **Step 3: AC-V6 / V7 自查**

- 全文 grep 确认无 `.Result` / `.Wait()`：`grep -rn "\.Result\|\.Wait()" MetBench_Client/ViewModels/SystemMtAsyncJobViewModel.cs MetBench_Client/Hosting/`（应为空）。
- 契约零改动：`git diff origin/main -- MetBench_BLL.Core MetBench_DAL`（应为空）。

- [ ] **Step 4: 写验收证据文档**

把截图 + 一段「状态序列实际观测」写进 `docs/superpowers/specs/2026-06-03-async-execution-vm-verification/README.md`，逐条对照 §2 AC 打勾。

- [ ] **Step 5: 执行后回写（CLAUDE.md §11.1 第 4 步）**

更新本计划 frontmatter 状态 → `Completed`；活跃计划索引登记；`AGENTS.md` 对应 Stage 加 VM 交付记录（Stage 粒度）。

- [ ] **Step 6: Commit + PR**

```bash
git add docs/superpowers/specs/2026-06-03-async-execution-vm-verification/ \
        docs/superpowers/plans/2026-06-03-systemmt-async-execution-vm-plan.md \
        docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
git commit -m "docs(client): async execution VM verification evidence + plan closure"
```

PR body 填 `pr-gate-checklist.md` 7 节；Windows Classification 标 **WPF / VM-only，CI 不编译**；「Review」节注明本 PR 是 cloud→VM 链路的 VM 末端，若整链 ≥3 PR 需按 §12.4 R2 链尾 review。

---

## 4. Self-Review（写计划者自检，已执行）

- **Spec 覆盖**：spec §7 polling（手动 + 定时、不阻塞 dispatcher）→ Task 1 + AC-V4/V6；spec §12 WPF=VM follow-up → 整计划定位。`DispatcherTimer` 选择满足「不阻塞 dispatcher」（Tick 是 async void 事件处理器，内部 await 不卡 UI）。
- **占位扫描**：`DescribeResult` / `MrRunResult` 字段、converters 复用为「落地时对照真实源文件 / 既有资源」，非 TBD；DI 生命周期冲突已在 Task 4 Step 1 明确二选一并选定 (a)。
- **依赖一致**：所用契约类型（`ISystemMtJobService` / `SystemMtJobRequest` / `SystemMtJobHandle` / `SystemMtJobStatus` / `SystemMtJobState` / `IJobQueue` / `IJobStore` / `SystemMtJobWorker` / `ISystemMtAsyncPipeline` / `SystemMtAsyncPipeline` / `ChannelJobQueue` / `LiteDbJobStore`）与 Cloud 计划定义逐一对应；无 VM 侧自造契约类型。

## 5. 跨环境链路说明

本计划与 Cloud 计划构成一条 cloud→VM 链路（≥2 PR；若 Cloud 计划本身拆多 PR，则整链 ≥3，触发 §12.4 R2 链尾 holistic review）。合并顺序：**Cloud 全部 PR 先入 main → VM PR 后入**。VM 开工前必须确认 `origin/main` 已含 job 契约（接 §0.1 前置）。
