# v2 VM-Side Implementation Guide

> **Audience**: Windows VM developer / VM-side Claude agent.
> **Scope**: WPF UI + LLM provider wiring + end-to-end smoke verification.
> **Prereq**: cloud-side P1-P8 已交付（`MetBench_BLL.Core/` 全部 service + 接口 + DTO 已就绪），见 `docs/superpowers/plans/2026-05-13-v2-development-plan.md`。
> **Cross-environment rules**: 见 `CLAUDE.md`. VM agent 不得直接改 `MetBench_BLL.Core/SystemMT/*` 公共类型 —— 任何 cloud-side facade 变更需先回 cloud agent 走 RFC。

---

## 0 准备 / Pre-flight

1. 在 Windows VM 拉最新 branch：
   ```powershell
   git fetch origin claude/continue-phase-2-AdZ6f
   git checkout claude/continue-phase-2-AdZ6f
   ```
2. 验证 cloud 代码可编译（VM 端就能编 WPF）：
   ```powershell
   dotnet build MetBench.sln
   dotnet test MetBench_SystemMT.Tests
   ```
   **验收**: build succeed + 全部 xUnit 通过（与 cloud 一致：321/323 pass）。

3. 找参考样板页：`Views/Pages/ApplicationManagementPage.xaml` + `ViewModels/ApplicationManagementViewModel.cs`，
   下面所有新页面都按此模板抄一份再改。

---

## 1 v2 WPF 页面（共 7 个）

> 每页都遵守 `CLAUDE.md` 中的 **Page+ViewModel pairing pattern**：3 文件 + DI 双注册 + 1 个导航菜单项。
> 列表型页面统一继承 cloud 已提供的 `PagingViewModel<T>` 基类（见 §1.0）。

### 1.0 通用分页机制（**先理解、所有列表页都用**）

cloud 端 `MetBench_BLL.Core/Paging/` 提供三件套：

| 类型 | 文件 | 角色 |
|------|------|------|
| `PageRequest(int PageIndex, int PageSize)` | `Paging/PageRequest.cs` | 请求一页（0-based）+ `Skip` + `Validate()` |
| `PagedResult<T>(Items, TotalCount, PageIndex, PageSize)` | `Paging/PagedResult.cs` | 一页数据 + `TotalPages` / `HasPrevious` / `HasNext` |
| `abstract PagingViewModel<T> : ObservableObject` | `Paging/PagingViewModel.cs` | VM 基类。子类只实现 `LoadPageAsync` 即可获得：<br>**属性**: `Items` / `PageIndex` / `PageSize` / `TotalCount` / `TotalPages` / `HasPrevious` / `HasNext` / `IsLoading`<br>**命令**: `FirstPageCommand` / `PreviousPageCommand` / `NextPageCommand` / `LastPageCommand` / `RefreshCommand` |

**已落地的仓库分页 API**（VM 端**直接调用**，不用再造）：

```csharp
// 1) v2 高频实体（23 collection）—— Guid PK：IGuidRepository<T>
ObservableCollection<T> GetPage(int pageIndex, int pageSize);
int Count();

// 2) v1 method-level 实体 —— int PK：IRepository<T> 没自带分页（VM 端按需在 ViewModel 切片）

// 3) typed async pagination（SystemMT 结果集）—— ISystemMtResultRepository
Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken ct);
Task<PagedResult<SystemMtResultRecord>> ListPagedByScenarioAsync(string scenarioName, PageRequest request, CancellationToken ct);
```

#### 1.0.A 完整 ViewModel 示例（AnomalyListViewModel —— 可直接抄）

```csharp
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT.Anomaly;
using MetBench_Domain;
using MetBench_IDAL;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels;

public sealed partial class AnomalyListViewModel
    : PagingViewModel<MetBench_Domain.Anomaly>, INavigationAware
{
    private readonly IAnomalyService _service;
    private readonly IAnomalyRepository _repo;

    [ObservableProperty] private string? _severityFilter;
    [ObservableProperty] private string? _statusFilter;

    public AnomalyListViewModel(IAnomalyService svc, IAnomalyRepository repo)
    {
        _service = svc;
        _repo = repo;
        PageSize = 25;  // override base default if needed
    }

    public async void OnNavigatedTo() => await LoadAsync();
    public void OnNavigatedFrom() { }

    // 唯一必须实现的方法
    protected override Task<PagedResult<MetBench_Domain.Anomaly>> LoadPageAsync(
        PageRequest req, CancellationToken ct)
    {
        // 路径 A：无过滤 —— 直接走仓库分页（最快，DB-side skip/limit）
        if (string.IsNullOrEmpty(SeverityFilter) && string.IsNullOrEmpty(StatusFilter))
        {
            var pageItems = _repo.GetPage(req.PageIndex, req.PageSize);
            var total = _repo.Count();
            return Task.FromResult(new PagedResult<MetBench_Domain.Anomaly>(
                pageItems, total, req.PageIndex, req.PageSize));
        }

        // 路径 B：有过滤 —— 走 BLL service，内存切片
        var filter = new AnomalyFilter(Severity: SeverityFilter, Status: StatusFilter);
        var all = _service.List(filter);
        var slice = all.Skip(req.Skip).Take(req.PageSize).ToList();
        return Task.FromResult(new PagedResult<MetBench_Domain.Anomaly>(
            slice, all.Count, req.PageIndex, req.PageSize));
    }

    // 过滤变化 → 回到第一页并重新加载
    partial void OnSeverityFilterChanged(string? value) => _ = LoadAsync(PageRequest.First(PageSize));
    partial void OnStatusFilterChanged(string? value) => _ = LoadAsync(PageRequest.First(PageSize));
}
```

**关键点**:
- 不写任何 `INotifyPropertyChanged` —— 基类 `[ObservableProperty]` 都生成好了
- `LoadPageAsync` 是**唯一**要实现的方法
- 重入保护：`LoadAsync` 内部检查 `IsLoading`，命令的 `CanExecute` 也自动跟随
- 过滤变化要回到第 1 页 → `LoadAsync(PageRequest.First(PageSize))`

#### 1.0.B 完整 XAML 示例（页面分页条 —— 可直接抄）

> **已封装为 UserControl**: `MetBench_Client/Views/Controls/PagingBar.xaml`。
> 调用方只需引入 namespace + 1 行就够，不必复制 40 行分页 XAML。

```xml
<Page x:Class="MetBench_Client.Views.Pages.AnomalyListPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
      xmlns:controls="clr-namespace:MetBench_Client.Views.Controls"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      ui:Design.Background="{DynamicResource ApplicationBackgroundBrush}"
      Foreground="{DynamicResource TextFillColorPrimaryBrush}"
      mc:Ignorable="d">

  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto" />  <!-- filter bar -->
      <RowDefinition Height="*" />     <!-- data grid -->
      <RowDefinition Height="Auto" />  <!-- pagination bar -->
    </Grid.RowDefinitions>

    <!-- 过滤栏 -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="8">
      <TextBlock Text="Severity:" VerticalAlignment="Center" Margin="0,0,4,0" />
      <ComboBox SelectedItem="{Binding ViewModel.SeverityFilter}" Width="120" Margin="0,0,12,0">
        <ComboBoxItem Content="" />
        <ComboBoxItem Content="noise" />
        <ComboBoxItem Content="minor" />
        <ComboBoxItem Content="major" />
        <ComboBoxItem Content="critical" />
      </ComboBox>
      <TextBlock Text="Status:" VerticalAlignment="Center" Margin="0,0,4,0" />
      <ComboBox SelectedItem="{Binding ViewModel.StatusFilter}" Width="160">
        <ComboBoxItem Content="" />
        <ComboBoxItem Content="new" />
        <ComboBoxItem Content="investigating" />
        <ComboBoxItem Content="confirmed-bug" />
        <ComboBoxItem Content="false-positive" />
      </ComboBox>
    </StackPanel>

    <!-- 主数据列表 -->
    <ui:DataGrid Grid.Row="1" Margin="8"
                 ItemsSource="{Binding ViewModel.Items}"
                 AutoGenerateColumns="False"
                 IsReadOnly="True">
      <ui:DataGrid.Columns>
        <DataGridTextColumn Header="Id"        Binding="{Binding IdAnomaly}" Width="220" />
        <DataGridTextColumn Header="Severity"  Binding="{Binding Severity}"   Width="80"  />
        <DataGridTextColumn Header="Status"    Binding="{Binding Status}"     Width="120" />
        <DataGridTextColumn Header="Category"  Binding="{Binding Category}"   Width="120" />
        <DataGridTextColumn Header="Discovered" Binding="{Binding DiscoveredAt, StringFormat=yyyy-MM-dd HH:mm}" Width="140" />
        <DataGridTextColumn Header="Linked Bug" Binding="{Binding LinkedKnownBugId}" Width="100" />
      </ui:DataGrid.Columns>
    </ui:DataGrid>

    <!-- 分页条 (UserControl) -->
    <controls:PagingBar Grid.Row="2" DataContext="{Binding ViewModel}" />
  </Grid>
</Page>
```

**PagingBar 内部细节**（如果要改样式，改下面这一个文件，所有页面同步生效）:

`MetBench_Client/Views/Controls/PagingBar.xaml`:
- 包含 First / Previous / Next / Last 四个图标按钮 (`ui:Button` + `SymbolIcon`)
- 中间 `Page X / Y (Z total)` 文本
- 右侧 `PageSize` 下拉 (10 / 25 / 50 / 100) + `Refresh` 按钮 + 加载指示 ProgressBar
- 所有 binding 走继承的 `DataContext` —— 调用方写 `DataContext="{Binding ViewModel}"` 把 `PagingViewModel<T>` 喂进来即可
- 命令 `CanExecute` 自动随 `IsLoading` / `HasPrevious` / `HasNext` 联动（基类已做）

**多张表同页**:
```xml
<controls:PagingBar DataContext="{Binding ViewModel.LeftPagingVm}" />
<controls:PagingBar DataContext="{Binding ViewModel.RightPagingVm}" />
```
不需要 DependencyProperty；DataContext 注入足够。

#### 1.0.C 异步路径示例（SystemMtResultRepository —— typed PagedResult）

如果你绑的是 typed 异步仓库（如 `ISystemMtResultRepository`），`LoadPageAsync` 直接 await：

```csharp
public sealed partial class SystemMtResultListViewModel : PagingViewModel<SystemMtResultRecord>
{
    private readonly ISystemMtResultRepository _repo;
    public SystemMtResultListViewModel(ISystemMtResultRepository repo) { _repo = repo; }

    protected override async Task<PagedResult<SystemMtResultRecord>> LoadPageAsync(
        PageRequest req, CancellationToken ct)
    {
        // 仓库本身就返回 PagedResult<T>，原样转发即可
        return await _repo.ListPagedAsync(req, ct);
    }
}
```

#### 1.0.C-bis PagingBar UserControl 验收

cloud 端已写好 `MetBench_Client/Views/Controls/PagingBar.xaml` + `.cs`，但
**没法在 Linux 编译 WPF**，VM 端首次集成要确认：

- [ ] `MetBench_Client.csproj` 自动把 `Views/Controls/*.xaml` 当 Page 编译 —— 检查 `dotnet build MetBench_Client` 通过
- [ ] 任一列表页加上 `xmlns:controls="clr-namespace:MetBench_Client.Views.Controls"` + `<controls:PagingBar DataContext="{Binding ViewModel}" />`，启动后分页条正确渲染
- [ ] 命令绑定生效（点 Next 真的换页）
- [ ] `IsLoading` 期间 ProgressBar 显示
- [ ] `SymbolIcon` 资源全部加载（如有缺失换成 `SymbolRegular.ArrowLeft20` / `ArrowRight20` 等已确认存在的 enum）



每个继承 `PagingViewModel<T>` 的页面都要满足：

- [ ] 首次进入：自动加载第 1 页（`OnNavigatedTo` 里 `await LoadAsync()`）
- [ ] 点 `Next` → `PageIndex` +1，`Items` 替换，URL/UI 状态不卡顿
- [ ] 点 `Last` → 跳到 `TotalPages-1` 页
- [ ] 第 1 页时 `Previous` / `First` 按钮自动灰显（`CanExecute=false`）
- [ ] 末页时 `Next` / `Last` 自动灰显
- [ ] 切 `PageSize` → 当前页失效不崩，触发 reload（建议绑定到一个 `OnPageSizeChanged` partial method → `_ = LoadAsync(PageRequest.First(PageSize))`）
- [ ] 加载中 `IsLoading=true` → ProgressBar 显示 + 所有命令 `CanExecute=false`
- [ ] 重入保护：连点 5 下 `Next` 不会发起 5 个并发请求（基类 `LoadAsync` 自带 if-guard）

#### 1.0.E 不要做的反模式

| 做错 | 为什么错 | 正确做法 |
|---|---|---|
| 把 `Items.Clear()` + `for{ Add }` 写到 ViewModel 里 | 基类 `LoadAsync` 已经做了 | 只实现 `LoadPageAsync`，返回 `PagedResult<T>` |
| `LoadPageAsync` 里 `new ObservableCollection(...)` 替换 `Items` | 会断开 UI binding 引用 | 基类用 `Items.Clear()` + `Add` 增量更新，自动正确 |
| 在过滤变化时不重置到第 1 页 | 第 5 页 + 新过滤 → 可能直接空白 | `partial void OnXxxChanged → LoadAsync(PageRequest.First(PageSize))` |
| `LoadPageAsync` 抛异常 → 整个 UI 崩 | 命令链未捕获 | 在 `LoadPageAsync` 内部 catch，把错误填到 `ErrorMessage` ObservableProperty 显示 |
| 把 `IPageService.GetPage(typeof(X))` 当分页 | 那是 WPF 页面导航服务，不是数据分页 | 看 `PagingViewModel<T>`，别看 `IPageService` |

---

### 1.1 AnomalyListPage（P6 配套）

| 文件 | 内容 |
|---|---|
| `Views/Pages/AnomalyListPage.xaml` | `ui:DataGrid` 列出 Anomaly；过滤栏（Severity / Status / DateRange / KnownBugId）；状态转移按钮 |
| `Views/Pages/AnomalyListPage.xaml.cs` | 标准 code-behind，`INavigableView<AnomalyListViewModel>` |
| `ViewModels/AnomalyListViewModel.cs` | 继承 `PagingViewModel<MetBench_Domain.Anomaly>`，注入 `IAnomalyService` + `IAnomalyRepository` |

> **ViewModel/XAML 模板**: 见 §1.0.A / §1.0.B —— 抄即可。本页特有的扩展：
> - `[ObservableProperty] CommonalityReport? _commonality;`
> - `[RelayCommand] AnalyzeCommonality()` → `Commonality = _service.AnalyzeCommonalities(Items.ToList());`
> - `[RelayCommand] TransitionAsync(Anomaly a)` → 弹对话框 → `_service.TransitionStatus(...)` → `RefreshCommand.Execute(null)`

**验收（页面专属，分页通用项见 §1.0.D）**:
- 启动应用 → 导航到 "Anomalies"
- DataGrid 显示行（cloud 端 `LiteDbAnomalyRepository` 落库的数据）
- 选 severity=major → 列表过滤
- 点 "Analyze commonality" → 弹出 CommonalityReport 中 `Hypothesis` 文本
- 选一行 → "Transition to investigating" → 状态改 + AuditLog 有一条 `anomaly.status-change`

### 1.2 ReplayResultPage（P6 配套）

显示一次 ReplayService 跑完后的对比表（original vs replay → classification）。

| 文件 | 内容 |
|---|---|
| `Views/Pages/ReplayResultPage.xaml` | 2 列对比表：MR / SUT / 触发参数 / source / followup / assertion / classification |
| `ViewModels/ReplayResultViewModel.cs` | 注入 `ReplayService` + `ISystemMtPipeline`；调用 `ReplayAsync(ctx, original)` |

入口：从 AnomalyListPage 右键 "Replay this anomaly" 弹此页。

**验收**:
- 触发一次 anomaly 的 Replay → 显示 `ReplayClassification`（如 `Reproduced` / `FixedOrFlaky`）
- UI 显示 6 个分类对应不同颜色

### 1.3 DiscoveryPage（P7 配套）

驱动一次 MR Discovery（MetaPattern 或 LLM-Native）→ 列出 CandidateMR。

| 文件 | 内容 |
|---|---|
| `Views/Pages/DiscoveryPage.xaml` | Discoverer 下拉（metapattern-noether / llm-native）+ Target SUT 下拉 + "Run discovery" 按钮 + 结果 DataGrid |
| `ViewModels/DiscoveryViewModel.cs` | 注入 `DiscoveryService` + `IEnumerable<IMRDiscoverer>` |

```csharp
[RelayCommand]
private async Task RunDiscoveryAsync()
{
    var disc = SelectedDiscoverer;  // metapattern-noether | llm-native
    var result = await _discoveryService.RunAsync(disc, methodId: 1,
        targetApplicationId: SelectedSutId, actor: "wpf-user");
    LastRun = result;
    LoadCandidates();
}
```

**验收**:
- 选 "metapattern-noether" + 任意 SUT → "Run discovery" → 至少 3 个 CandidateMR 落入 LiteDB CandidateMRs 表
- DiscoveryRuns 表新增一行，status=ok

### 1.4 CandidateReviewPage（P7 配套）

对单个 CandidateMR 跑 ≥2 validator → 自动 promote。

| 文件 | 内容 |
|---|---|
| `Views/Pages/CandidateReviewPage.xaml` | 候选信息卡 + 3 个 validator checkbox + "Validate selected" 按钮 + ValidationRun 历史表 |
| `ViewModels/CandidateReviewViewModel.cs` | 注入 `ValidationService` + 三个 `IMRValidator` 实例 |

**验收**:
- 选 EmpiricalValidator + TheoreticalLlmValidator + AdversarialMutmutValidator → "Validate"
- ValidationRuns 表新增 3 行
- 若 ≥2 通过 → 候选状态变 promoted + MetamorphicRelations 表新增一行 + AuditLog 一条 `candidate.promote`

### 1.5 MutationCampaignPage（P7 配套）

新建并运行一次 MutationCampaign。

| 文件 | 内容 |
|---|---|
| `Views/Pages/MutationCampaignPage.xaml` | Mutants 多选 + MRBindings 多选 + SampleCases 列 + "Start campaign" 按钮 + 进度条 + 结果矩阵 |
| `ViewModels/MutationCampaignViewModel.cs` | 注入 `MutationCampaignService` + `MutationCellRunner`（VM 端用真实 `ISystemMtPipeline`） |

VM 端要实现真实 `MutationCellRunner` 委托（cloud 端用的是 fake）：
```csharp
private async Task<MutationCellOutcome> RealCellRunner(MutationCellInput input, CancellationToken ct)
{
    // 1. apply mutant patch to working SUT
    // 2. build PipelineContext from MRBinding + SampleCase
    // 3. await _pipeline.ExecuteAsync(ctx, ct)
    // 4. map outcome → "detected" / "missed" / "error" / "not-affected"
}
```

**验收**:
- 选 5 mutants × 5 MRBindings → "Start" → 进度条逐 cell 推进
- 完成后 MutationResults 表有 25 行
- 摘要显示 detection-rate

### 1.6 CoverageDashboardPage（P8 配套）

可视化 4 维 coverage。

| 文件 | 内容 |
|---|---|
| `Views/Pages/CoverageDashboardPage.xaml` | 4 个 LiveCharts pie/donut：MetaPattern / SUT×MR / Bug / Mutation |
| `ViewModels/CoverageDashboardViewModel.cs` | 注入 `CoverageService`；启动时调 `Compute()` |

**验收**:
- 导航进页面 → 4 个图都画出
- 数字对得上 `CoverageService.Compute()` 单测里的算法

### 1.7 TrendDashboardPage（P8 配套）

显示周报 + WoW 对比 + bursts。

| 文件 | 内容 |
|---|---|
| `Views/Pages/TrendDashboardPage.xaml` | DatePicker 选周起始 + KPI 卡片（execs / anomalies / rate / WoW Δ）+ LiveCharts 折线 + bursts 列表 |
| `ViewModels/TrendDashboardViewModel.cs` | 注入 `TrendAnalysisService` |

**验收**:
- 选本周 → Headline 文本显示
- 任意制造一组异常爆发数据 → bursts 列表非空 + Headline 包含 "burst"

---

## 2 DI 注册（一次性）

`MetBench_Client/App.xaml.cs` 的 `ConfigureServices`：

```csharp
// === v2 services ===
services.AddSingleton<IAnomalyService, AnomalyService>();
services.AddSingleton<DiscoveryService>();
services.AddSingleton<ValidationService>();
services.AddSingleton<MutationCampaignService>();
services.AddSingleton<CoverageService>();
services.AddSingleton<TrendAnalysisService>();
services.AddSingleton<SystemMtReportService>();
services.AddSingleton<ReplayService>();

// === v2 IDAL → LiteDB ===
services.AddV2LiteDbRepositories(connectionString: ...); // ServiceCollectionExtensions

// === Discovery LLM gateway (real impl, 见 §3) ===
services.AddSingleton<ILlmGateway, DeepSeekLlmGateway>();

// === Discoverers / Validators ===
services.AddSingleton<IMRDiscoverer>(sp => new MetaPatternDiscoverer(
    pythonExe: GetSetting("METBENCH_PYTHON") ?? "python",
    scriptPath: Path.Combine(AppContext.BaseDirectory, "..", "..", "tools", "noether_candidates.py")));
services.AddSingleton<IMRDiscoverer>(sp => new LlmNativeDiscoverer(sp.GetRequiredService<ILlmGateway>()));

// === Pages + ViewModels (×7) ===
services.AddScoped<Views.Pages.AnomalyListPage>();
services.AddScoped<ViewModels.AnomalyListViewModel>();
services.AddScoped<Views.Pages.ReplayResultPage>();
services.AddScoped<ViewModels.ReplayResultViewModel>();
services.AddScoped<Views.Pages.DiscoveryPage>();
services.AddScoped<ViewModels.DiscoveryViewModel>();
services.AddScoped<Views.Pages.CandidateReviewPage>();
services.AddScoped<ViewModels.CandidateReviewViewModel>();
services.AddScoped<Views.Pages.MutationCampaignPage>();
services.AddScoped<ViewModels.MutationCampaignViewModel>();
services.AddScoped<Views.Pages.CoverageDashboardPage>();
services.AddScoped<ViewModels.CoverageDashboardViewModel>();
services.AddScoped<Views.Pages.TrendDashboardPage>();
services.AddScoped<ViewModels.TrendDashboardViewModel>();
```

**验收**: `App.xaml.cs` 编译通过，启动应用 service container 无 resolve 异常。

---

## 3 真实 LLM Gateway（替换 NullLlmGateway）

cloud 端默认绑 `NullLlmGateway`（返回空字符串）。VM 端要实现一个真正的 provider，让 `LlmNativeDiscoverer` + `TheoreticalLlmValidator` 能调通。

### 推荐位置

`MetBench_BLL/Discovery/DeepSeekLlmGateway.cs`（**MetBench_BLL** 是 WPF-only 项目，可以放 Windows 专属代码 / `HttpClient`）

### 骨架

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MetBench_BLL.Discovery;

public sealed class DeepSeekLlmGateway : ILlmGateway
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public DeepSeekLlmGateway(IConfiguration config)
    {
        _http = new HttpClient();
        _apiKey = config["DeepSeek:ApiKey"] ?? throw new InvalidOperationException("DeepSeek:ApiKey not set");
        _baseUrl = config["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com/anthropic";
        _model = config["DeepSeek:Model"] ?? "deepseek-v4-pro";
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var body = new
        {
            model = _model,
            max_tokens = 2048,
            messages = new[] { new { role = "user", content = prompt } },
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/messages")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");

        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        // Anthropic-shaped: content[0].text
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }
}
```

**配置**: 用 `MetBench_Client/appsettings.json`（gitignore 中加 `appsettings.local.json`）：
```json
{
  "DeepSeek": {
    "ApiKey": "sk-...",
    "BaseUrl": "https://api.deepseek.com/anthropic",
    "Model": "deepseek-v4-pro"
  }
}
```

**验收**:
- 不修改 cloud 单测的前提下，在 DiscoveryPage 选 llm-native → 真正打 API → 拿到 ≥1 CandidateMR
- 网络断开时 `LlmNativeDiscoverer` 的 catch 块把 DiscoveryRun 标 error，UI 显示错误（不崩）

---

## 4 导航菜单（一次性）

`MetBench_Client/ViewModels/MainWindowViewModel.cs` `InitializeViewModel()` 加 7 项：

```csharp
new NavigationViewItem
{
    Content = "Anomalies",
    Icon = new SymbolIcon { Symbol = SymbolRegular.Warning24 },
    TargetPageType = typeof(Views.Pages.AnomalyListPage),
},
new NavigationViewItem
{
    Content = "Discovery",
    Icon = new SymbolIcon { Symbol = SymbolRegular.Lightbulb24 },
    TargetPageType = typeof(Views.Pages.DiscoveryPage),
},
new NavigationViewItem
{
    Content = "Candidate Review",
    Icon = new SymbolIcon { Symbol = SymbolRegular.Checkmark24 },
    TargetPageType = typeof(Views.Pages.CandidateReviewPage),
},
new NavigationViewItem
{
    Content = "Mutation Campaigns",
    Icon = new SymbolIcon { Symbol = SymbolRegular.BeakerEdit24 },
    TargetPageType = typeof(Views.Pages.MutationCampaignPage),
},
new NavigationViewItem
{
    Content = "Coverage",
    Icon = new SymbolIcon { Symbol = SymbolRegular.DataPie24 },
    TargetPageType = typeof(Views.Pages.CoverageDashboardPage),
},
new NavigationViewItem
{
    Content = "Trends",
    Icon = new SymbolIcon { Symbol = SymbolRegular.DataTrending24 },
    TargetPageType = typeof(Views.Pages.TrendDashboardPage),
},
```

**验收**: 启动 → 左侧菜单看到全部 6+ 项（ReplayResultPage 由 AnomalyListPage 弹出，不上菜单）+ 每项点击能打开页面（即使空数据也不应崩）。

---

## 5 周报 webhook（可选）

cloud 端 `SystemMtReportService.GenerateWeekly()` 输出 markdown。VM 端可加：

- 一个 `MetBench_BLL/Reporting/WeeklyEmailService.cs`，把 markdown → HTML → SMTP 发到订阅者
- 或一个 `WeeklySlackService.cs`，POST 到 Slack incoming webhook

**验收**: 手动触发 → 邮箱 / Slack 频道收到周报。

---

## 6 端到端 smoke（v2 ship 验收）

`docs/superpowers/plans/2026-05-13-v2-development-plan.md` § P8.5。VM 端跑一次：

1. **新 SUT 接入** → ApplicationManagementPage 加 OpenMOC + OpenMC
2. **CRUD MR** → MRManagementPage 录入 MR-T / MR02
3. **启动 Execution** → MTExecutionPage 选 MR-T × OpenMOC，跑出一次 anomaly
4. **Anomaly drill** → AnomalyListPage 看到 anomaly，点 Transition → investigating
5. **Replay** → 右键 Replay → ReplayResultPage 显示 Reproduced
6. **Discovery** → DiscoveryPage 跑 metapattern → 看到 ≥3 候选
7. **Validation** → CandidateReviewPage 通过 2 个 → 一个 MR 被 promote
8. **MutationCampaign** → MutationCampaignPage 跑 5×5 → 看 detection-rate
9. **Coverage** → CoverageDashboardPage 4 图都有数据
10. **Trend** → TrendDashboardPage 周报 Headline 显示

**验收**: 10 步一次走通 + LiteDB 23 个 collection 都至少有一行数据（除 paper-package 那条 Report）+ 录屏存档。

---

## 7 验收总清单（v2 ship）

- [ ] 所有 6 个新页面在 WPF 启动后可见且可打开
- [ ] DI 容器 resolve 全部 v2 service 不抛
- [ ] `DeepSeekLlmGateway` 真实调通至少一次 LLM API（DiscoveryPage 或 CandidateReviewPage）
- [ ] 端到端 smoke §6 十步走通，每步在 LiteDB 留下可验证的记录
- [ ] 截图存到 `docs/screenshots/v2-ship-{date}/` 共 ≥10 张
- [ ] 把本 doc 中每个"验收"项打勾后，把本文件 `[ ]` 改 `[x]`，commit 推送
- [ ] 在 PR description 中链接录屏 + 截图

---

## 8 常见坑 / Troubleshooting

| 现象 | 原因 | 处理 |
|---|---|---|
| `App.xaml.cs` build 失败找不到 `AnomalyService` | `MetBench_BLL.Core` 没 reference | 检查 `MetBench_Client.csproj` `<ProjectReference Include="..\MetBench_BLL.Core\MetBench_BLL.Core.csproj" />` 已存在 |
| 导航点击后白屏 | Page 构造抛了但 WPF 静默吞掉 | 加 `App.xaml.cs DispatcherUnhandledException` handler，弹 MessageBox |
| LiteDB throw `BsonExpression invalid` | v2 collection mapper 没注册 | 调 `V2DbConfig.RegisterMappers()`（已在 P1.8 配好，App 启动时调一次） |
| DataGrid 空白但 Items.Count > 0 | XAML `{Binding ViewModel.Items}` 写成 `{Binding Items}` | code-behind `DataContext = this`，binding 必须穿 ViewModel |
| `MetaPatternDiscoverer` 报 python not found | PATH 没有 python | 用 `METBENCH_PYTHON` 环境变量指定绝对路径 |
| `appsettings.local.json` 不读 | `Host.CreateDefaultBuilder` 没加 | `.ConfigureAppConfiguration(c => c.AddJsonFile("appsettings.local.json", optional: true))` |

---

## 9 与 cloud agent 协作规则

- VM 端不得 commit 改 `MetBench_BLL.Core/SystemMT/*` 中的 public 类型签名（CI 会 catch）
- 若 VM 端发现 cloud-side service 缺接口，**先在 PR description 中提**，cloud agent 加完再 rebase
- VM 端可自由改：`MetBench_Client/*`、`MetBench_BLL/*`（WPF-only）、`appsettings.*`、`docs/screenshots/`

---

## 附录：cloud 端就绪检查表

| Cloud 交付物 | VM 用到的位置 | 验证 |
|---|---|---|
| `IAnomalyService` + `AnomalyService` | AnomalyListPage / ReplayResultPage | DI 绑定 + 单测 14/14 pass |
| `IMRDiscoverer` + 2 impl + `DiscoveryService` | DiscoveryPage | DI 绑定 + 单测 12/12 pass |
| 3 `IMRValidator` + `ValidationService` | CandidateReviewPage | DI 绑定 + 单测 14/14 pass |
| `MutationCampaignService` | MutationCampaignPage | DI 绑定 + 单测 8/8 pass |
| `CoverageService` + `CoverageReport` | CoverageDashboardPage | DI 绑定 + 单测 5/5 pass |
| `TrendAnalysisService` + `WeeklyReport` | TrendDashboardPage | DI 绑定 + 单测 7/7 pass |
| `SystemMtReportService` 5 scope | 周报触发器 / 任何 page 的 "Export report" | DI 绑定 + 单测 6/6 pass |
| `PagingViewModel<T>` + `PageRequest` + `PagedResult<T>` | 所有列表型 ViewModel 继承（用法见 §1.0） | DI 不需要（base 类） + 单测 22/22 pass |
| `IGuidRepository<T>.GetPage(int, int) + Count()` | 23 v2 collection 的服务器端分页 | LiteDB 实现位于 `LiteDbGuidPkRepositoryBase.GetPage`（Skip/Limit） |
| `ISystemMtResultRepository.ListPagedAsync(PageRequest)` | typed 异步 typed pagination | 返回 `Task<PagedResult<SystemMtResultRecord>>` |
| `ILlmGateway` 抽象 | DeepSeekLlmGateway VM 实现 | cloud 默认绑 NullLlmGateway，VM 用 `services.Replace(...)` 覆盖 |

如本表任一行的 "验证" 列对不上，cloud 端没交付完，**先回 cloud agent 报缺漏，再做 VM 工作**。
