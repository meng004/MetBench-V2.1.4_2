# Plan — 完善已有工作（v2.1.x polish 批次）

> **批次**: v2.1.x 收尾 polish（4 项技术债）
> **创建**: 2026-05-19 ｜ **修订**: 2026-05-21
> **状态**: 已交付 —— Item 3/4 见 PR #80 / #81；Item 1/2 见本 PR
> **关联**: round-1 [`results-summary.md`](../../uat/reports/round-1-limeng-2026-05-18/results-summary.md) · runbook [`windows-uat-round-1.md`](../../uat/runbooks/windows-uat-round-1.md)

---

## 0. 修订记录

本计划一度把 Item 1/2 误删。原因：核实时在过时的开发分支
（落后 `main` 7 个 commit、缺 PR #75）上 `grep`，错判
`RecordAnomalyIfFailedAsync` / `TODO(stage-7-followup)` 不存在。实际上 PR #75
（UC-B7，已合入 `main`）明确把 severity / category 硬编码并留 `TODO` 待本批次处理。
2026-05-21 已订正：4 项全部为真实技术债，Item 1/2 恢复并在本 PR 落地。

---

## 1. 四项 & 状态

| # | 项 | Track | 状态 |
|---|---|---|---|
| 1 | Anomaly severity 分级 | Cloud | ✅ 本 PR |
| 2 | Anomaly category 拆分（新增 `runner-failure`） | Cloud | ✅ 本 PR |
| 3 | HandyControl 移除 | VM | ✅ PR #80 |
| 4 | UAT runbook ↔ UI 对齐 | VM | ✅ PR #81 |

---

## 2. 设计决策

- **DP-1 — Anomaly 加第 6 个 category `runner-failure`** ✅ 已采纳。runner 进程崩溃与
  「MR 数学被违反」是两类信号，混入 `single-point` 会污染 commonality 分析。
- **DP-2 — runbook 偏差只做 doc-fix** ✅ 已采纳（PR #81）。runbook 改到与实际 v2.1
  WPF UI 一致；E3/E4/E5 + A4 的真功能缺口单列 backlog（`2026-05-21-uat-ui-gaps-backlog.md`）。
- **DP-3 — severity 阈值是系统配置参数** ✅ 已采纳。端点 `{1,10,50}` 收敛到一个
  `AnomalySeverityThresholds` record；WPF 侧可经 `appsettings.json` 覆盖、不重编译。
  BLL.Core 不读配置，仅持有 record，未注入时回退 `.Default`。

---

## 3. 关键改动

### Item 1 + 2 — Anomaly 分类（Cloud，本 PR）

新增 `MetBench_BLL.Core/SystemMT/Anomaly/`：

- **`AnomalySeverityThresholds.cs`** —— record，端点 `NoiseMaxPercent=1` /
  `MinorMaxPercent=10` / `MajorMaxPercent=50`，附 `.Default`。severity 是 `|Δk%|` 的
  分段函数，半开区间 `[0,1)=noise / [1,10)=minor / [10,50)=major / [50,∞)=critical`。
- **`AnomalyClassifier.cs`** —— 静态：
  - `ClassifySeverity(SystemMtResult, AnomalySeverityThresholds)`：runner 崩溃
    （`SourceRun`/`FollowUpRun` 任一 `!Succeeded`）→ `critical`；`SourceValue≈0`
    （<1e-12）无法算相对变化 → `critical`；否则按区间落档。
  - `ClassifyCategory(SystemMtResult)`：runner 崩溃 → `runner-failure`，否则 → `single-point`。

改 `SystemMtMrLauncher`：

- 构造函数新增可选第 4 参 `AnomalySeverityThresholds? severityThresholds = null`，
  未注入回退 `.Default`（既有 3 参调用方 / 测试 / DI 均不破）。
- `RecordAnomalyIfFailedAsync` 删 `TODO(stage-7-followup)`，severity / category
  改调 `AnomalyClassifier`。

改 `MetBench_Domain/V2/Anomaly.cs`：Category 文档注释加 `runner-failure` 条目。

测试：新增 `AnomalyClassifierTests.cs`（25 个 case —— severity 区间内取值 + 半开
边界 + Source≈0 + runner 崩溃 + 自定义阈值 + null 守卫；category 两路）。既有
`AnomalyCreationOnFailureTests` 无需改（其 fixture 仍分类为 `minor`/`single-point`）。

**待续（VM 侧，DP-3 配置绑定）**：`MetBench_Client/appsettings.json` 加
`"AnomalySeverity"` 段 + `App.xaml.cs` 绑定为 `AnomalySeverityThresholds` singleton
注入 launcher。因 launcher 有 `.Default` 回退，此项无硬性顺序依赖，可随时接入。

### Item 3 — HandyControl 移除（VM，PR #80 已交付）

8 个文件去 HandyControl：6 页 `hc:Pagination` 换自建 `SimplePagination` UserControl，
`App.xaml` 去 HC 主题合并，`MainWindow.xaml` 去死 `xmlns:hc`，移除 `HandyControl` NuGet 包。

### Item 4 — runbook 对齐（VM，PR #81 已交付）

`windows-uat-round-1.md` 按 round-1 偏差改到与实际 v2.1 WPF UI 一致；5 个 UI 功能
缺口落档 `docs/superpowers/plans/2026-05-21-uat-ui-gaps-backlog.md`。

---

## 4. 验证（本 PR / Item 1+2）

- `dotnet build MetBench_BLL.Core` → 0 error。
- `dotnet test MetBench_SystemMT.Tests` 全套 → 561 passed / 2 skipped / 0 failed。
- 焦点回归：`AnomalyClassifier` + `AnomalyCreationOnFailure` + `SystemMtMrLauncher`
  + `AnomalyService` → 66/66 passed。

## 5. 完成状态

- `grep -rn "TODO(stage-7-followup)"` → 0 命中
- `grep -rn "HandyControl\|hc:" MetBench_Client/` → 0 命中（PR #80）
- severity 端点集中在 `AnomalySeverityThresholds`，`appsettings.json` 可改不重编译
- runbook 与 v2.1 WPF UI 逐 UC 一致（PR #81）

## 6. 不交付（scope 外）

- Anomaly category 的 `basin` / `mc-floor` / `cross-program` 细分启发式 —— 本批次只做
  crash vs violation 二分；细分留 Stage 8 cross-program 工作。
- E3/E4/E5/A4 的 UI 功能缺口 —— 见 backlog 文档，下个 sprint 判定。
- F11 m_adj / 第 5 SUT —— 外部依赖未解。
