# T2 WPF Result Viewer + 4-end Export Design (Windows-only Complement of Linux T2 Plan)

> **Date**: 2026-05-27
> **Status**: Active design (PR-W1 / PR-W2 / PR-W3 pending VM track execution)
> **Scope**: Define the WPF-side complement that closes CLAUDE.md §2.2 T2 "图表展示 + 4 端报告生成" by consuming the Linux-only artifacts shipped by the T2 sequenced plan.
> **Companion plan (Linux-only)**: [`docs/superpowers/plans/2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md`](../plans/2026-05-27-t2-systemmt-visualization-and-t3-gap-fill-plan.md)
> **Implementation plan**: to be produced by `superpowers:writing-plans` from this spec.

---

## 1. 动机

Linux T2 sequenced plan 把 SystemMT 可视化与 4-end 报表的**数据 / 渲染层**整体 ship 进 `MetBench_BLL.Core` + `MetBench_BLL`（`ChartFigure` DTO、`SkiaChartRenderer`、`Pdf/Word/Excel/HtmlSystemMtResultReportRenderer`），并显式声明 non-goal "No WPF / XAML / `MetBench_Client/` change"。结果是：

- CLAUDE.md §2.2 T2 的完整定义（图表展示 **+** 4 端报告生成）**WPF 侧投影**目前为零：无 `SystemMtResultPage`，无 SystemMT 交互图，无 4-end 导出 UI。
- WPF 已有 `LiveChartsCore.SkiaSharpView.WPF` 2.0.0-rc5.4 + `Microsoft.Web.WebView2` 1.0.3179.45 +  legacy `Services/Plotting/{Base,Line,Scatter,Pie}Plotter.cs` 基础设施可直接借鉴。

本 spec 定义 **Approach B（Native LiveCharts WPF Plotter）** 的设计 —— 在 `MetBench_Client/` 内消费 Linux 侧产物，闭合 T2 全部用户可见功能。

---

## 2. 范围

### 2.1 在范围

- 新增 `MetBench_Client/Services/Plotting/SystemMt/*` —— 平行于 legacy plotter 命名空间的 SystemMT 专用 plotter 族（`BinaryRunPlotter` / `PhaseConvergencePlotter` / `HistoricalTrendPlotter` + factory）。
- 新增 `MetBench_Client/Views/Pages/SystemMtResultPage.xaml(+.xaml.cs)` —— SystemMT 运行结果浏览页（DataGrid + 交互式 `CartesianChart`）。
- 新增 `MetBench_Client/ViewModels/SystemMtResultViewModel.cs` —— `CommunityToolkit.Mvvm` + `INavigationAware`，提供 `Refresh / ViewMode 切换 / Export[Pdf|Word|Excel|Html]` 命令。
- 在 `App.xaml.cs` 注册新 plotter / page / ViewModel + 4 个 renderer（若 Linux T2 phase 未注册）。
- 在 `MainWindowViewModel.InitializeViewModel()` 加 "SystemMT 结果" 导航项。
- WebView2 内嵌 PDF / HTML 预览；Word / Excel 通过 `Process.Start` 交给系统默认应用打开。

### 2.2 不在范围（显式 non-goals）

- 不动 legacy method-MT plotter（`MetBench_Client/Services/Plotting/{Base,Line,Scatter,Pie}Plotter.cs`），保持两套并行（CLAUDE.md §0.5 最小修改）。
- 不动现有 `SystemMtExecutionPage` —— 跨页 event bus / "执行完直接跳结果页" 等联动不做。
- 不引入新 cross-platform 单元测试（plotter 与 ViewModel 故意保持仅 VM 手验，理由见 §7.4）。
- 不引入 UIAutomation smoke（与 T1 Windows VM 计划的 Optional 一致）。
- 不引入 i18n / 性能基准 / 跨 Windows 版本测试矩阵。
- 不动仓库 logging 基线。
- 不为本计划改动 BLL.Core / BLL（CLAUDE.md §3 invariant：Windows-only PR 不动 cross-platform 项目）。

---

## 3. 架构与模块边界

```
MetBench_Client/
├── Services/Plotting/SystemMt/                    [新]
│   ├── ISystemMtChartPlotter.cs
│   ├── SystemMtChartBinding.cs
│   ├── SystemMtChartPlotterFactory.cs
│   ├── BinaryRunPlotter.cs                        ↔ ChartFigureKind.BinaryScatter
│   ├── PhaseConvergencePlotter.cs                 ↔ ChartFigureKind.PhaseLine
│   └── HistoricalTrendPlotter.cs                  ↔ ChartFigureKind.HistoricalTrend
├── Views/Pages/
│   └── SystemMtResultPage.xaml(.cs)               [新]
└── ViewModels/
    └── SystemMtResultViewModel.cs                  [新]
```

**依赖方向（不可反转）**：

```
MetBench_Client (WPF, net8.0-windows7.0)
  ├─→ MetBench_BLL.Core (ChartFigure / ISystemMtChartDataProjector / I{Pdf,Word,Excel,Html}SystemMtResultReportRenderer)
  └─→ MetBench_BLL      (SkiaChartRenderer 实现 + 4 个 renderer 实现)
                ↓
            Linux-only — 不反向依赖 WPF
```

**plotter 不进 BLL.Core / BLL 的理由**：plotter 即便仅使用 `LiveChartsCore.SkiaSharpView`（cross-platform）也仍归属 `Services/Plotting/` 既有目录以与 legacy plotter 对称；混放会破坏 §0.5 最小修改原则。

---

## 4. 组件 & 公共契约

### 4.1 Plotter 公共出口类型

```csharp
public sealed record SystemMtChartBinding(
    string Title,
    IReadOnlyList<ISeries> Series,
    IReadOnlyList<ICartesianAxis> XAxes,
    IReadOnlyList<ICartesianAxis> YAxes);
```

XAML 仅绑这一个对象（而非 4 个独立属性）以避免多属性同步竞态。

### 4.2 Plotter 接口与实现

```csharp
public interface ISystemMtChartPlotter
{
    SystemMtChartBinding Build(ChartFigure figure);
}

sealed class BinaryRunPlotter        : ISystemMtChartPlotter { /* ScatterSeries × 2 */ }
sealed class PhaseConvergencePlotter : ISystemMtChartPlotter { /* LineSeries 跨 phases */ }
sealed class HistoricalTrendPlotter  : ISystemMtChartPlotter { /* LineSeries × 时间轴 */ }

public sealed class SystemMtChartPlotterFactory
{
    public SystemMtChartBinding Build(ChartFigure figure) => figure.Kind switch
    {
        ChartFigureKind.BinaryScatter   => _binary.Build(figure),
        ChartFigureKind.PhaseLine       => _phase.Build(figure),
        ChartFigureKind.HistoricalTrend => _history.Build(figure),
        _ => throw new NotSupportedException($"Unknown ChartFigureKind: {figure.Kind}")
    };
}
```

### 4.3 ViewModel 公共面

```csharp
public enum ChartViewMode { Binary, Phase, Historical }

public partial class SystemMtResultViewModel : ObservableObject, INavigationAware
{
    [ObservableProperty] private ObservableCollection<SystemMtResultRecord> _records = new();
    [ObservableProperty] private SystemMtResultRecord? _selectedRecord;
    [ObservableProperty] private ChartViewMode _viewMode = ChartViewMode.Binary;
    [ObservableProperty] private SystemMtChartBinding? _chartBinding;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _previewUri;
    [ObservableProperty] private bool _canShowPhaseView;
    [ObservableProperty] private bool _canShowHistoricalView;

    [RelayCommand] private Task RefreshAsync();
    [RelayCommand(CanExecute=nameof(CanExport))] private Task ExportPdfAsync();
    [RelayCommand(CanExecute=nameof(CanExport))] private Task ExportWordAsync();
    [RelayCommand(CanExecute=nameof(CanExport))] private Task ExportExcelAsync();
    [RelayCommand(CanExecute=nameof(CanExport))] private Task ExportHtmlAsync();
    private bool CanExport() => !IsBusy && Records.Count > 0;

    public async void OnNavigatedTo() => await RefreshAsync();
    public void OnNavigatedFrom() {}
}
```

### 4.4 ViewMode → projector 映射

| `ViewMode` | 投影器（来自 BLL.Core） | 适用前置 |
|---|---|---|
| `Binary` | `BinaryRunPointProjector.Project(record)` | 恒真兜底 |
| `Phase` | `PhaseConvergenceProjector.Project(record.PhaseMetrics, mrId, metric)` | `record.PhaseMetrics != null && Count ≥ 2` |
| `Historical` | `HistoricalTrendProjector.ProjectAsync(mrId, lookback=20, repo, ct)` | repo 内同 `MrId` 记录 ≥ 2 条 |

不适用时对应 view-mode radio button 灰掉；当前若已选不适用 mode 自动 fallback 到 `Binary`，**不抛异常、不静默切换**（显式 `StatusMessage` 提示）。

### 4.5 SystemMtResultPage.xaml 骨架

```xml
<Page xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
      xmlns:lvc="clr-namespace:LiveChartsCore.SkiaSharpView.WPF;assembly=LiveChartsCore.SkiaSharpView.WPF"
      xmlns:webview2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
      ui:Design.Background="{DynamicResource ApplicationBackgroundBrush}"
      Foreground="{DynamicResource TextFillColorPrimaryBrush}">
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>  <!-- toolbar: Refresh + ViewMode radios -->
      <RowDefinition Height="*"/>     <!-- main: DataGrid | CartesianChart -->
      <RowDefinition Height="Auto"/>  <!-- 4 export buttons -->
      <RowDefinition Height="Auto"/>  <!-- WebView2 preview (Collapsed 时高度 0) -->
    </Grid.RowDefinitions>
    <!-- main row: 左 DataGrid 绑 Records/SelectedRecord；右 lvc:CartesianChart 绑 ChartBinding.{Title,Series,XAxes,YAxes} -->
    <!-- export buttons 绑 4 个 RelayCommand；WebView2.Source 绑 PreviewUri -->
  </Grid>
</Page>
```

### 4.6 DI 注册

```csharp
services.AddSingleton<BinaryRunPlotter>();
services.AddSingleton<PhaseConvergencePlotter>();
services.AddSingleton<HistoricalTrendPlotter>();
services.AddSingleton<SystemMtChartPlotterFactory>();
services.AddScoped<Views.Pages.SystemMtResultPage>();
services.AddScoped<ViewModels.SystemMtResultViewModel>();
// IPdf/Word/Excel/HtmlSystemMtResultReportRenderer：Linux T2 Phase 3a/3b/3c 计划已落地时随 PR 同步注册
```

### 4.7 导航菜单

```csharp
new NavigationViewItem {
    Content = "SystemMT 结果",
    Icon = new SymbolIcon { Symbol = SymbolRegular.DataBarVertical24 },
    TargetPageType = typeof(Views.Pages.SystemMtResultPage)
}
```

---

## 5. 数据流

### 5.1 View flow

```
[User] 导航至 "SystemMT 结果"
   ↓
INavigationService → SystemMtResultPage(scope) → DataContext = page
   ↓
ViewModel.OnNavigatedTo() → RefreshAsync()
   ↓
ISystemMtResultRepository.LoadAllAsync(ct)
   ↓
Records.Clear() / Add(record × N)                 → DataGrid 重绑
   ↓
SelectedRecord = Records.FirstOrDefault()         → 触发 OnSelectedRecordChanged
   ↓
ResolveViewMode()                                 // 默认 Binary；若 PhaseMetrics 可用且当前选 Phase 保持
   ↓
ProjectChartFigure()                              // §4.4 表分派
   ↓
SystemMtChartPlotterFactory.Build(figure)        → SystemMtChartBinding
   ↓
ChartBinding = ...                                → CartesianChart 自动重绘
```

**Cancellation**：`OnSelectedRecordChanged` 维护 `CancellationTokenSource _projectionCts`；切换记录时 `cts.Cancel(); cts = new()` 避免 Historical 慢 IO 把旧投影盖到新 record。

### 5.2 ViewMode 切换

```
[User] click ViewMode radio
   ↓
TwoWay binding → OnViewModeChanged
   ↓
(if SelectedRecord != null) ProjectChartFigure() → ChartBinding
```

`CanShowPhaseView` / `CanShowHistoricalView` 在 `OnSelectedRecordChanged` / `RefreshAsync` 末端计算一次。

### 5.3 Export flow（以 PDF 为例，其他三种对称）

```
[User] 点 "导出 PDF" → ExportPdfCommand
   ↓
IsBusy = true; StatusMessage = "正在生成 PDF..."
   ↓
Task.Run(() => _pdfRenderer.Render(records, evidenceByGuid, reportContext))
   ↓
SaveFileDialog(filter="*.pdf")  → 用户挑路径 (或 Cancel)
   ↓
await File.WriteAllBytesAsync(path, bytes, ct)
   ↓
StatusMessage = $"已保存 {path}"
PreviewUri = new Uri(path, UriKind.Absolute).AbsoluteUri   // 仅 PDF / HTML 设
   ↓
WebView2 Navigate(PreviewUri) → 内嵌预览
   ↓ finally
IsBusy = false
```

### 5.4 预览支持矩阵

| Format | WebView2 内嵌预览 | 备注 |
|---|---|---|
| **HTML** | ✅ | 直接 `file://` 加载，可先存 temp 再 Navigate |
| **PDF** | ✅ | Edge WebView2 自带 PDF viewer，沿用 `MTReportGeneratorPage.xaml:52` 先例 |
| **Word** | ❌ | "已保存 + 是否打开" 提示；点 "打开" → `Process.Start(path, UseShellExecute=true)` |
| **Excel** | ❌ | 同 Word |

### 5.5 Refresh flow

仅两条路径，**不引跨页 event bus**：

1. 用户点工具栏 Refresh 按钮 → `RefreshAsync()`。
2. 导航重入：`OnNavigatedTo` 每次都跑一次 `RefreshAsync()`，离开-回来即拉新数据。

---

## 6. 错误处理 & 边界

### 6.1 输入边界

| 情景 | 行为 |
|---|---|
| Repo 空 | DataGrid 空态 + 图表区 "暂无 SystemMT 运行结果"；4 export 按钮 `CanExecute = false` |
| Repo 仅 1 条 | `CanShowHistoricalView = false`；Binary / Phase 正常 |
| `record.PhaseMetrics == null \|\| Count < 2` | Phase radio 灰；当前若为 Phase 自动 fallback 到 Binary |
| `ChartFigure.SeriesList` 空 | plotter 返回 `Series = []`，XAML 显示 "(无数据点)" overlay；不抛异常 |
| `ChartPoint` 含 NaN / ±∞ | LiveCharts 跳过；`StatusMessage = "提示：N 个数据点为 NaN/Inf 已跳过"`（显式提示，CLAUDE.md §6） |
| `metric` 不在 `PhaseMetrics.Keys` | `AvailableMetrics` 下拉仅含合法值；万一非法 → `KeyNotFoundException` 转 InfoBar |
| `evidenceByGuid == null` | Renderer 已支持（Linux Phase 3a/3b/3c 验收守住），report 跳过 TypedVerification 块 |

### 6.2 投影 / 渲染异常

| 来源 | 处理 |
|---|---|
| `PhaseConvergenceProjector` 空 dict / `ArgumentException` | 理论被 §4.4 前置守住；到达即 InfoBar |
| `HistoricalTrendProjector` IO `IOException` | `StatusMessage = "历史趋势加载失败：{msg}"`；留在 Binary |
| `OperationCanceledException`（切换重投） | 静默吃掉，cts.Cancel 的正常路径 |
| `NotSupportedException`（未知 Kind） | **不 catch** —— fail-fast（CLAUDE.md §6 显式报错；不静默） |
| Renderer 抛任意 Exception | catch in `ExportXxxAsync` → 红 InfoBar `导出失败：{msg}`；不重试；`IsBusy = false` |

### 6.3 IO / 文件系统

| 情景 | 处理 |
|---|---|
| SaveFileDialog Cancel | `StatusMessage = "已取消"`，finally 复位 IsBusy |
| 权限拒绝 / 磁盘满 | `UnauthorizedAccessException` / `IOException` → InfoBar；不留半截文件 |
| 路径含中文 / 空格 / 全角 | `PreviewUri = new Uri(path, UriKind.Absolute).AbsoluteUri` 走 URL 编码 |
| HTML 预览 temp 文件 | 记入 `_tempPreviewFiles : List<string>`；`OnNavigatedFrom` best-effort 删除；删除失败 swallow |

### 6.4 并发 / 竞态

| 情景 | 机制 |
|---|---|
| 快速切换 SelectedRecord | `_projectionCts` 复位，旧任务 OperationCanceledException 退出 |
| 快速点导出 | `[NotifyCanExecuteChangedFor(nameof(IsBusy))]` + `CanExport()` —— 同一时刻最多一个导出 |
| Refresh 进行中点 Export | `CanExport()` 通过 `IsBusy` 联动锁住 |
| WebView2 `EnsureCoreWebView2Async` 并发 | 内部幂等；首次失败置位 `_webViewInitialized = false`，下次走 fallback |

### 6.5 WebView2 运行时

| 情景 | 处理 |
|---|---|
| Runtime 未安装 | `WebView2RuntimeNotFoundException` → InfoBar + `Process.Start(path, UseShellExecute=true)` fallback |
| Navigate 失败（URI 编码 / 文件锁定） | `CoreWebView2.NavigationCompleted.IsSuccess == false` → InfoBar，不重试 |

### 6.6 启动 / DI

未注册的 renderer / repository / plotter → DI 解析时抛 `InvalidOperationException`，**app 启动即失败**（CLAUDE.md §6 fail-fast，不假装能跑）。

### 6.7 显式 punt（不在本计划处理）

- 跨页消息（`SystemMtExecutionPage` 完成 → push 到结果页）
- 多用户 / 并发数据库写入（LiteDB 单进程 store）
- 报告内中文字体缺失（Linux Phase 3a renderer 责任）
- 导出中途取消（4-end renderer 未接 CancellationToken）

---

## 7. 测试策略

### 7.1 测试金字塔

```
                  ╔══════════════════════════╗
                  ║  Linux CI (ubuntu-24.04) ║   ← 唯一自动化层
                  ║    dotnet build + xUnit  ║
                  ╚══════════════════════════╝
                                ↑ 仅守住 Core/BLL 编译 + 0 regress
                  ╔══════════════════════════╗
                  ║   Windows VM 手动 verify ║   ← 真功能验证
                  ║   (无 UIAutomation v1)   ║
                  ╚══════════════════════════╝
```

### 7.2 Linux CI 自动化（每 PR 必须）

| 检查 | 守住的不变式 |
|---|---|
| `dotnet build MetBench_BLL.Core/MetBench_BLL.Core.csproj` 0 errors | WPF 改动未泄漏进 Core |
| `dotnet build MetBench_BLL/MetBench_BLL.csproj` 0 errors | 同上对 BLL |
| `dotnet test MetBench_SystemMT.Tests` 0 failed | Linux Phase 1–3c 合后基线零 regression |
| `git diff main...HEAD -- 'MetBench_BLL.Core/**' 'MetBench_BLL/**'` 空 | Windows-only PR 不动 cross-platform 项目（PR Gate Checklist §Scope 自查） |

Hard `test` gate 绿 ≠ WPF 改动正确，仅代表 cross-platform 未被破坏。

### 7.3 Windows VM 手动验证 checklist

**PR-W1 必跑**：
- [ ] `dotnet build MetBench_Client/MetBench_Client.csproj` 0 error 0 warning
- [ ] `dotnet run --project MetBench_Client` 启动，导航菜单显示 "SystemMT 结果"
- [ ] Repo 空 → 空态正确；4 export 按钮 disabled
- [ ] Repo 非空 → DataGrid 列出所有记录；默认选首行；右侧 Binary scatter 显示
- [ ] ViewMode radio：Phase / Historical 在数据形态不支持时灰掉、可用时切换图表
- [ ] 连点 5 行不同 record → 不闪烁、不抛异常、最终图表与最后行匹配
- [ ] 视觉：title / 坐标轴 label / 数值格式 `InvariantCulture` 正确

**PR-W2 必跑**：
- [ ] 4 个 export 按钮各点一次，PDF / HTML 走 WebView2 内嵌预览，Word / Excel 走 `Process.Start`
- [ ] SaveFileDialog Cancel → 无文件、`StatusMessage = "已取消"`、IsBusy 复位
- [ ] 文件名含中文 / 空格 / 全角 → 存盘 + 预览均成功
- [ ] 只读目录 → 红色 InfoBar `导出失败：{msg}`，不 crash
- [ ] 模拟 WebView2 Runtime 未装 → fallback `Process.Start` 触发
- [ ] 并发：PDF 生成中其他 export 按钮 disabled
- [ ] 同一组 records 连续生成两次 PDF → 文件大小一致 ± 50 字节（PDF `/CreationDate` 容差）

**两 PR 都跑**：关闭应用、删除 `SystemMT.Litedb`、重启 → 空态降级正确，无 NullRef。

### 7.4 不写 cross-platform 单元测试的显式决定

| 候选方案 | 决定 | 原因 |
|---|---|---|
| plotter 搬到 `MetBench_BLL` 跑 CI 单测 | **不做** | 与 legacy `MetBench_Client/Services/Plotting/` 同位约定冲突，违 §0.5 |
| 抽 helper（label formatter 等）到 BLL.Core | **不做** | YAGNI；都是 `string.Format(InvariantCulture, ...)` 级别 |
| ViewModel mockable 单测 | **不做** | `[ObservableProperty]` 在 `net8.0-windows7.0` SDK 展开；建影子测试项目 < 收益 |
| UIAutomation smoke | **不做（v1）** | 与 T1 Windows VM 计划 §8 Optional 一致 |

显式决定写入 PR body §Tests 节，避免下次有人误以为忘了。

---

## 8. PR 切分 & 排序

### 8.1 依赖矩阵

| WPF 阶段 | 硬依赖（必须 Linux 已合）| 软依赖（可并行）|
|---|---|---|
| **PR-W1** plotters + Result Page | Linux **Phase 1**（PR-T2-1: ChartFigure DTO + 3 projectors） | Linux Phase 2/3a/3b/3c 可平行 |
| **PR-W2** 4-end export + WebView2 | Linux **Phase 3a + 3b + 3c** 全部合 | — |
| **PR-W3** docs ledger refresh | PR-W1 + PR-W2 都合 | — |

HTML renderer（`ISystemMtResultReportRenderer`）已在 PR #126/128 落地，不构成新依赖。

### 8.2 PR 范围

#### PR-W1 — `feat(wpf): SystemMT result viewer page + LiveCharts plotters`

文件清单：
- `MetBench_Client/Services/Plotting/SystemMt/ISystemMtChartPlotter.cs` [新]
- `MetBench_Client/Services/Plotting/SystemMt/SystemMtChartBinding.cs` [新]
- `MetBench_Client/Services/Plotting/SystemMt/SystemMtChartPlotterFactory.cs` [新]
- `MetBench_Client/Services/Plotting/SystemMt/BinaryRunPlotter.cs` [新]
- `MetBench_Client/Services/Plotting/SystemMt/PhaseConvergencePlotter.cs` [新]
- `MetBench_Client/Services/Plotting/SystemMt/HistoricalTrendPlotter.cs` [新]
- `MetBench_Client/Views/Pages/SystemMtResultPage.xaml(+.xaml.cs)` [新] —— 不含 export 按钮
- `MetBench_Client/ViewModels/SystemMtResultViewModel.cs` [新] —— 不含 export commands
- `MetBench_Client/App.xaml.cs` [改] —— DI 注册 plotter × 3 + factory + page + ViewModel
- `MetBench_Client/ViewModels/MainWindowViewModel.cs` [改] —— nav menu 加 "SystemMT 结果"

估算 6 新 + 2 改 ≈ 700-900 行。验收：§7.3 PR-W1 checklist 全部 ✅。

#### PR-W2 — `feat(wpf): SystemMT 4-end report export + WebView2 preview`

文件清单：
- `MetBench_Client/Views/Pages/SystemMtResultPage.xaml` [改] —— 加 export 按钮行 + WebView2 区
- `MetBench_Client/ViewModels/SystemMtResultViewModel.cs` [改] —— 加 4 RelayCommand + IsBusy / StatusMessage / PreviewUri + Uri 构造 + Process.Start fallback
- `MetBench_Client/App.xaml.cs` [改] —— DI 注册 4 个 renderer（若 Linux Phase 3a/3b/3c PR 未含 WPF 注册指引）
- 可选：`MetBench_Client/Services/FileExport/SystemMtExportService.cs` [新] —— 仅当 4 命令共享逻辑达到 §0.5 抽取阈值

估算 3 改（可能 + 1 新）≈ 400-600 行。验收：§7.3 PR-W2 checklist 全部 ✅。

#### PR-W3 — `docs(status): refresh ledger after T2 WPF complement (PR-W1 + PR-W2)`

文件清单：
- `docs/status/current.md` —— `Latest code-test baseline commit` 推进到 PR-W2 合并 SHA；Stage 8 行加 "T2 SystemMT 可视化 WPF 端" → Controlled
- `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md` —— 本计划标 Completed，附 3 SHA
- 本 spec frontmatter `Status` → `Delivered`

零代码改。Hard `test` gate 自动绿（docs-only PR）；soft `review` 按 `paths-ignore`（PR #175）自动 skip。

### 8.3 排序时间线

```
Time →                                                    (today main = 2911af2)
─────────────────────────────────────────────────────────────────────────────────►
Linux:  PR-T2-1 ─→ PR-T2-2 ─→ PR-T2-3a ─┐
                                          ├─→ Linux Phase 4 / 5 / 6 (独立轨)
                                  PR-T2-3b ┤
                                  PR-T2-3c ┘

WPF:                  PR-W1 ←─(deps on T2-1)
                              \
                               \   (W1 与 T2-2/3a/3b/3c 并行)
                                ─→ PR-W2 ←─(deps on 3a+3b+3c 全合)
                                              \
                                               └→ PR-W3 (docs)
```

PR-W1 可在 PR-T2-1 合后立即开分支，**不等** Linux Phase 2-3 落地。

### 8.4 Cloud / VM 轨道分工

| PR | 轨道 | 由谁开 |
|---|---|---|
| Linux Phase 1–6 | Cloud track（Claude Code Web） | Cloud agent |
| PR-W1 / W2 | **VM track（Windows + VS 2022）** | VM agent / 人工 |
| PR-W3（docs-only） | Cloud 或 VM 皆可 | — |

理由：CLAUDE.md §9 "Cloud agents must not modify *.xaml* files in `MetBench_Client/` without explicit user direction"，Linux SDK 不能编 WPF。

### 8.5 Ledger 独立账本

本计划与 Linux T2 计划**各自独立结账**：

- Linux T2 Phase 6 ledger → 标 Stage 8 "T2 数据层" Controlled
- 本计划 PR-W3 ledger → 标 Stage 8 "T2 WPF UI 层" Controlled

不合并，因为节奏与失败模式不同。

### 8.6 附带：Linux T2 计划 companion 指针（可选附带）

建议在 Linux T2 计划开头加一行 "Companion plan: <本 spec 路径>"。PR-W1 开包附带的小修订，不单独成 PR；如严守 §0.5 不附带也可，留 PR-W3 一起处理。

---

## 9. 风险与缓解

| ID | 风险 | 缓解 |
|---|---|---|
| **R-WPF-1** | Linux T2 Phase 1 未合就开 PR-W1 → WPF 编译失败 | PR-W1 触发条件明文 "本地 `git pull` 后 `MetBench_BLL.Core/SystemMT/Reporting/Charts/ChartFigure.cs` 等已存在"；CLAUDE.md §12 PR Gate Checklist §Scope 自查 |
| **R-WPF-2** | `LiveChartsCore.SkiaSharpView.WPF` 2.0.0-rc5.4 是 RC 版，API 可能漂移 | 已在仓库使用 3 处页面（`MTReportGeneratorPage.xaml:12` / `MTExecutionPage.xaml:11` / `CoverageDashboardPage.xaml:5`），版本不动；PR-W1/W2 不升级该包 |
| **R-WPF-3** | WebView2 在某些 Windows 版本未预装 | §6.5 fallback `Process.Start(UseShellExecute=true)`；不阻塞功能 |
| **R-WPF-4** | PDF 渲染时间长（多 record）→ UI 看起来卡死 | `IsBusy` ObservableProperty + 转圈 InfoBar；4 export 按钮联动 disabled；不假装能 cancel 中途（CLAUDE.md §6 显式说明） |
| **R-WPF-5** | 同一 SHA 两份 ledger 切状态歧义 | §8.5 明确两份独立账本，分别标 "数据层 Controlled" / "WPF UI 层 Controlled" |
| **R-WPF-6** | Linux Phase 5 gap-fill MR 改变 `SystemMtResultRecord` schema → WPF 端 binding 失效 | Linux Phase 5 验收已守 "无 SystemMtResultRecord schema 改动"（PR-Bol-2B / PR-N2 先例）；若未来真改 schema，按 CLAUDE.md §6 系统-MT facade 类型泄漏规则处理 |

---

## 10. 全局验收

PR-W1 + PR-W2 + PR-W3 全部合后：

- [ ] WPF 应用启动 → 导航菜单可见 "SystemMT 结果"；点击进入页面无 crash。
- [ ] 加载 ≥ 1 条 SystemMT 运行记录的 repo → DataGrid 显示、首行自动选中、右侧图表渲染 Binary scatter。
- [ ] 切换 ViewMode 至 Phase（前提：record 有 PhaseMetrics）→ 折线图正确显示 phase 收敛轨迹。
- [ ] 切换至 Historical（前提：同 MrId 至少 2 条）→ 时间趋势图正确显示。
- [ ] 4 个 export 按钮每个均产出可被对应应用打开的文件（PDF / Word / Excel / HTML）。
- [ ] PDF / HTML 在 WebView2 内嵌预览成功；Word / Excel 通过 `Process.Start` 由系统默认应用打开。
- [ ] Cross-platform 不变式：`git diff` between PR-W1/W2 base 与 head **零** `MetBench_BLL.Core/` 或 `MetBench_BLL/` 改动。
- [ ] Linux CI 在每个 PR 上的 ≥ 1445 facts 全绿、零数量变化。
- [ ] `docs/status/current.md` 显示 Stage 8 "T2 SystemMT 可视化 WPF 端" → Controlled。
- [ ] 本 spec 的 `Status` 已更新为 `Delivered`。

---

## 11. 后续可能的扩展（明文 punt）

- T1 Windows VM 计划（MR CRUD manifest editor）—— 独立计划，本 spec 不依赖也不影响。
- Phase 4 `MetaPatternMatrixAuditor` 绑定到既有 `CoverageDashboardPage` —— 单独评估，不挤本计划。
- 跨页 event bus 让 `SystemMtExecutionPage` 跑完直接 push 结果到本页 —— 单独评估。
- `tools/smokeshot` 自动截图 visual regression 扩展到 SystemMT 页面 —— 单独评估。
- 性能基准（大数据集渲染时长）、i18n、跨 Windows 版本测试 —— 待真实需求驱动。

---

*Spec 完成 —— 待用户 review，approval 后进入 `superpowers:writing-plans` 产出实施计划。*
