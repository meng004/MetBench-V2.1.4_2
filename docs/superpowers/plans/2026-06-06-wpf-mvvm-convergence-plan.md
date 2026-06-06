# WPF MVVM Convergence Plan（2026-06-06）

> **状态：Planned。** 从成熟度评估的"5 套 MVVM 框架并存"派生的独立 follow-up（**不在**成熟度修复计划主干内）。
> 主体目标：让 WPF 实际只用 1 套 MVVM 机制（CommunityToolkit.Mvvm），把死引用、幽灵 weave、legacy XAML action 路径逐步收敛。
> **REQUIRED SUB-SKILL**：superpowers:executing-plans，逐 PR TDD-first，cloud/VM 分工严格。

## 0. 据实修正评估（先承认夸大）

成熟度评估文档说"5 套 MVVM 框架并存"。**实测核对后这是夸大**。`main` @ `944718c` 上的真实分布：

| 机制 | csproj | 实测用途 | 实际计数 | 处置 |
|---|---|---|---|---|
| **CommunityToolkit.Mvvm** | ✅ | 主力：`[ObservableProperty]` `[RelayCommand]` `ObservableObject` | 19 + 13 + 27 文件 | 保留为唯一 MVVM |
| **Stylet.Start** | ✅ | 仅用 `s:View.ActionTarget` + `s:Action` 路由 XAML 控件事件到 VM 方法；**无 Bootstrapper** | 9 个 legacy XAML | PR-3 替换为 `Microsoft.Xaml.Behaviors` |
| **PropertyChanged.Fody** | ✅ + `FodyWeavers.xml` 启用 `<PropertyChanged />` | 全局 IL weave 给所有类静默叠 INotify 实现；**0 文件显式标注** | 0 显式 / 全局隐式 | PR-2 移除（VM 验证） |
| **Prism.Wpf** | ✅ | 仅 2 文件 `using Prism.Common;`；**无任何具体 Prism 类型引用** | 2 个死 using | PR-1 立删（云端） |
| 手写 `INotifyPropertyChanged` / 手动 `OnPropertyChanged(...)` | — | 老 ViewModel 自实现 | 6 文件 | PR-4 渐进迁移到 ObservableObject |
| **HandyControl** | — | 已彻底移除（CLAUDE.md §4 已对齐） | 0 | — |
| **Microsoft.Xaml.Behaviors** | — | 评估说"新代码用"，实测**尚未引入** | 0 | PR-3 引入 |
| **Wpf.Ui** | ✅ | 控件 + 导航；**不是 MVVM 框架** | — | 保留 |

→ 真相：**3 套有效 + 1 套死引用 + 1 套幽灵 weave + 6 个手写遗留**，不是"5 套"。
PR-0 的 docs 投影会据实把评估那行 "5 套" 改成上面这张表的概要 + 引本计划。

## 1. 目标与验收总纲

- 让 WPF 实际只剩 **CommunityToolkit.Mvvm** 一套 MVVM（外加 `Microsoft.Xaml.Behaviors` 作为 XAML event-binding 工具，行业标准做法）。
- `MetBench_Client.csproj` 不再引 `Prism.Wpf` / `Stylet.Start` / `PropertyChanged.Fody`。
- `FodyWeavers.xml` 删 `<PropertyChanged />`（或整个文件删除）。
- 不引入新机制；不"顺手"重排页面或重命名 VM；CLAUDE.md §0.5 严格限定。
- 行为保持：每个 PR 后 WPF 运行时行为与改前**视觉与功能一致**（VM 截图对比）。

## 2. PR 链（4 个，独立可交付，按风险递增）

### PR-1（云端，docs + csproj 清理）— 删 Prism 死引用 + PR-0 评估投影

| 项 | 内容 |
|---|---|
| Scope | 删 `MetBench_Client.csproj` 的 `Prism.Wpf` PackageReference；删 2 处死 `using Prism.Common;`；据实修评估文档"5 套"为 §0 真实分布表 |
| 验收 | 云端不能编译 WPF，但 `MetBench_Client.csproj` 依赖图变小可读；`grep -rn "using Prism" MetBench_Client/` 为空；VM 端补一次 `dotnet build MetBench.sln` 0 errors 作为 follow-up（评论里附） |
| 风险 | **极低**（grep 已证 0 类型引用） |
| TDD | source-guard 测试 `WpfMvvmConvergenceGuardTests`：断言 `MetBench_Client.csproj` 不含 `"Prism.Wpf"` + `MetBench_Client/Views/Pages/*.cs` 不含 `using Prism` |

### PR-2（VM 提示词为主，docs 与 source-guard 在云端落）— 删 PropertyChanged.Fody

| 项 | 内容 |
|---|---|
| Scope | VM 删 `PropertyChanged.Fody` PackageReference 与 `FodyWeavers.xml` 中 `<PropertyChanged />`；本地编译验证；冒烟测试至少 3 个页面属性绑定（执行历史、MR 管理、Application 管理）；附 source-guard：csproj 不含 Fody，FodyWeavers.xml 不存在或不含 `<PropertyChanged />` |
| 验收 | WPF build 0 errors；CommunityToolkit `[ObservableProperty]` 路径的 PropertyChanged 仍能触发 UI 刷新；手写 `OnPropertyChanged` 的 6 个老 VM 仍工作（属性变化触发 UI 刷新）；如某老 VM 静默依赖 Fody，VM 端在该文件加显式 `: ObservableObject` 或保留手写实现 |
| 风险 | **中**：Fody 全局 weave 不可见，可能某些类靠它隐式拿到 INotify；VM 编译会暴露具体破点 |
| 提示词 | `docs/superpowers/vm-prompts/2026-06-06-wpf-fody-removal-vm-prompt.md`（PR-2 写） |

### PR-3（VM 提示词为主）— 9 个 legacy XAML 的 Stylet `s:Action` → `Microsoft.Xaml.Behaviors`

| 项 | 内容 |
|---|---|
| Scope | 在 csproj 加 `Microsoft.Xaml.Behaviors.Wpf` PackageReference；逐 XAML 把 `xmlns:s="..."` 改为 `xmlns:i="http://schemas.microsoft.com/xaml/behaviors"`；把 `<Button Command="{s:Action methodName}">` 重写为 `[RelayCommand] async Task MethodName()` + `<Button Command="{Binding ViewModel.MethodNameCommand}">`；对真正的"事件 → command"（非 click）用 `<i:Interaction.Triggers><i:EventTrigger><i:InvokeCommandAction />`。**`MTExecutionPage.xaml` 走 Stylet 全 ActionTarget 路径较深**，作为 PR-3a 独立子项 |
| 验收 | 9 个 XAML 不含 `s:Action` 与 `s:View.ActionTarget`；删 `Stylet.Start` 包；source-guard 断言 `grep -rln s:View.ActionTarget MetBench_Client` 为空；VM 端用 UIA 跑每个原 `s:Action` 触发点的截图对比 |
| 风险 | **较大**：每个 XAML 都要改 + 测；method 名称大小写一致性敏感 |
| 提示词 | `docs/superpowers/vm-prompts/2026-06-06-wpf-stylet-to-behaviors-vm-prompt.md`（PR-3 写） |

### PR-4（云端/VM 渐进）— 6 个手写 `INotifyPropertyChanged` 文件迁移到 `ObservableObject`

| 项 | 内容 |
|---|---|
| Scope | 6 文件逐一改 `: ObservableObject`；私有字段 + `[ObservableProperty]` 自动生成属性 + 旧手写 `OnPropertyChanged("X")` 调用全部删除；分批进 PR-4a/4b/4c（每批 2 文件以便复审） |
| 验收 | source-guard 断言 `MetBench_Client/ViewModels` 下手写 `OnPropertyChanged(` 调用计数下降到 0；功能 UI 不变 |
| 风险 | **小**（单文件渐进，触一个测一个） |

## 3. Source-guards（贯穿全 plan）

每个 PR 都加 / 加强 `WpfMvvmConvergenceGuardTests`（云端 source-scan，不编译 WPF）：

| Guard | 断言 | 引入 PR |
|---|---|---|
| no_prism_using | `MetBench_Client/**/*.cs` 不含 `using Prism` | PR-1 |
| csproj_no_prism | `MetBench_Client.csproj` 不含 `Prism.Wpf` PackageReference | PR-1 |
| csproj_no_fody | `MetBench_Client.csproj` 不含 `PropertyChanged.Fody` | PR-2 |
| no_fody_weavers | `MetBench_Client/FodyWeavers.xml` 不存在 或 不含 `<PropertyChanged` | PR-2 |
| csproj_no_stylet | `MetBench_Client.csproj` 不含 `Stylet.Start` | PR-3 |
| no_stylet_actions | `MetBench_Client/Views/**/*.xaml` 不含 `s:View.ActionTarget` 或 `s:Action` | PR-3 |
| no_manual_inotify_in_vm | `MetBench_Client/ViewModels/**/*.cs` 中手写 `OnPropertyChanged(` 调用计数为 0 | PR-4（最后） |

## 4. Cloud / VM 分工

| PR | Cloud | VM |
|---|---|---|
| PR-1 | 全部（docs + csproj 清理 + 加 PR-1 两条 guard） | 跟一次 build 验证（在 PR 评论里附） |
| PR-2 | 写提示词 + PR-2 两条 guard | 执行：删 Fody，编译验证，冒烟测试，截图 |
| PR-3 | 写提示词 + PR-3 两条 guard | 执行：逐 XAML 重写，UIA 对比每条原 action 路径 |
| PR-4 | 单文件机械迁移（小，自包含），最后加 guard | 分批 build 验证 |

## 5. 不交付（明确排除）

- 不引入 IoC 容器替换（Wpf.Ui + Microsoft.Extensions.DependencyInjection 现役不动）。
- 不重排页面或重命名 VM。
- 不动 Wpf.Ui / Microsoft.Extensions.Hosting。
- 不动 BLL.Core / BLL / DAL / Domain / IDAL 的任何代码。
- 任一 PR 失败/超时即**回滚到前一个 PR 的边界**；不混改。

## 6. Self-Review

- 所有"是不是有 N 套"都由 grep 实测，不引用评估的转述（CLAUDE.md §0/§0.5/§6）。
- 每个 PR 都有 source-guard 把成果锁住（防回潮）。
- CLAUDE.md §9：WPF 不在云端编译，按 cloud/VM 分工拆。
- 与成熟度修复计划主干不重叠：MVVM 收敛不是 P3/P4 的延伸，是独立维度。
