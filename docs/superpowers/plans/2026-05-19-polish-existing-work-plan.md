# Plan — 完善已有工作（v2.1.x polish 批次）

> **批次**: v2.1.x 收尾 polish
> **创建**: 2026-05-19 ｜ **修订**: 2026-05-21（原 Item 1/2 前提不成立，已移除 —— 见 §0）
> **状态**: 正式实施计划，已 approved
> **关联**: round-1 [`results-summary.md`](../../uat/reports/round-1-limeng-2026-05-18/results-summary.md) · runbook [`windows-uat-round-1.md`](../../uat/runbooks/windows-uat-round-1.md)
> **总工时**: VM ~2–3 天（2 PR）｜ Cloud 无可执行项

---

## 0. 修订说明 —— 为什么砍掉原 Item 1/2

原计划含 4 项，其中 Item 1（Anomaly severity 分级）、Item 2（category 拆分）建立在一份**不准确的代码调研**上。落地前核实实际代码，发现前提不成立：

- `SystemMtMrLauncher` **不存在** `RecordAnomalyIfFailedAsync` 方法；全仓 `grep` **无** `TODO(stage-7-followup)` 标记。
- 系统 MT 跑挂时**根本不自动创建 `Anomaly`** —— `SystemMtMrLauncher.RunAsync` 失败时只存一条 `SystemMtResultRecord` 即返回（这点 round-1 UC-B7 也实测到："System MT 失败 run 未写入 Anomalies 表"）。
- 全仓**唯一**创建 `Anomaly` 的地方是 `RCaseReproductionService.cs:155–170`（R-Case 复现已知 bug）。该处 `Severity = "major"` 虽硬编码，但语境是"复现一个已确认的 KnownBug"，判 major 属合理设计；`Category` 是 `$"r-case-{KnownBugCode}"`，已动态派生（不是调研声称的硬编 `"single-point"`）。

结论：原 Item 1/2 所指的技术债**不存在**，移除。原 DP-1（新增 `runner-failure` 类别）、DP-3（severity 阈值配置）随之作废。本批次只保留 Item 3 + Item 4，两项均为 VM-track 工作。

> 若未来确实需要"MT 跑挂 → 自动建 Anomaly + 分级"（round-1 UC-B7 暴露的 data 接线 gap），那是一项**净新增 feature**，应另开 RFC 设计（落在 launcher / `AnomalyService` / pipeline 哪一层需先定），不属于 polish 批次。

---

## 1. 目标 & 验收标准

| # | 项 | 验收标准 | Track |
|---|---|---|---|
| 3 | HandyControl 移除 | 8 个 XAML 不再引 HandyControl（`hc:` 控件 + `App.xaml` 主题合并）；`HandyControl` NuGet 包移除；WPF 能 build；6 个翻页页面功能可视验证无回归 | VM |
| 4 | runbook ↔ UI 对齐 | round-1 报出的 runbook↔UI 偏差逐条归类；`windows-uat-round-1.md` 改到与实际 v2.1 WPF UI 一致；真功能缺口单独落 backlog | VM |

**Cloud 说明**：原 Item 1/2 移除后，本批次无 `MetBench_BLL.Core` 改动 —— Cloud 会话无可执行项，本计划是向 VM track 的交接文档。

---

## 2. 设计决策

### DP — runbook 偏差：doc-fix vs UI-fix ✅ 已定：只做 doc-fix

round-1 报出的 runbook↔UI 偏差分两类：

| 类型 | UC | 处理 |
|---|---|---|
| **v2.1 重设计 / schema 变更**（runbook 过时，UI 正确） | A1 / A3 / A5 / B2 / B3 / B6 | doc-fix：改 runbook |
| **UI 缺 runbook 描述的东西**（是砍掉的设计还是漏实现，待判定） | A4 "Bound Applications" / E3 "Generate All"+scope 下拉 / E4 "View HTML in App" / E5 "Dashboard 主页" | runbook 改成与实际 UI 一致（标 N/A 或改步骤）；真功能缺口**单独立 backlog**，不在本批次实现 |

- **已采纳**：本批次只做 doc-fix。E3/E4/E5（+ A4 多选框）的功能缺口下个 sprint 决定补不补，**不混进 polish 批次**（避免 scope creep）。
- A2 / A5 / B1 是 round-1 定位的**真实代码 bug**（`UpdateService` 自身排除、`ApplicationEx` 缺 `ToString`）—— 属独立 bug-fix track，不在本计划范围。

---

## 3. 关键改动

### Item 3 — HandyControl 移除（VM）

全仓 **8 个文件**引用 HandyControl（调研原称 6 个，漏了 `App.xaml` 与 `MainWindow.xaml`）：

| 文件 | HandyControl 触点 | 处理 |
|---|---|---|
| `App.xaml` L15–16 | 2× `ResourceDictionary` 合并 HC 主题（`SkinDefault.xaml` / `Theme.xaml`） | 删两条 merge；**先验证** Wpf.Ui 主题已覆盖所需画刷（styling 回归风险点） |
| `Views/Windows/MainWindow.xaml` L11 | 仅 `xmlns:hc` 声明，**body 无使用** | 直接删 xmlns 行 |
| `Views/Pages/AutoDetectMRPage.xaml` L12, 100–101 | `xmlns:hc` + `<hc:Pagination>`（带 Stylet `s:View.ActionTarget` + `PageUpdated="{s:Action reload_ItemsSource}"`） | 换翻页控件 + 重接 `PageUpdated` 事件 |
| `Views/Pages/MRDisplayPage.xaml` L10, 138–145 | `xmlns:hc` + `<hc:Pagination>` + **已注释**的 `hc:Interaction.Triggers`/`hc:EventToCommand` | 换翻页控件；注释块直接删（dead code） |
| `Views/Pages/DomainManagementPage.xaml` L9, 81–83 | `xmlns:hc` + `<hc:Pagination>` | 换翻页控件 |
| `Views/Pages/MRRecommendationPage.xaml` L12, 140–143 | `xmlns:hc` + `<hc:Pagination>` | 换翻页控件 |
| `Views/Pages/MRManagementPage.xaml` L12, 165–167 | `xmlns:hc` + `<hc:Pagination>` | 换翻页控件 |
| `Views/Pages/ApplicationManagementPage.xaml` L11, 112–113 | `xmlns:hc` + `<hc:Pagination>`（带 Stylet `s:View.ActionTarget` + `PageUpdated="{s:Action reload_ItemsSource}"`） | 换翻页控件 + 重接 `PageUpdated` 事件 |

要点：
- **唯一在用的 HC 控件是 `hc:Pagination`**（`hc:EventToCommand` 仅以注释存在）。`hc:Pagination` 绑定 `MaxPageCount` / `PageIndex` / `DataCountPerPage` / `MaxPageInterval` / `IsJumpEnabled` + `PageUpdated` 事件，对应 ViewModel 属性已存在。
- **翻页控件替换策略**（VM 端评估，~1h 内可定）：优先 `Wpf.Ui` 自带翻页控件；若无，写一个轻量 `Pagination` UserControl 或 `ItemsControl` + 上/下页 `ui:Button`，复用现成 VM 属性。
- **`PageUpdated` 事件重接**：现用 Stylet `{s:Action reload_ItemsSource}`。替换后改为 code-behind 事件处理，或加 `Microsoft.Xaml.Behaviors.Wpf`（当前未引）用 `EventTrigger` + `InvokeCommandAction` 绑 VM 命令。
- `MetBench_Client.csproj:26` 的 `HandyControl 3.5.0` PackageReference —— **8 个文件全部去 HC 后**才移除。

### Item 4 — runbook 对齐（VM）

按 §2 DP（只做 doc-fix），改 `docs/uat/runbooks/windows-uat-round-1.md`：

| UC | runbook 现状 | 改成 |
|---|---|---|
| A1 | 缺 `SoftwareUnderTest` 描述 | 补：v2.1 表单 `SoftwareUnderTest` 必填 + 文件上传 |
| A3 | 暗示软删 | 明确：实测为硬删 |
| A5 | 表单字段过时 | 改为实际 schema：`InputPattern`/`OutputPattern`/`Operator`/`Expression` 等 method-level 字段 |
| B2 | 描述 legacy "MT Execution 页" | 改为 "System MT 页" |
| B3 | "Generate Follow-up / Run" 两步 | 改为：System MT 页单步 Run |
| B6 | chart hover tooltip 验证 | 删该步：System MT 页无图表区，结果为表格 |
| A4 / E3 / E4 / E5 | 描述 UI 没有的功能 | runbook 步骤标 N/A 或改到实际 UI；功能缺口 → backlog |

- 新开 backlog 条目（`docs/superpowers/plans/` 下）记录 A4/E3/E4/E5 的功能缺口 + A4 "Desciption" typo，交下个 sprint 判定。
- round-2（`round-2-docker-sut-2026-05-19-limeng-macos`）是 Docker SUT 轮，与 WPF runbook 对齐无关，不引入本项。

---

## 4. Phase breakdown

| Phase | 内容 | 工时 | Track |
|---|---|---|---|
| **P3** | HandyControl 移除：评估翻页替换控件 → 6 页换 `hc:Pagination` + 重接 `PageUpdated` → 删 `MainWindow` 死 xmlns + `MRDisplayPage` 注释块 → 删 `App.xaml` HC 主题合并（验 styling 无回归）→ 移 `csproj` HC 包 → WPF build + 6 页翻页可视验证 | ~1.5–2 天 | VM |
| **P4** | runbook §UC 步骤对齐（6 个 doc-fix UC）+ A4/E3/E4/E5 功能缺口 backlog 落档 | ~0.5–1 天 | VM |

P3 / P4 互不依赖，VM 端可并行。

---

## 5. PR 切片

| PR | 内容 | Track | CI |
|---|---|---|---|
| **PR-1** | P3 — HandyControl 移除 | VM | WPF 不进 CI；VM 本地 build + 可视验证 |
| **PR-2** | P4 — runbook 对齐 + backlog 落档 | VM | 纯文档 |

两个 PR 均 target `main`，VM 端并行，互不依赖。Cloud 无 PR。

---

## 6. 风险 & 缓解

| 项 | 风险 | 缓解 |
|---|---|---|
| Item 3 `App.xaml` 主题合并 | 删 HC 主题 merge 后画刷丢失、UI 串样式 | 删前逐一确认所用画刷由 Wpf.Ui 主题提供；改完 6 页 + 主窗逐屏对比截图 |
| Item 3 翻页替换 | `Wpf.Ui` 可能无 Pagination 控件 | fallback：轻量 UserControl 或 `ItemsControl`+按钮，复用现成 VM 属性；VM 端先 1h 评估 |
| Item 3 `PageUpdated` 重接 | Stylet `{s:Action}` 换掉后翻页不刷新数据 | 6 页逐个翻页点击验证 reload 生效（P3 验收项） |
| Item 3 WPF 不进 CI | Cloud 无法编译验证 | 全程 VM 端 build + 可视验证；本计划不含 Cloud 侧改动 |
| Item 4 doc-fix 误判 | 把真功能缺口当 runbook 笔误改掉 | A4/E3/E4/E5 强制单独 backlog，不在本批次动 UI |

---

## 7. 验证

- **PR-1**：VM 端 `dotnet build MetBench_Client` 通过 + `grep -rn "HandyControl\|hc:" MetBench_Client/` 0 命中 + 6 个翻页页面逐个手动翻页点击（确认 reload 生效）+ 主窗与 6 页逐屏截图比对无 styling 回归。
- **PR-2**：纯文档；VM 端对照实际 v2.1 WPF UI 逐 UC review runbook。

---

## 8. 完成时状态

- `grep -rn "HandyControl\|hc:" MetBench_Client/` → 0 命中
- `MetBench_Client.csproj` 无 `HandyControl` PackageReference
- WPF 仍能 build + 6 页翻页功能正常 + 无 styling 回归
- `windows-uat-round-1.md` 与 v2.1 WPF UI 逐 UC 一致
- A4/E3/E4/E5 功能缺口已落 backlog 条目

## 9. 不交付（scope 外，明确）

- 原 Item 1/2（Anomaly severity 分级 / category 拆分）—— 前提不成立，已移除（§0）
- "MT 跑挂 → 自动建 Anomaly" 新功能（round-1 UC-B7 的 data 接线 gap）—— 另开 RFC，非 polish
- A4/E3/E4/E5 的 UI 功能缺口 —— 本批次只落 backlog 不实现
- A2/A5/B1 真实代码 bug —— 独立 bug-fix track
- F11 m_adj / 第 5 SUT —— 外部依赖未解，不在范围
