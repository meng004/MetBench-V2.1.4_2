# SP3b UAT WPF-UI 验收 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans / subagent-driven-development. Steps use checkbox (`- [ ]`) tracking.

**状态**: 部分完成（工具/引擎完备并稳健；核心流程已真验：A1 创建、A7 元模式、E2 覆盖、B2-B5 System-MT 端到端真跑通过；其余数据依赖页渲染但需上游运行；5 项产品/UX 发现已记录）。详见 `docs/superpowers/specs/2026-06-15-sp3b-uat-wpf-ui-evidence/sp3b-summary.md`。25/25 页导航渲染通过；7 ✅ / 11 ⚠️ / 1 ❌(E5 无入口) / 5 未跑。
**分支**: `sp3b-uat-wpf-ui-acceptance`
**前序**: SP3a（22 trx 支撑类用例，PR #366 已合并）。SP3b 收尾 UAT rubric 剩余 **25 个 WPF UI 类**用例。

**Goal:** 把 `docs/uat/acceptance-rubric.md` 里 25 个 UI 类用例（A1–A7、B1–B9、C6–C9、E2–E5）在真实运行的 WPF 客户端上逐个驱动、截图、按判据如实判定（✅/⚠️/❌），就地填 rubric 结果/证据列，归档截图 + UIA 树证据。CI 门禁不变。

**Architecture:**
- 复用并扩展 `tools/uia-acceptance`（FlaUI/UIA3，**仅 UIA pattern，无输入注入**）。新增**步骤 DSL 模式** `--steps`（`nav/waitid/sleep/shot/dumppage/setid/selcombo/selrow/invokeid/invokename/assertid/assertname/assertgridmin/gridrows/log`），一次编译即可命令行驱动所有异构用例，退出码反映断言结果。
- `tools/sp3b_run.ps1` 包装器：每次运行**前后强制 `Stop-Process MetBench_Client`**，避免残留进程占用 LiteDB（`MR/SystemMT/SystemMtJobs.Litedb`，单写）导致下次启动崩溃（已实测此坑：未清理→"MetBench Error: file is being used by another process"）。
- 一次 app 启动 = 一个用例（label=caseXX），证据命名 `caseXX-NN-*.png` / `caseXX-*.txt`。
- 导航项是 Wpf.Ui `NavigationViewItem`（UIA 上为 `DataItem`，无 Invoke peer）→ 工具用 Focus + posted Space/Enter（经 app 自身消息队列，非 SendInput）激活；breadcrumb 文本匹配确认导航成功。

**Tech Stack:** .NET 8 WPF（`MetBench_Client` Release exe）、FlaUI.UIA3 4.0、PowerShell 包装、PrintWindow 截图（锁屏/遮挡仍可截真实内容）。

**执行约定：**
- `dotnet`=`C:\Program Files\dotnet\dotnet.exe`；client exe=`MetBench_Client\bin\Release\net8.0-windows7.0\MetBench_Client.exe`；工具 exe=`tools\uia-acceptance\bin\Release\net8.0-windows\UiaAcceptance.exe`。
- 证据目录 `docs/superpowers/specs/2026-06-15-sp3b-uat-wpf-ui-evidence/`。
- §0.5 最小修改；§4 真实验证（不凑数、不假截图）；§6 显式报错（缺口/⚠️/❌如实标，不掩盖）。
- **不改** `MetBench_BLL.Core/SystemMT/*` public 类型与 CI 门禁；如执行中发现需改 WPF source 才能验收，先记录为缺口，最小改动并说明。

---

## Nav 映射（dump 实测，权威）

| Nav AutomationId | 页面 | 相关用例 |
|---|---|---|
| Nav_ApplicationManagement | ApplicationManagementPage | A1 A2 A3 (A4 绑定) |
| Nav_DomainManagement | DomainManagementPage | A4 |
| Nav_MrManagement | MRManagementPage | A5 A6 |
| Nav_MetaPatterns | MetaPatternsPage | A7 ✅ |
| Nav_Discovery | DiscoveryPage | B1 |
| Nav_SystemMtExecution | SystemMtExecutionPage | B2 B3 B4 B5 |
| Nav_MtExecution | MTExecutionPage | B6 |
| Nav_Anomalies | AnomalyListPage | B7 B8 |
| Nav_Replay | ReplayResultPage | B9 |
| Nav_CandidateReview | CandidateReviewPage | C6 |
| Nav_MrRecommendation | MRRecommendationPage | C7 |
| Nav_MrDetection | AutoDetectMRPage | C8 |
| Nav_Mutation | MutationCampaignPage | C9 |
| Nav_Coverage | CoverageDashboardPage | E2 |
| Nav_MrReportGenerator | MTReportGeneratorPage | E3 E4 |
| (无 nav 项) | DashboardPage | E5（**风险：可能不可从导航到达**） |

已确认控件 id：`DataGrid_MR`、`DataGrid_Application`、`DataGrid_Domain`、`ComboBox_SystemMtExecutionMr`、`Button_RunSystemMt`、`ReportTypeComboBox`、`webview2`。其余控件逐页 `dumppage` 实测后再定步骤（WPF 默认把 `x:Name` 暴露为 AutomationId）。

---

## File Structure

| 文件 | 动作 | 职责 |
|---|---|---|
| `tools/uia-acceptance/Program.cs` | Modify ✅ | 加 `--steps` DSL 引擎 + 通用导航/查找/断言/dump 辅助（保留 `--dump`/`--mr` 旧模式） |
| `tools/sp3b_run.ps1` | Create ✅ | 前后清进程的运行包装器 |
| `docs/superpowers/specs/2026-06-15-sp3b-uat-wpf-ui-evidence/` | Create | 每用例截图 + UIA 树 dump + `sp3b-summary.md` |
| `docs/uat/acceptance-rubric.md` | Modify | 填 25 个 UI 行结果/证据列 |
| `docs/status/current.md`、active plan index、本 plan | Modify | 状态投影（含 SP3a "待 PR" 陈旧修正） |

---

## Tasks

### Task 0: SP3a 收尾 + 工具就绪
- [x] 扩展 `tools/uia-acceptance` 加 `--steps` 引擎，编译通过。
- [x] `tools/sp3b_run.ps1` 包装器（前后清进程）。
- [x] 冒烟：A7 MetaPatterns 干净启动跑通（8 total 分页确认 + 截图）。
- [ ] 修正 `docs/status/current.md` + active index 中 SP3a "待 PR" → 已合并(#366)；决定 SP3a 证据 trx 去留。

### Task 1: A 组 (A1–A7) 管理 CRUD
- [x] **A7** MetaPattern 列表 8 个（4 active + 4 out-of-scope）— ✅ 已跑通。
- [ ] **A1** 新建 Application（dump 页 → setid 名称/语言 → invoke Add → assert 新行）。🔴
- [ ] **A2** 编辑 Application（selrow → 改字段 → Modify → assert 更新）。🔴
- [ ] **A3** 删除 Application（selrow → Delete → assert 行消失/软删）。🔴
- [ ] **A4** 新建 Domain 并绑定 App（Nav_DomainManagement 建域 + ApplicationManagement 绑定）。🟡
- [ ] **A5** 新建 method-level MR（Nav_MrManagement 多控件填表 → Add → assert 增行）。🔴
- [ ] **A6** MR 列表搜索/筛选（设过滤 → Query → 观察筛选 → 清空恢复）。🟢

### Task 2: B 组 (B1–B9) MR 测试主流程
- [ ] **B1** Discovery 选 MR（选 discoverer+app → Run → assert 候选 ≥1 含 confidence）。🟡
- [ ] **B2** System-MT 选 MR + input（选 MR → assert Selected MR + Source Input Preview）。🔴
- [ ] **B3** 生成 followup（跑后 assert FollowUpValue 显示 + 落 temp）。🔴
- [ ] **B4** 跑测试（Run → 进度 → assert status ok/anomaly）。🔴
- [ ] **B5** 结果面板字段齐全（assert src/flw/passed/Δ/threshold/reason）。🔴
- [ ] **B6** Result chart（Nav_MtExecution → assert CartesianChart+PieChart）。🟡
- [ ] **B7** Anomaly List（Nav_Anomalies → assert 倒序网格含 Severity/Category；**需种子异常**）。🔴
- [ ] **B8** 多选 anomaly commonality（多选 → Analyze → assert 报告或 "No commonality"）。🟡
- [ ] **B9** Anomaly Replay（Nav_Replay → assert old vs new + Reproduced；**需异常+回放上下文**）。🔴

### Task 3: C 组 (C6–C9) 发现 UI
- [ ] **C6** Candidate Review（assert 候选列表 + Promote/Validate）。🟡
- [ ] **C7** MR Recommendation（assert top-K 按 confidence 排序）。🟢
- [ ] **C8** AutoDetectMR（Run → 进度 <2min → assert 候选可入库）。🟡
- [ ] **C9** Mutation Campaign（Seed demo → assert mutants/bindings + Kill Rate ≥0）。🟡

### Task 4: E 组 (E2–E5) 可视化&报表
- [ ] **E2** Coverage Dashboard（assert 4 个 PieChart + legend；**需覆盖数据**）。🟡
- [ ] **E3** 报表导出 4 端（ReportTypeComboBox 选 Word/Excel/PDF/HTML → Export → assert 4 文件生成）。🔴
- [ ] **E4** HTML 嵌入 WebView2（assert `webview2` 存在 + 渲染）。🟡
- [ ] **E5** Dashboard 主页 cards（**先验证是否有 nav 入口**；无则如实标缺口/❌并说明）。🟢

### Task 5: 数据种子（按需，执行中触发）
- [ ] 检查 LiteDB 是否已有异常/覆盖/候选数据；不足则用既有 `tools/SeedCrossProgramAnomalies/` 或 UI 内 Seed 按钮（如 C9 Seed demo、MetaPatterns 自动 seed）补足，记录种子来源。

### Task 6: 填 rubric + 归档
- [ ] 填 `acceptance-rubric.md` 25 个 UI 行结果/证据；更新总评汇总表（22 trx + 25 UI = 47）。
- [ ] 写 `sp3b-summary.md`（25 用例逐项 verdict + 实测 + 任何 ⚠️/❌/缺口/种子记录 + 环境）。

### Task 7: 状态投影 + PR
- [ ] current.md + active index + 本 plan 状态字段；SP3a "待 PR" 修正一并落地。
- [ ] 按 `pr-gate-checklist.md` 7 节开 PR；Windows classification=`run-and-log`；贴每用例 verdict。

---

## 判定与诚实原则（接 SP3a）
- 真实运行截图为准；**禁止**伪造截图或在无证据时声称通过。
- 行为符合判据 + 产物齐全 → ✅；核心 OK 有非阻断瑕疵 → ⚠️（备注写明）；功能缺失/异常/不达标 → ❌。
- 任一 🔴 ❌ 阻断 Release；累计 ≥3 🟡 ❌ 阻断；🟢 可延期。结论如实写进 summary 与 rubric 总评，不为"全绿"而放宽判据。
- 需要改 WPF source 才能验收的，先最小改动 + 记录，再验收（接 §9 VM 轨道：本机即 Windows host，可编译可运行）。

## PR Gate Classification
- Scope：单一目的——SP3b UAT WPF-UI 25 用例验收。
- Windows classification：`run-and-log`（真实 WPF 运行 + 截图 + UIA 证据）。
- 代码改动：`tools/`（FlaUI 工具 + 包装脚本，standalone，不入 MetBench.sln）+ 文档/证据；若触 WPF source 单独标注。
- 模块 E：单 PR，非 ≥3-PR chain。
