# UAT Round-1 UI Gap Backlog

> **创建**: 2026-05-21  
> **来源**: Windows UAT round-1 (limeng 2026-05-18) 发现的 runbook↔UI 偏差  
> **处理策略**: polish 批次只做 doc-fix；UI 功能缺口单独落 backlog，下个 sprint 决定是否补实现

---

## 功能缺口列表

| # | UC | Gap 描述 | 优先级 | 备注 |
|---|---|---|---|---|
| G-1 | UC-A4 | DomainManagementPage 缺 "Bound Applications" 多选框，无法在 Domain 表单内绑定 Application | P2 | round-1 跑到此步只能填 Name + Description；MR 表单的 ApplicationName 多选是在 MRManagementPage，Domain 级别绑定未实现 |
| G-2 | UC-A4 | DomainManagementPage 表单标签 "Desciption" 拼写错误（少 'r'，应为 "Description"） | P3 | 纯 UI 文本 typo，`DomainManagementPage.xaml:115` |
| G-3 | UC-E4 | MTReportGeneratorPage 缺 "View HTML in App" 按钮（WebView2 内嵌 HTML 报告入口） | P2 | HtmlSystemMtResultReportRenderer 已实现生成，但 WPF 侧展示入口未接线；SystemMtExecutionPage 已有 WebView2 接线可参考 |
| G-4 | UC-E5 | WPF 左导航缺 "Dashboard 主页" 入口（含 Total MRs / Executions Today / Anomalies This Week / Pass Rate card 组件） | P2 | DashboardPage.xaml 存在但未注册到导航；round-1 实测主页打开后直接显示 MR Display |
| G-5 | UC-E3 | MTReportGeneratorPage 缺 "Generate All"（一次导出 4 端）按钮 + scope 下拉（By MR / By Domain / …） | P3 | 现有 UI 需逐端手动 Export；scope 过滤未暴露 |

---

## 判定说明

- **G-1** 属于 Domain-Application 关联设计问题，需先确认 Domain-Application 关联是否在 v2 数据模型中存在（`MetBench_Domain` 是否有 `DomainApplication` join entity），否则 UI 加多选框也无存储支撑。
- **G-3** `HtmlSystemMtResultReportRenderer` 已经 TDD 通过；WPF 侧只需在 `SystemMtExecutionPage` 或新建 HTML 报告预览页面接 `ISystemMtResultReportRenderer.RenderAsync`，`WebView2.NavigateToString(html)` 即可。
- **G-4** `DashboardPage.xaml` 已存在；需确认 ViewModel 的 card 数据源（可能依赖 `AnomalyService` / `ISystemMtResultRepository` 聚合）是否已实现。
- **G-2 / G-5** 较小，可在任意 polish sprint 快速修。

---

## 不在本 backlog 范围

- A2 / A5 / B1 的 bug fix（已在 round-1 / round-2 修复，PR #71/#72/#75）
- UC-B7 data 接线 gap（已在 round-2 inline 修复，PR #77）
- 原 Item 1/2（Anomaly severity 分级 / category 拆分）——调研前提不成立，已从 polish 批次移除（见 `2026-05-19-polish-existing-work-plan.md §0`）
