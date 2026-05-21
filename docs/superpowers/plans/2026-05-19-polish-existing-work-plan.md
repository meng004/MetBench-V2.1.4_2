# Plan — 完善已有工作（v2.1.x polish 批次）

> **批次**: v2.1.x 收尾 polish — Stage 8 启动前的技术债清理
> **日期**: 2026-05-19
> **状态**: 正式实施计划，已 approved（DP-1 / DP-2 已拍板，2026-05-19）
> **关联**: [AGENTS.md Stage 7](../../../AGENTS.md#stage-7-w11-w12-delivered-2026-05-17) · round-1 [`results-summary.md`](../../uat/reports/round-1-limeng-2026-05-18/results-summary.md) · round-2 [`findings.md`](../../uat/reports/round-2-windows-2026-05-19-limeng/findings.md)
> **总工时**: Cloud ~6–7h（1 PR）+ VM ~2–2.5 天（2 PR）

---

## 1. 目标 & 验收标准

清掉 v2.1.x 期间留下的 4 项已知技术债，全部完成后 main 不再有 `TODO(stage-7-followup)` 标记、不再依赖 HandyControl、UAT runbook 与实际 WPF UI 一致。

| # | 项 | 验收标准 | Track |
|---|---|---|---|
| 1 | Anomaly severity 分级 | `RecordAnomalyIfFailedAsync` 不再硬编 `"minor"`；按 \|Δk%\| 分 noise/minor/major/critical；TDD 覆盖 4 档边界 | Cloud |
| 2 | Anomaly category 拆分 | runner 崩溃 → `runner-failure`，MR 反例 → `single-point`（保留）；TDD 覆盖 crash/violation 两路 | Cloud |
| 3 | HandyControl 移除 | 6 个 XAML 文件不再引 `hc:`；`HandyControl` NuGet 包移除；WPF 能 build + 翻页功能可视验证 | VM |
| 4 | runbook ↔ UI 对齐 | round-1 报出的 10 处偏差逐条归类（doc-fix / UI-gap）；runbook §3–5 改到与实际 UI 一致 | VM |

---

## 2. 设计决策（DP-1 / DP-2 已拍板 2026-05-19）

### DP-1 — Anomaly 加第 6 个 category `runner-failure`？ ✅ 已定：加

`MetBench_Domain/V2/Anomaly.cs:17–22` 当前文档化 5 个 category：`basin` / `mc-floor` / `cross-program` / `single-point` / `legacy`。runner 进程崩溃（非 MR 反例）语义上不属于任何一个。

- **已采纳**：加 `runner-failure` 为第 6 个文档化 category。理由：crash 与"MR 数学被违反"是两类完全不同的信号，混进 `single-point` 会污染 commonality 分析。
- ~~备选：crash 归 `single-point` + 用 severity=`critical` 区分。~~ 未采纳。

### DP-2 — runbook 10 处偏差：doc-fix vs UI-fix ✅ 已定：只做 doc-fix

round-1 报出的 10 处 runbook↔UI 偏差，分两类：

| 类型 | UC | 处理 |
|---|---|---|
| **v2.1 重设计的正常结果**（runbook 过时） | A1/A3/A4/B2/B3/B6 | doc-fix：改 runbook |
| **疑似真缺功能**（UI 该有没有） | E3 "Generate All" 按钮 + scope 下拉 / E4 "View HTML in App" / E5 Dashboard 落地页 | 需判定：是 v2.1 砍掉的设计，还是漏实现 |

- **已采纳**：本批次只做 doc-fix（改 runbook 到与实际 UI 一致，~0.5 天）。E3/E4/E5 的 3 个"疑似缺功能"单独立 backlog item，下个 sprint 决定补不补，**不混进 polish 批次**（避免 scope creep）。

---

## 3. 关键改动

### Item 1 + 2 — Anomaly 分类（Cloud，合一个 PR）

新增 `MetBench_BLL.Core/SystemMT/Anomaly/AnomalyClassifier.cs`（静态 helper）：

```csharp
public static class AnomalyClassifier
{
    // |Δk%| 阈值：noise <1, minor 1–10, major 10–50, critical >50
    public static string ClassifySeverity(SystemMtResult result);
    // crash → runner-failure；assertion 违反 → single-point
    public static string ClassifyCategory(SystemMtResult result);
}
```

- `ClassifySeverity`：从 `SystemMtAssertionResult.SourceValue` / `FollowUpValue`（`SystemMtAssertionResult.cs:3–9`）算 `|（FollowUp−Source）/Source|×100`。**Source≈0 守卫**：分母 < 1e-12 时直接判 `critical`（无法算相对变化的异常值）。crash 时也直接 `critical`。
- `ClassifyCategory`：`result.SourceRun.Succeeded == false || result.FollowUpRun.Succeeded == false`（`CliRunResult.Succeeded`）→ `runner-failure`；否则 `single-point`。
- 改 `SystemMtMrLauncher.cs:148–159` 的 `RecordAnomalyIfFailedAsync`：删 `TODO(stage-7-followup)` 注释，severity/category 改调 classifier。
- `Anomaly.cs:17–22` 文档注释加 `runner-failure` 条目（若 DP-1 选推荐方案）。

### Item 3 — HandyControl 移除（VM）

6 个文件全在 `MetBench_Client/Views/Pages/`：`ApplicationManagementPage` / `AutoDetectMRPage` / `DomainManagementPage` / `MRDisplayPage` / `MRManagementPage` / `MRRecommendationPage`。

- 全部用 `hc:Pagination`。`MRDisplayPage.xaml:138–145` 另有**已注释**的 `hc:EventToCommand` → 直接删（dead code，不迁移）。
- `hc:Pagination` 替换：优先 `Wpf.Ui` 自带翻页控件；若 Wpf.Ui 无对应控件，用 `ItemsControl` + 上一页/下一页 `ui:Button`（ViewModel 已有分页命令，绑定现成）。
- `MetBench_Client.csproj`：加 `Microsoft.Xaml.Behaviors.Wpf` PackageReference（当前未引）；6 文件全部去 `hc:` 后移除 `HandyControl 3.5.0`（`.csproj:26`）。

### Item 4 — runbook 对齐（VM）

- 改 `docs/uat/runbooks/windows-uat-round-1.md` §3–5 的 UC 步骤到与实际 v2.1 WPF UI 一致（按 DP-2 推荐，只做 doc-fix 类的 6 个 UC）。
- E3/E4/E5 的 3 个疑似缺功能 → 在 `docs/superpowers/plans/` 新开一个 backlog 条目记录，不在本批次实现。

---

## 4. Phase breakdown

| Phase | 内容 | 工时 | Track |
|---|---|---|---|
| **P1** | `AnomalyClassifier` + TDD（severity 4 档边界 + category crash/violation 两路 + Source≈0 守卫）| ~3h | Cloud |
| **P2** | wire 进 `RecordAnomalyIfFailedAsync` + 删 TODO + `Anomaly.cs` 文档加 `runner-failure` + 既有 `AnomalyCreationOnFailureTests` 参数化 | ~3h | Cloud |
| **P3** | HandyControl 移除：加 Behaviors 包 → 6 文件改 `hc:Pagination` → 删注释 EventToCommand → 移 HandyControl 包 → WPF build + 翻页可视验证 | ~1–1.5 天 | VM |
| **P4** | runbook §3–5 对齐（6 个 doc-fix UC）+ E3/E4/E5 backlog 条目落档 | ~0.5 天 | VM |

P1+P2 是同一个 cloud PR 的两段（TDD 先写 classifier，再 wire）。

---

## 5. PR 切片

| PR | 内容 | Track | CI |
|---|---|---|---|
| **PR-A** | P1+P2 — Anomaly severity + category 分类（`AnomalyClassifier` + wire + tests） | Cloud | ✅ CI 全跑 |
| **PR-B** | P3 — HandyControl 移除 | VM | WPF 不进 CI，VM 本地 build + 可视验证 |
| **PR-C** | P4 — runbook 对齐 + backlog 落档 | VM | 纯文档 |

PR-A 先走（cloud，最快、CI 兜底）。PR-B / PR-C VM 端并行，互不依赖。

---

## 6. 依赖 & 风险

| 项 | 风险 | 缓解 |
|---|---|---|
| Item 1 Source≈0 | 相对变化分母为 0 | classifier 显式守卫 → `critical` |
| Item 2 crash 检测 | `CliRunResult.Succeeded` 语义是否真覆盖所有 crash（超时？非零退出？） | TDD 造 Succeeded=false 的 fixture；超时路径单独 case |
| Item 3 Wpf.Ui 无 Pagination | 替换控件不存在 | fallback `ItemsControl`+按钮；VM 端 1h 内可判定 |
| Item 3 WPF 不进 CI | cloud 无法验证 | VM 端 build + 6 个页面逐个翻页点击验证（P3 验收项） |
| Item 4 DP-2 误判 | 把真缺功能当 doc-fix 改掉 | E3/E4/E5 强制单独 backlog，不在本批次动 UI |

---

## 7. 测试策略

- **PR-A**：TDD 先行 — `AnomalyClassifierTests` 覆盖 severity 4 档边界值（0.9% / 5% / 30% / 80% / Source=0 / crash）+ category 两路（source crash / followup crash / 纯 assertion 违反）。既有 `AnomalyCreationOnFailureTests` 3 个测试参数化适配新签名。回归：cross-program 4 + 全套 `dotnet test` 0 fail。
- **PR-B**：VM 端 build 通过 + 6 个含翻页的页面逐个手动翻页点击 + 截图存证。
- **PR-C**：纯文档，无测试；VM 端对照实际 UI 逐条 review。

---

## 8. 完成时 main 状态

- `grep -rn "TODO(stage-7-followup)"` → 0 命中
- `grep -rn "HandyControl\|hc:" MetBench_Client/` → 0 命中
- `MetBench_Client.csproj` 无 `HandyControl` PackageReference
- runbook §3–5 与 v2.1 WPF UI 逐 UC 一致
- 全套 `dotnet test` 0 fail；新增 ~10 个 classifier 测试

## 9. 不交付（scope 外，明确）

- E3/E4/E5 的 3 个疑似缺 UI 功能 — 单独 backlog，本批次只落档不实现
- Anomaly category 的 `basin` / `mc-floor` / `cross-program` 细分启发式 — 本批次只做 crash vs violation 二分；细分留 Stage 8 cross-program 工作时一并做
- F11 m_adj / 第 5 SUT — 外部依赖未解，不在 polish 范围
